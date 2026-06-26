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
        private const ulong kStaticFieldsOffset = 184;   // void* static_fields (0xB8): IL2CPP v31 (Unity 2022.3) adds klass self-ref at +120, shifting static_fields from 0xB0 to 0xB8

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

        // Cached type info for System.String (resolved from metadata, not gRPC)
        private Il2CppTypeInfo _stringTypeInfo;

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

                // System.String uses IL2CPP_TYPE_STRING (not CLASS), so it won't appear in the
                // type maps. Return the metadata-derived field layout from the cached entry.
                if (managedType == typeof(string))
                {
                    result = _stringTypeInfo;
                    return _stringTypeInfo != null;
                }

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
            result.Fields.AddRange(BuildFields(typeDef, typeDefIndex));

            _logger?.LogDebug("[BinaryTypeInfoProvider] v26 resolved {Name}: classPtr=0x{C:X}, staticFields=0x{SF:X}, fields={N}",
                il2cppName, classPtr, staticFieldsAddr, result.Fields.Count);
            return true;
        }

        private bool TryGetTypeInfoV27Plus(Il2CsRuntimeContext runtime, Type managedType, string il2cppName, out Il2CppTypeInfo result)
        {
            result = null;
            if (!_typeNameToTypeIndex!.TryGetValue(il2cppName, out var entry))
            {
                _logger?.LogWarning("[BinaryTypeInfoProvider] v27+ Not in type index: {Name}", il2cppName);
                return false;
            }

            var (typeIndex, typeDefIndex) = entry;
            var il2cpp = _typeModel!.Il2Cpp;
            Il2CppType il2cppType = il2cpp.Types[typeIndex];
            ulong slide = GetAslrSlide(runtime);

            // Verify the name in the map matches what the TypeModel generates for this specific type entry
            string verifyName = "(error)";
            try { verifyName = _typeModel.GetTypeName(il2cppType, addNamespace: true, is_nested: false); } catch { }
            _logger?.LogWarning("[BinaryTypeInfoProvider] v27+ lookup {Name}: typeIndex={TI}, typeEnum={TE}, mapKey={Verify}",
                il2cppName, typeIndex, il2cppType.type, verifyName);

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
            result.Fields.AddRange(BuildFields(typeDef, typeDefIndex));

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

                    // Read class_inst (offset +8 from base) to verify which generic arg is actually stored
                    ulong genericClassRuntimeBase = genericClassBinaryVA + slide;
                    ulong classInstPtr = 0, typeArgvPtr = 0, firstArgTypePtr = 0;
                    string firstArgName = "(err)";
                    try
                    {
                        classInstPtr = runtime.ReadPointer(genericClassRuntimeBase + 8);
                        if (classInstPtr != 0)
                        {
                            // Il2CppGenericInst: uint32 argc at +0, padding, Il2CppType** argv at +8
                            typeArgvPtr = runtime.ReadPointer(classInstPtr + 8);
                            if (typeArgvPtr != 0)
                            {
                                firstArgTypePtr = runtime.ReadPointer(typeArgvPtr);
                                if (firstArgTypePtr != 0)
                                {
                                    // Il2CppType layout: ulong data at +0, uint16 attrs at +8, byte type at +10
                                    // data for CLASS type contains klassIndex (low 32 bits) OR a pointer to Il2CppClass
                                    // Read 8 bytes at +0 (data field) and 1 byte at +10 (type enum)
                                    var typeBytes = runtime.ReadMemory(firstArgTypePtr, 12).ToArray();
                                    ulong dataField = System.BitConverter.ToUInt64(typeBytes, 0);
                                    byte typeEnum = typeBytes[10];
                                    // Also: if the argType is CLASS (typeEnum=0x0A=10), dataField low 32 bits = klassIndex in metadata
                                    // Then read classname from class at klassIndex
                                    firstArgName = $"argTypePtr=0x{firstArgTypePtr:X},data=0x{dataField:X},typeEnum=0x{typeEnum:X}";

                                    // For CLASS type (0x12): data is a pointer to Il2CppClass at runtime
                                    // Try reading its name
                                    if (typeEnum == 0x12 && dataField != 0)
                                    {
                                        try
                                        {
                                            ulong argNamePtr = runtime.ReadPointer(dataField + kClassNameOffset);
                                            string argClassName = argNamePtr != 0 ? ReadProcessString(runtime, argNamePtr, 128) : "(null namePtr)";
                                            firstArgName += $",argClass={argClassName}";
                                        }
                                        catch (Exception ex) { firstArgName += $",argClassErr={ex.Message}"; }
                                    }
                                }
                            }
                        }
                    }
                    catch { firstArgName = "(read failed)"; }

                    _logger?.LogWarning(
                        "[BinaryTypeInfoProvider] GENERICINST {Name}: genericClassBinaryVA=0x{GVA:X}, slide=0x{Slide:X}, cachedClassAddr=0x{CCA:X}, classPtr=0x{CP:X}, classInstPtr=0x{CI:X}, firstArg={FA}",
                        name, genericClassBinaryVA, slide, cachedClassRuntimeAddr, classPtr, classInstPtr, firstArgName);

                    if (classPtr != 0)
                    {
                        try
                        {
                            ulong namePtr = runtime.ReadPointer(classPtr + kClassNameOffset);
                            string className = namePtr != 0 ? ReadProcessString(runtime, namePtr, 128) : "(null)";

                            // Read generic_class field at +96 from the Il2CppClass* to verify which instantiation this is
                            // generic_class* at +96: if non-null, its VA (minus slide) should match genericClassBinaryVA
                            ulong genericClassRtPtr = runtime.ReadPointer(classPtr + 96);
                            ulong genericClassVAFromClass = genericClassRtPtr != 0 ? (genericClassRtPtr - slide) : 0;
                            _logger?.LogWarning("[BinaryTypeInfoProvider] classPtr=0x{CP:X} name={ClassName}, generic_class@+96=0x{GC:X} -> binaryVA=0x{BGVA:X} (expected 0x{ExpVA:X})",
                                classPtr, className, genericClassRtPtr, genericClassVAFromClass, genericClassBinaryVA);

                            // Scan offsets near static_fields to diagnose wrong offset vs uninitialized
                            for (ulong scan = 152; scan <= 200; scan += 8)
                            {
                                try
                                {
                                    ulong val = runtime.ReadPointer(classPtr + scan);
                                    if (val != 0)
                                        _logger?.LogWarning("[BinaryTypeInfoProvider]   classPtr+{Offset}=0x{Val:X}", scan, val);
                                }
                                catch { }
                            }
                        }
                        catch { /* diagnostic only */ }
                    }

                    return classPtr;
                }

                // CLASS/VALUETYPE in v27+: no MetadataUsages to look up.
                // Fall back: scan method binary code for RIP-relative TypeInfo global references.
                return TryGetClassPtrFromMethodCode(runtime, (int)il2cppType.data.klassIndex, slide, name);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[BinaryTypeInfoProvider] TryGetClassPtrV27 failed for {Name}", name);
                return 0;
            }
        }

        // Finds the Il2CppClass* for a CLASS/VALUETYPE type in v27+ by scanning the first method's
        // binary code for RIP-relative references to the global TypeInfo pointer that the generated
        // IL2CPP code uses to access the class object. Each IL2CPP method that uses a concrete type
        // starts with LEA/MOV instructions of the form [REX] 8B/8D [RIP+disp32] that point to the
        // global `TypeName__TypeInfo` variable stored in the binary's .data/.bss section.
        private ulong TryGetClassPtrFromMethodCode(Il2CsRuntimeContext runtime, int typeDefIndex, ulong slide, string name)
        {
            try
            {
                var metadata = _typeModel!.Metadata;
                var il2cpp = _typeModel.Il2Cpp;
                var typeDef = metadata.typeDefs[typeDefIndex];

                // Find which image (module) contains this type definition
                string imageName = null;
                for (int i = 0; i < metadata.imageDefs.Length; i++)
                {
                    var img = metadata.imageDefs[i];
                    if (typeDefIndex >= img.typeStart && typeDefIndex < img.typeStart + img.typeCount)
                    {
                        imageName = metadata.GetStringFromIndex(img.nameIndex);
                        break;
                    }
                }
                if (imageName == null || !il2cpp.CodeGenModuleMethodPointers.ContainsKey(imageName))
                {
                    _logger?.LogWarning("[BinaryTypeInfoProvider] MethodCode: no module for {N} (image={I})", name, imageName ?? "null");
                    return 0;
                }

                // Find first method of the type with a non-zero binary VA
                ulong methodBinaryVA = 0;
                string methodName = null;
                for (int m = 0; m < typeDef.method_count && methodBinaryVA == 0; m++)
                {
                    var methodDef = metadata.methodDefs[typeDef.methodStart + m];
                    ulong va = il2cpp.GetMethodPointer(imageName, methodDef);
                    if (va != 0)
                    {
                        methodBinaryVA = va;
                        methodName = metadata.GetStringFromIndex(methodDef.nameIndex);
                    }
                }
                if (methodBinaryVA == 0)
                {
                    _logger?.LogWarning("[BinaryTypeInfoProvider] MethodCode: no method pointer for {N}", name);
                    return 0;
                }
                _logger?.LogWarning("[BinaryTypeInfoProvider] MethodCode: scanning {N}.{M} @ binary 0x{VA:X}", name, methodName, methodBinaryVA);

                // Read 256 bytes of method code from the binary file
                byte[] code;
                try
                {
                    il2cpp.Position = il2cpp.MapVATR(methodBinaryVA);
                    code = il2cpp.ReadBytes(256);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning("[BinaryTypeInfoProvider] MethodCode: binary read failed for {N}: {E}", name, ex.Message);
                    return 0;
                }

                // Scan for x64 RIP-relative memory references: [REX] OP modrm[mod=00,rm=101] disp32
                // Instruction encoding (7 bytes total):
                //   byte 0: REX prefix (0x40–0x4F)
                //   byte 1: opcode (0x8B=MOV or 0x8D=LEA)
                //   byte 2: ModRM with mod=00 (bits 7:6), rm=101 (bits 2:0) → mask 0xC7 == 0x05
                //   bytes 3–6: 32-bit signed displacement
                // refBinaryVA = (methodBinaryVA + instrOffset + 7) + disp32
                string shortName = name.Contains('.') ? name.Split('.')[^1] : name;
                for (int off = 0; off <= 249; off++)
                {
                    byte b0 = code[off];
                    if (b0 < 0x40 || b0 > 0x4F) continue;
                    byte b1 = code[off + 1];
                    if (b1 != 0x8B && b1 != 0x8D) continue;
                    byte modrm = code[off + 2];
                    if ((modrm & 0xC7) != 0x05) continue;

                    int disp32 = BitConverter.ToInt32(code, off + 3);
                    ulong nextInstrBinaryVA = methodBinaryVA + (ulong)off + 7;
                    ulong refBinaryVA = (ulong)((long)nextInstrBinaryVA + disp32);
                    ulong refRuntimeAddr = refBinaryVA + slide;

                    try
                    {
                        ulong classPtr = runtime.ReadPointer(refRuntimeAddr);
                        if (classPtr == 0) continue;

                        // Validate: read the class name from the Il2CppClass* object
                        ulong namePtr = runtime.ReadPointer(classPtr + kClassNameOffset);
                        if (namePtr == 0) continue;
                        string className = ReadProcessString(runtime, namePtr, 64);
                        if (className == null) continue;

                        _logger?.LogWarning("[BinaryTypeInfoProvider] MethodCode {N}: code+{Off}=0x{Op:X2}{Mod:X2}, refBinaryVA=0x{RVA:X}, classPtr=0x{CP:X}, className={CN}",
                            name, off, b1, modrm, refBinaryVA, classPtr, className);

                        if (string.Equals(className, shortName, StringComparison.Ordinal))
                            return classPtr;
                    }
                    catch { continue; }
                }

                _logger?.LogWarning("[BinaryTypeInfoProvider] MethodCode: no TypeInfo ref found for {N} in method {M}", name, methodName);
                return 0;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("[BinaryTypeInfoProvider] TryGetClassPtrFromMethodCode failed for {N}: {E}", name, ex.Message);
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

            _stringTypeInfo = BuildStringTypeInfo();
        }

        private Il2CppTypeInfo BuildStringTypeInfo()
        {
            var metadata = _typeModel!.Metadata;
            for (int i = 0; i < metadata.typeDefs.Length; i++)
            {
                var td = metadata.typeDefs[i];
                if (metadata.GetStringFromIndex(td.namespaceIndex) == "System" &&
                    metadata.GetStringFromIndex(td.nameIndex) == "String")
                {
                    var info = new Il2CppTypeInfo
                    {
                        KlassId = new ClassId { Address = 0 },
                        StaticFieldsAddress = 0
                    };
                    info.Fields.AddRange(BuildFields(td, i));
                    _logger?.LogInformation("[BinaryTypeInfoProvider] System.String typedef found at index {I}, fields={N}", i, info.Fields.Count);
                    return info;
                }
            }
            _logger?.LogWarning("[BinaryTypeInfoProvider] System.String typedef not found in metadata");
            return null;
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

                    // Log every SingleInstance<T> entry; also manually resolve the binary generic arg to cross-check TypeModel
                    if (name.Contains("SingleInstance<") && il2cppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
                    {
                        string binaryArgName = "(err)";
                        try
                        {
                            var genClass = il2cpp.MapVATR<Il2CppGenericClass>(il2cppType.data.generic_class);
                            if (genClass.context.class_inst != 0)
                            {
                                var genInst = il2cpp.MapVATR<Il2CppGenericInst>(genClass.context.class_inst);
                                if (genInst.type_argc > 0 && genInst.type_argv != 0)
                                {
                                    // type_argv is an array of ulong (binary VAs of Il2CppType*)
                                    ulong firstArgVA = il2cpp.MapVATR<ulong>(genInst.type_argv);
                                    var firstArgType = il2cpp.MapVATR<Il2CppType>(firstArgVA);
                                    firstArgType.Init(_typeModel.Metadata.Version);
                                    binaryArgName = _typeModel.GetTypeName(firstArgType, addNamespace: true, is_nested: false);
                                }
                            }
                        }
                        catch (Exception ex) { binaryArgName = $"err:{ex.Message}"; }
                        _logger?.LogWarning("[BinaryTypeInfoProvider] BuildMapsV27Plus SingleInstance entry: i={I}, TypeModel={N}, binaryArg={BA}, generic_class_VA=0x{GVA:X}",
                            i, name, binaryArgName, il2cppType.data.generic_class);
                    }

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

        private IEnumerable<Il2CppField> BuildFields(Il2CppTypeDefinition typeDef, int typeDefIndex)
        {
            var metadata = _typeModel!.Metadata;
            var il2cpp = _typeModel.Il2Cpp;
            var fields = new List<Il2CppField>(typeDef.field_count);

            // For generic type definitions, the binary field offset table pointer is null.
            // Detect this: FieldOffsetsArePointers && FieldOffsets[typeDefIndex] == 0.
            // When true, compute offsets manually: reference-type fields are 8 bytes each,
            // static fields start at 0, instance fields start at 16 (object header).
            // For generic type definitions (genericContainerIndex >= 0), IL2CPP v27+ stores
            // zeros for all field offsets even when the pointer is non-null. Compute manually.
            bool hasBinaryOffsets = !il2cpp.FieldOffsetsArePointers
                || (typeDefIndex < il2cpp.FieldOffsets.Length
                    && il2cpp.FieldOffsets[typeDefIndex] != 0
                    && typeDef.genericContainerIndex < 0);
            ulong computedStaticOffset = 0;
            ulong computedInstanceOffset = 16; // skip 16-byte object header

            for (int i = 0; i < typeDef.field_count; i++)
            {
                int fieldDefIndex = typeDef.fieldStart + i;
                var fieldDef = metadata.fieldDefs[fieldDefIndex];
                string fieldName = metadata.GetStringFromIndex(fieldDef.nameIndex);
                var fieldType = il2cpp.Types[fieldDef.typeIndex];
                bool isStatic = ((FieldAttributes)fieldType.attrs).HasFlag(FieldAttributes.Static);

                ulong offset;
                if (hasBinaryOffsets)
                {
                    offset = _typeModel.GetFieldOffsetFromIndex(typeDef, fieldDefIndex);
                }
                else
                {
                    // No binary offsets for generic type definition: lay fields out sequentially.
                    // All types treated as pointer-sized (8 bytes) — sufficient for reference types.
                    if (isStatic)
                    {
                        offset = computedStaticOffset;
                        computedStaticOffset += 8;
                    }
                    else
                    {
                        offset = computedInstanceOffset;
                        computedInstanceOffset += 8;
                    }
                }

                _logger?.LogWarning("[BinaryTypeInfoProvider] BuildFields field[{I}] name={Name} isStatic={S} offset={O} hasBinaryOffsets={H}",
                    i, fieldName, isStatic, offset, hasBinaryOffsets);
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
