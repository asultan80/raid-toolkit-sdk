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
        private readonly ILogger<BinaryTypeInfoProvider> _logger;
        private readonly object _initLock = new object();

        private TypeModel _typeModel;
        // Maps canonical IL2CPP type name → (MetadataUsages destIndex, typeDef index for field lookup)
        private Dictionary<string, (uint destIndex, int typeDefIndex)> _typeNameToSlot;

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

                // Read static_fields pointer from Il2CppClass at offset +96
                ulong staticFieldsAddr = runtime.ReadPointer(classPtr + 96);

                var typeDef = _typeModel.Metadata.typeDefs[typeDefIndex];
                var fields = BuildFields(typeDef);

                result = new Il2CppTypeInfo
                {
                    KlassId = new ClassId { Address = classPtr },
                    StaticFieldsAddress = staticFieldsAddr
                };
                result.Fields.AddRange(fields);

                _logger?.LogDebug("[BinaryTypeInfoProvider] Resolved {Name}: classPtr=0x{C:X}, staticFields=0x{SF:X}, fields={N}",
                    il2cppName, classPtr, staticFieldsAddr, result.Fields.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[BinaryTypeInfoProvider] Failed: {Name}", managedType.FullName);
                return false;
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
            _logger?.LogInformation("[BinaryTypeInfoProvider] Ready: {Count} types in MetadataUsages",
                _typeNameToSlot!.Count);
        }

        private void BuildMaps()
        {
            _typeNameToSlot = new Dictionary<string, (uint, int)>(StringComparer.Ordinal);

            var metadata = _typeModel!.Metadata;
            var il2cpp = _typeModel.Il2Cpp;

            if (!metadata.metadataUsageDic.TryGetValue(Il2CppMetadataUsage.kIl2CppMetadataUsageTypeInfo, out var usageDic))
            {
                _logger?.LogWarning("[BinaryTypeInfoProvider] No kIl2CppMetadataUsageTypeInfo entries in metadata");
                return;
            }

            foreach (var pair in usageDic)
            {
                uint destIndex = pair.Key;
                uint typeIndex = pair.Value;

                if (typeIndex >= (uint)il2cpp.Types.Length) continue;
                Il2CppType il2cppType = il2cpp.Types[(int)typeIndex];

                // Get the typeDef index for field lookup
                int typeDefIndex = GetTypeDefIndex(il2cppType);
                if (typeDefIndex < 0) continue;

                // Compute the canonical name via TypeModel — this correctly handles
                // generic instantiations (GENERICINST) by using the actual type args
                string name;
                try { name = _typeModel.GetTypeName(il2cppType, addNamespace: true, is_nested: false); }
                catch { continue; }
                if (string.IsNullOrEmpty(name)) continue;

                _typeNameToSlot[name] = (destIndex, typeDefIndex);
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
                // Raw metadata name matches what StaticFieldMember was constructed with
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
            // (e.g. "List`1" → "List", "AppModel" → "AppModel")
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
                // Format args without namespace, joined with ", " (matches GetGenericInstParams)
                IEnumerable<string> args = type.GenericTypeArguments.Select(a => GetIl2CppTypeName(a, addNamespace: false));
                text += "<" + string.Join(", ", args) + ">";
            }

            return text;
        }
    }
}
