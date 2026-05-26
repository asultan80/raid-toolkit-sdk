using System;
using System.Collections.Generic;
using System.IO;
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
        private Dictionary<string, int> _typeNameToIndex;       // "Namespace.TypeName" → typeIndex
        private Dictionary<int, uint> _typeIndexToMetadataSlot; // typeIndex → MetadataUsages dest index

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
                    _logger?.LogDebug("[BinaryTypeInfoProvider] Skipping nested type: {Name}", managedType.FullName);
                    return false;
                }

                if (!_typeNameToIndex!.TryGetValue(il2cppName, out int typeIndex))
                {
                    _logger?.LogDebug("[BinaryTypeInfoProvider] Type not found in binary: {Name}", il2cppName);
                    return false;
                }

                if (!_typeIndexToMetadataSlot!.TryGetValue(typeIndex, out uint destIndex))
                {
                    _logger?.LogDebug("[BinaryTypeInfoProvider] No MetadataUsage entry for type: {Name} (typeIndex={Idx})", il2cppName, typeIndex);
                    return false;
                }

                var il2cpp = _typeModel!.Il2Cpp;
                if (il2cpp.MetadataUsages == null || destIndex >= (uint)il2cpp.MetadataUsages.Length)
                {
                    _logger?.LogWarning("[BinaryTypeInfoProvider] MetadataUsages out of range for type: {Name}", il2cppName);
                    return false;
                }

                ulong slide = GetAslrSlide(runtime);
                ulong slotVA = il2cpp.MetadataUsages[destIndex];
                ulong slotAddr = slotVA + slide;

                // slotAddr points to a slot in the binary's data section that holds Il2CppClass* at runtime
                ulong classPtr = runtime.ReadPointer(slotAddr);
                if (classPtr == 0)
                {
                    _logger?.LogWarning("[BinaryTypeInfoProvider] Null class pointer for type: {Name}", il2cppName);
                    return false;
                }

                // Read static_fields pointer from Il2CppClass at offset +96
                ulong staticFieldsAddr = runtime.ReadPointer(classPtr + 96);

                var typeDef = _typeModel.Metadata.typeDefs[typeIndex];
                var fields = BuildFields(typeDef);

                result = new Il2CppTypeInfo
                {
                    KlassId = new ClassId { Address = classPtr },
                    StaticFieldsAddress = staticFieldsAddr
                };
                result.Fields.AddRange(fields);

                _logger?.LogDebug("[BinaryTypeInfoProvider] Resolved {Name}: classPtr=0x{Class:X16}, staticFields=0x{SF:X16}, fields={Count}",
                    il2cppName, classPtr, staticFieldsAddr, result.Fields.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[BinaryTypeInfoProvider] Failed to resolve type: {Name}", managedType.FullName);
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

            _logger?.LogInformation("[BinaryTypeInfoProvider] Loading game binary metadata from {Path}", gasmPath);
            Loader loader = new();
            loader.Init(gasmPath, metadataPath);
            _typeModel = new TypeModel(loader);
            BuildMaps();
            _logger?.LogInformation("[BinaryTypeInfoProvider] Ready: {TypeCount} types, {UsageCount} MetadataUsages",
                _typeNameToIndex!.Count, _typeIndexToMetadataSlot!.Count);
        }

        private void BuildMaps()
        {
            _typeIndexToMetadataSlot = new Dictionary<int, uint>();
            _typeNameToIndex = new Dictionary<string, int>(StringComparer.Ordinal);

            var metadata = _typeModel!.Metadata;

            // Invert metadataUsageDic: typeIndex → destIndex in MetadataUsages[]
            if (metadata.metadataUsageDic.TryGetValue(Il2CppMetadataUsage.kIl2CppMetadataUsageTypeInfo, out var usageDic))
            {
                foreach (var pair in usageDic)
                    _typeIndexToMetadataSlot[(int)pair.Value] = pair.Key;
            }

            // Build fullName → typeIndex using raw metadata strings
            for (int i = 0; i < metadata.typeDefs.Length; i++)
            {
                var td = metadata.typeDefs[i];
                string ns = metadata.GetStringFromIndex(td.namespaceIndex);
                string name = metadata.GetStringFromIndex(td.nameIndex);
                string fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
                // Last write wins for duplicates (nested types may share simple names)
                _typeNameToIndex[fullName] = i;
            }
        }

        private ulong GetAslrSlide(Il2CsRuntimeContext runtime)
        {
            // Re-compute per call — slide changes when game restarts (new process)
            foreach (System.Diagnostics.ProcessModule mod in runtime.TargetProcess.Modules)
            {
                if (string.Equals(mod.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase))
                {
                    ulong moduleBase = (ulong)(long)mod.BaseAddress;
                    ulong imageBase = _typeModel!.Il2Cpp.ImageBase;
                    ulong slide = moduleBase - imageBase;
                    _logger?.LogDebug("[BinaryTypeInfoProvider] ASLR slide: 0x{Slide:X16} (base=0x{Base:X16}, preferred=0x{Image:X16})",
                        slide, moduleBase, imageBase);
                    return slide;
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
                // Use raw metadata name (not the cleaned-up name) to match what gRPC would return
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

        private static string GetIl2CppTypeName(Type managedType)
        {
            // Skip nested types — IL2CPP metadata stores them differently
            if (managedType.DeclaringType != null)
                return null;
            string ns = managedType.Namespace;
            string name = managedType.Name;
            // Strip generic arity suffix (e.g. "List`1" → "List")
            int backtick = name.IndexOf('`');
            if (backtick >= 0) name = name.Substring(0, backtick);
            return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        }
    }
}
