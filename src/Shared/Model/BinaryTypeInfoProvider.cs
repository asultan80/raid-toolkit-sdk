using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Il2CppToolkit.Injection.Client;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;

using Microsoft.Extensions.Logging;

namespace Raid.Toolkit.Model
{
    public class BinaryTypeInfoProvider : ITypeInfoProvider
    {
        // Il2CppClass field offsets (confirmed by ClassDefinition.cs: name@16, namespace@24, parent@80, base@88)
        private const ulong kClassNameOffset = 16;       // const char* name
        private const ulong kClassNamespaceOffset = 24;  // const char* namespaze
        private const ulong kStaticFieldsOffset = 176;   // void* static_fields (0xB0): after generic_class@96 + 8 more pointers

        // Il2CppGenericClass offsets for v27+ (type@0, context@8/16, cached_class@24)
        private const ulong kCachedClassOffset = 24;

        private readonly ILogger<BinaryTypeInfoProvider> _logger;
        private readonly object _initLock = new object();

        private TypeModel _typeModel;
        private bool _isV27Plus;

        // v<27: Maps canonical IL2CPP type name → (MetadataUsages destIndex, typeDef index)
        private Dictionary<string, (uint destIndex, int typeDefIndex)> _typeNameToSlot;

        // v27+: Maps canonical IL2CPP type name → (il2cpp.Types[] index, typeDef index)
        private Dictionary<string, (int typeIndex, int typeDefIndex)> _typeNameToTypeIndex;

        public BinaryTypeInfoProvider(ILogger<BinaryTypeInfoProvider> logger = null)
        {
            _logger = logger;
        }

        public bool TryGetTypeInfo(Il2CsRuntimeContext runtime, Type managedType, out Il2CppTypeInfo result)
        {
            result = null;
            try
            {
                EnsureInitialized();

                string il2cppName = GetIl2CppTypeName(managedType);
                if (il2cppName == null)
                {
                    _logger?.LogDebug("[BinaryTypeInfoProvider] No name for: {Name}", managedType.FullName);
                    return false;
                }

                if (_isV27Plus)
                    return TryGetTypeInfoV27Plus(runtime, managedType, il2cppName, out result);

                return TryGetTypeInfoV26(runtime, managedType, il2cppName, out result);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[BinaryTypeInfoProvider] Failed: {Name}", managedType.FullName);
                return false;
            }
        }

        private bool TryGetTypeInfoV26(Il2CsRuntimeContext runtime, Type managedType, string il2cppName, out Il2CppTypeInfo result)
        {
            result = null;
            if (!_typeNameToSlot!.TryGetValue(il2cppName, out var slot))
            {
                _logger?.LogDebug("[BinaryTypeInfoProvider] Not in MetadataUsages: {Name}", il2cppName);
                return false;
            }

            var (destIndex, typeDefIndex) = slot;
            var il2cpp = _typeModel!.Il2Cpp;

            if (il2cpp.MetadataUsages == null || destIndex >= (uint)il2cpp.MetadataUsages.Length)
            {
                _logger?.LogWarning("[BinaryTypeInfoProvider] MetadataUsages out of range: {Name}", il2cppName);
                return false;
            }

            ulong slide = GetAslrSlide(runtime);
            ulong slotVA = il2cpp.MetadataUsages[destIndex];
            ulong slotAddr = slotVA + slide;
            ulong classPtr = runtime.ReadPointer(slotAddr);

            if (classPtr == 0)
            {
                _logger?.LogWarning("[BinaryTypeInfoProvider] Null classPtr: {Name}", il2cppName);
                return false;
            }

            ulong staticFieldsAddr = runtime.ReadPointer(classPtr + kStaticFieldsOffset);
            var typeDef = _typeModel.Metadata.typeDefs[typeDefIndex];

            result = new Il2CppTypeInfo
            {
                KlassId = new ClassId { Address = classPtr },
                StaticFieldsAddress = staticFieldsAddr
            };
            result.Fields.AddRange(BuildFields(typeDef));

            _logger?.LogDebug("[BinaryTypeInfoProvider] v26 resolved {Name}: classPtr=0x{C:X}, staticFields=0x{SF:X}, fields={N}",
                il2cppName, classPtr, staticFieldsAddr, result.Fields.Count);
            return true;
        }

        private bool TryGetTypeInfoV27Plus(Il2CsRuntimeContext runtime, Type managedType, string il2cppName, out Il2CppTypeInfo result)
        {
            result = null;
            if (!_typeNameToTypeIndex!.TryGetValue(il2cppName, out var entry))
            {
                _logger?.LogDebug("[BinaryTypeInfoProvider] v27+ Not in type index: {Name}", il2cppName);
                return false;
            }

            var (typeIndex, typeDefIndex) = entry;
            var il2cpp = _typeModel!.Il2Cpp;
            Il2CppType il2cppType = il2cpp.Types[typeIndex];
            ulong slide = GetAslrSlide(runtime);

            ulong classPtr = TryGetClassPtrV27(runtime, il2cppType, slide, il2cppName);
            ulong staticFieldsAddr = 0;

            if (classPtr != 0)
            {
                staticFieldsAddr = runtime.ReadPointer(classPtr + kStaticFieldsOffset);
                _logger?.LogDebug("[BinaryTypeInfoProvider] v27+ resolved {Name}: classPtr=0x{C:X}, staticFields=0x{SF:X}",
                    il2cppName, classPtr, staticFieldsAddr);
            }
            else
            {
                _logger?.LogDebug("[BinaryTypeInfoProvider] v27+ no classPtr for {Name}, returning fields only", il2cppName);
            }

            var typeDef = _typeModel.Metadata.typeDefs[typeDefIndex];
            result = new Il2CppTypeInfo
            {
                KlassId = new ClassId { Address = classPtr },
                StaticFieldsAddress = staticFieldsAddr
            };
            result.Fields.AddRange(BuildFields(typeDef));

            if (staticFieldsAddr == 0)
                _logger?.LogWarning("[BinaryTypeInfoProvider] v27+ {Name}: StaticFieldsAddress=0 (classPtr=0 for CLASS/VALUETYPE — static field reads will fail)", il2cppName);

            _logger?.LogDebug("[BinaryTypeInfoProvider] v27+ {Name}: classPtr=0x{C:X}, staticFields=0x{SF:X}, fields={N}",
                il2cppName, classPtr, staticFieldsAddr, result.Fields.Count);
            return true;
        }

        private ulong TryGetClassPtrV27(Il2CsRuntimeContext runtime, Il2CppType il2cppType, ulong slide, string name)
        {
            try
            {
                if (il2cppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
                {
                    ulong genericClassBinaryVA = il2cppType.data.generic_class;
                    if (genericClassBinaryVA == 0)
                    {
                        _logger?.LogWarning("[BinaryTypeInfoProvider] GENERICINST has null generic_class VA: {Name}", name);
                        return 0;
                    }

                    // Il2CppGenericClass layout for v27+:
                    //   +0:  type (ulong, pointer to Il2CppType)
                    //   +8:  context.class_inst (ulong)
                    //   +16: context.method_inst (ulong)
                    //   +24: cached_class (ulong, the Il2CppClass* set at runtime)
                    ulong cachedClassRuntimeAddr = genericClassBinaryVA + slide + kCachedClassOffset;
                    ulong classPtr = runtime.ReadPointer(cachedClassRuntimeAddr);

                    _logger?.LogWarning(
                        "[BinaryTypeInfoProvider] GENERICINST {Name}: genericClassBinaryVA=0x{GVA:X}, slide=0x{Slide:X}, cachedClassAddr=0x{CCA:X}, classPtr=0x{CP:X}",
                        name, genericClassBinaryVA, slide, cachedClassRuntimeAddr, classPtr);

                    if (classPtr != 0)
                    {
                        // Sanity check: verify name string at classPtr + kClassNameOffset
                        try
                        {
                            ulong namePtr = runtime.ReadPointer(classPtr + kClassNameOffset);
                            if (namePtr != 0)
                            {
                                string className = ReadProcessString(runtime, namePtr, 128);
                                _logger?.LogDebug("[BinaryTypeInfoProvider] classPtr name check: '{ClassName}' (expected generic type name)", className);
                            }
                        }
                        catch { /* name check is diagnostic only */ }
                    }

                    return classPtr;
                }

                // CLASS / VALUETYPE: no classPtr available in v27+ without MetadataUsages
                _logger?.LogDebug("[BinaryTypeInfoProvider] v27+ CLASS/VALUETYPE {Name} ({T}): returning classPtr=0", name, il2cppType.type);
                return 0;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[BinaryTypeInfoProvider] TryGetClassPtrV27 failed for {Name}", name);
                return 0;
            }
        }

        private void EnsureInitialized()
        {
            if (_typeModel != null) return;
            lock (_initLock)
            {
                if (_typeModel != null) return;
                Initialize();
            }
        }

        private void Initialize()
        {
            PlariumPlayAdapter.GameInfo gameInfo = ModelLoader.GetGameInfo();
            string gameDir = Path.Combine(gameInfo.InstallPath, "build");
            string metadataPath = Path.Combine(gameDir, @"Raid_Data\il2cpp_data\Metadata\global-metadata.dat");
            string gasmPath = Path.Combine(gameDir, @"GameAssembly.dll");

            _logger?.LogInformation("[BinaryTypeInfoProvider] Loading metadata from {Path}", gasmPath);
            Loader loader = new();
            loader.Init(gasmPath, metadataPath);
            _typeModel = new TypeModel(loader);
            BuildMaps();
            _logger?.LogInformation("[BinaryTypeInfoProvider] Ready: v27+={V27}, types={Count}",
                _isV27Plus,
                _isV27Plus ? _typeNameToTypeIndex?.Count ?? 0 : _typeNameToSlot?.Count ?? 0);
        }

        private void BuildMaps()
        {
            var metadata = _typeModel!.Metadata;
            var il2cpp = _typeModel.Il2Cpp;

            if (metadata.metadataUsageDic != null)
            {
                // v<27: use MetadataUsages slot dictionary
                _isV27Plus = false;
                _typeNameToSlot = new Dictionary<string, (uint, int)>(StringComparer.Ordinal);
                BuildMapsV26(metadata, il2cpp);
            }
            else
            {
                // v27+: metadataUsageDic is null; build name→typeIndex map from il2cpp.Types[]
                _isV27Plus = true;
                _typeNameToTypeIndex = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
                BuildMapsV27Plus(il2cpp);
            }
        }

        private void BuildMapsV26(Metadata metadata, Il2Cpp il2cpp)
        {
            if (!metadata.metadataUsageDic.TryGetValue(Il2CppMetadataUsage.kIl2CppMetadataUsageTypeInfo, out var usageDic))
            {
                _logger?.LogWarning("[BinaryTypeInfoProvider] No kIl2CppMetadataUsageTypeInfo in metadataUsageDic");
                return;
            }

            foreach (var pair in usageDic)
            {
                uint destIndex = pair.Key;
                uint typeIndex = pair.Value;

                if (typeIndex >= (uint)il2cpp.Types.Length) continue;
                Il2CppType il2cppType = il2cpp.Types[(int)typeIndex];

                int typeDefIndex = GetTypeDefIndex(il2cppType);
                if (typeDefIndex < 0) continue;

                string name;
                try { name = _typeModel.GetTypeName(il2cppType, addNamespace: true, is_nested: false); }
                catch { continue; }
                if (string.IsNullOrEmpty(name)) continue;

                _typeNameToSlot[name] = (destIndex, typeDefIndex);
            }
        }

        private void BuildMapsV27Plus(Il2Cpp il2cpp)
        {
            for (int i = 0; i < il2cpp.Types.Length; i++)
            {
                try
                {
                    Il2CppType il2cppType = il2cpp.Types[i];

                    int typeDefIndex = GetTypeDefIndex(il2cppType);
                    if (typeDefIndex < 0) continue;

                    string name;
                    try { name = _typeModel.GetTypeName(il2cppType, addNamespace: true, is_nested: false); }
                    catch { continue; }
                    if (string.IsNullOrEmpty(name)) continue;

                    _typeNameToTypeIndex[name] = (i, typeDefIndex);
                }
                catch { /* skip types that fail to process */ }
            }
        }

        private int GetTypeDefIndex(Il2CppType il2cppType)
        {
            switch (il2cppType.type)
            {
                case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                    return (int)il2cppType.data.klassIndex;
                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                    var genericClass = _typeModel!.Il2Cpp.MapVATR<Il2CppGenericClass>(il2cppType.data.generic_class);
                    long idx = _typeModel.GetGenericClassTypeDefinitionIndex(genericClass);
                    return idx >= 0 ? (int)idx : -1;
                default:
                    return -1;
            }
        }

        private ulong GetAslrSlide(Il2CsRuntimeContext runtime)
        {
            foreach (System.Diagnostics.ProcessModule mod in runtime.TargetProcess.Modules)
            {
                if (string.Equals(mod.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase))
                {
                    ulong moduleBase = (ulong)(long)mod.BaseAddress;
                    ulong imageBase = _typeModel!.Il2Cpp.ImageBase;
                    return moduleBase - imageBase;
                }
            }
            throw new InvalidOperationException("GameAssembly.dll not found in target process modules");
        }

        private IEnumerable<Il2CppField> BuildFields(Il2CppTypeDefinition typeDef)
        {
            var metadata = _typeModel!.Metadata;
            var il2cpp = _typeModel.Il2Cpp;
            var fields = new List<Il2CppField>(typeDef.field_count);

            for (int i = 0; i < typeDef.field_count; i++)
            {
                int fieldDefIndex = typeDef.fieldStart + i;
                var fieldDef = metadata.fieldDefs[fieldDefIndex];
                string fieldName = metadata.GetStringFromIndex(fieldDef.nameIndex);
                var fieldType = il2cpp.Types[fieldDef.typeIndex];
                bool isStatic = ((FieldAttributes)fieldType.attrs).HasFlag(FieldAttributes.Static);
                ulong offset = _typeModel.GetFieldOffsetFromIndex(typeDef, fieldDefIndex);

                fields.Add(new Il2CppField
                {
                    Name = fieldName,
                    Offset = (uint)offset,
                    KlassAddr = 0,
                    Static = isStatic
                });
            }

            return fields;
        }

        private static string ReadProcessString(Il2CsRuntimeContext runtime, ulong ptr, int maxLen = 256)
        {
            if (ptr == 0) return null;
            try
            {
                var bytes = runtime.ReadMemory(ptr, (ulong)maxLen).ToArray();
                int len = Array.IndexOf(bytes, (byte)0);
                if (len < 0) len = maxLen;
                return System.Text.Encoding.UTF8.GetString(bytes, 0, len);
            }
            catch { return null; }
        }

        // Mirrors TypeModel.GetTypeName(addNamespace, is_nested) format exactly:
        //   - Outermost type: namespace included (addNamespace=true)
        //   - Generic arguments: NO namespace (addNamespace=false), separator ", "
        //   - Nested types: "." separator, no generic args appended (isNested=true stops early)
        private static string GetIl2CppTypeName(Type type, bool addNamespace = true, bool isNested = false)
        {
            if (type == null) return null;

            string text = "";

            if (type.DeclaringType != null)
            {
                // Nested type: prefix with declaring type (no generic args on declaring type per is_nested)
                text = GetIl2CppTypeName(type.DeclaringType, addNamespace, isNested: true) + ".";
            }
            else if (addNamespace && !string.IsNullOrEmpty(type.Namespace))
            {
                text = type.Namespace + ".";
            }

            // Use generic type definition's name to get the backtick-stripped base name
            string name = type.IsConstructedGenericType
                ? type.GetGenericTypeDefinition().Name
                : type.Name;
            int bt = name.IndexOf('`');
            if (bt >= 0) name = name.Substring(0, bt);
            text += name;

            // Nested types stop here (TypeModel does the same via is_nested=true)
            if (isNested) return text;

            if (type.IsConstructedGenericType)
            {
                IEnumerable<string> args = type.GenericTypeArguments.Select(a => GetIl2CppTypeName(a, addNamespace: false));
                text += "<" + string.Join(", ", args) + ">";
            }

            return text;
        }
    }
}
