# Release Notes

## 2.9.x

* Fixed RTK startup hang — ModelLoader version check now reads .NET assembly version (AssemblyName.GetAssemblyName) instead of PE file version (FileVersionInfo); generated Interop DLLs embed only assembly version, causing FileVersionInfo to always return 0.0.0.0 and triggering a 30–60 minute rebuild on every launch
* Fixed CS0433 build error — Raid.Interop.dll (game build 150352) embeds Newtonsoft.Json types including JObject, conflicting with Newtonsoft.Json package; resolved with extern alias RaidInterop in Extensibility.Host.csproj; removed dead using static RoutingTable from AccountReaderWriter.cs

* Fixed LoadedTypes crash (ReflectionTypeLoadException) when Raid.Interop.dll contains types with unsatisfiable generic constraints or Newtonsoft.Json version mismatches — assembly.GetTypes() now catches ReflectionTypeLoadException and uses successfully-loaded types via ex.Types
* Added diagnostic: scan Il2CppClass offsets 152–200 with non-zero values logged to find correct static_fields offset in v31; classPtr name verified from memory
* Added diagnostic: GENERICINST resolution logs VA, slide, and computed runtime address at Warning level; ReadMemory errors include the failing address and handle value
* Added diagnostic: OpenProcess failure now throws immediately with Win32 error code (5=ACCESS_DENIED means RAID runs elevated; run RTK as admin) instead of a silent RT2 cascade; RT2 structured exceptions now include stack traces in logs
* Fixed BinaryTypeInfoProvider crash in v27+ metadata (NullReferenceException on null metadataUsageDic) — added separate v27+ code path that builds type name index from il2cpp.Types[] instead of metadataUsageDic; for GENERICINST types resolves Il2CppClass* via Il2CppGenericClass.cached_class (offset 24) at runtime; corrected static_fields offset to 184 (0xB8) — IL2CPP v31 (Unity 2022.3) adds a klass self-reference pointer at +120 shifting static_fields from 0xB0 to 0xB8; confirmed via offset scan of live Il2CppClass in memory

* Added support for IL2CPP metadata version 31 (required for current RAID: Shadow Legends game update)
* Fixed assembly version mismatch on launch caused by il2cpptoolkit DLLs not being included in the installer
* Fixed multiple RTK instances launching when a broken/stale instance was already running
* Fixed model rebuild crash (OverflowException in PE binary scanner) when game binary layout changes
* Fixed PELoader fallback failing with Win32 error 126 — GameAssembly.dll dependencies now resolved from game directory
* Fixed "Can't use auto mode" error — corrected IL2CPP code registration pointer offset for metadata version >= 29.1 (v31)
* Fixed compiler crash (IndexOutOfRangeException) — Il2CppMethodDefinition.returnParameterToken must be field 4 (between returnType and parameterStart) per metadata v31 binary layout
* Fixed accounts not loading — AccountManager now catches up with game instances that started before it subscribed to the OnAdded event (race condition on startup)
* Fixed null Logger in ProcessManager (constructor was not assigning the injected logger)
* Added diagnostic logging to account loading path and process detection for easier troubleshooting
* Added logging and error handling to GameInstanceManager.AddInstance to surface silent failures in InitializeOrThrow and OnAdded
* Fixed infinite hang in gRPC type/method calls — added 10-second deadline to all InjectionClient gRPC calls (Il2CppTypeCache, Il2CppTypeInfoLookup); without a deadline the calls block forever when the injection host inside RAID doesn't respond
* Added binary metadata bypass (BinaryTypeInfoProvider) — when gRPC type resolution fails (RpcException/DeadlineExceeded), RTK now falls back to reading type/field info directly from GameAssembly.dll and global-metadata.dat; resolves class pointers via MetadataUsages slots with ASLR adjustment, reads static_fields pointer from Il2CppClass at runtime; accounts now load even when the injection host DLL is incompatible with the current game build
* Fixed binary fallback failing for generic types (e.g. SingleInstance&lt;AppModel&gt;) — BuildMaps now iterates MetadataUsages entries using TypeModel.GetTypeName for canonical names (handles GENERICINST correctly); GetIl2CppTypeName now mirrors TypeModel's naming format (namespace only on outermost type, no namespace on generic args); fallback is tried before gRPC to avoid 10-second timeout per type
* Fixed StaticFieldsAddress=0 being permanently cached when RTK connects before RAID initializes static fields — cache entry is evicted and retried on next poll
* Fixed gRPC DeadlineExceeded on primitive/enum types (e.g. Int64, bool) — binary fallback or early return for IsPrimitive/IsEnum types skips unnecessary gRPC call
* Fixed NullReferenceException in StringFactory — System.String type info now resolved from binary metadata (typedef scan) instead of falling back to gRPC
* Fixed IOException in DbgLog on hot paths — File.AppendAllText now wrapped in try/catch to suppress file contention errors from concurrent threads
* Fixed ArtifactExtension MissingMethodException — ExternalArtifactsStorage._state removed in game build 150352; GetMigratedArtifacts now returns empty array (migrated storage path unavailable); NoInlining wrapper preserved so catch remains safe if path is re-enabled
* Fixed Account extension compilation against game build 150352 — removed references to DestroyStatsParams.MaxDestructionPercentFormula, InitialState.DamageTaken, HeroType.Element, HeroType.LeaderSkill, and ChangeEffectLifetimeParams.Turns which were removed from the game model
* Fixed ArtifactsProviderState field names — UserArtifactData.NextArtifactId/NextArtifactRevisionId renamed to LastArtifactId/LastArtifactRevisionId in game build 150352; incremental update check and MarkRefresh now use the correct field names
* Updated Raid.Interop.dll reference assembly for game build 150352
* Fixed gRPC DeadlineExceeded on Il2CppToolkit.Runtime wrapper types (e.g. Native__Dictionary<K,V>) — these construct via IRuntimeObject and don't need gRPC field offsets; now skipped alongside generated/primitive/enum types
* Fixed StaticTypesExtension permanently skipping after first failure — HasWork=false is now only set once all static data files are confirmed written; previously a single gRPC timeout on startup locked out all static data (hero types, skills, artifacts, etc.) forever until RTK restart
* Fixed StaticResources.AddStrings throwing ArgumentException on second call — dictionary used Add() which throws on duplicate keys when localization strings are re-added on each tick; changed to indexer assignment (dict[key] = value) which silently overwrites
* Fixed HeroesExtension blocking on hero-types.static.json — hero-types require StaticHeroData via gRPC (CLASS type, no MetadataUsages entry) which always times out; HeroesExtension now proceeds with null heroType when static data unavailable; Hero.Name falls back to TypeId string
* Fixed artifacts always empty in game build 150352 — ExternalArtifactsStorage._state was removed but artifacts moved to _cachedArtifacts._artifacts (not back to ArtifactData.Artifacts as assumed); GetMigratedArtifacts now reads storage._cachedArtifacts._artifacts

## 2.8.x

* Hero equipped artifacts will now update when removed, and will not show as equipped on multiple heroes.

## 2.7.x

* Stabilization and bugfixes

## 2.6.x

* Stabilization and bugfixes

## 2.5.x

* Added VSIX extension template to SDK build
* Updated to Il2CppToolkit 2.0.115-alpha, which adds "(Hooked)" to the window title for hooked processes

## 2.4.x

* #117 - Add account management UX for exporting/importing account information

## 2.3.x

* #116 - Add missing fields:

    > `Raid.Toolkit.DataModel.Hero`:
    >
    > * `AwakenRank` (`awakenRank`)
    > * `Blessing` (`blessing`)
    > * `FreeBlessingResetUsed` (`blessingResetUsed`)
    >
    > `Raid.Toolkit.DataModel.HeroType`:
    > * `ShortName`
    >
    > `Raid.Toolkit.DataModel.Artifact`:
    > * `AscendLevel` (`ascendLevel`)
    > * `AscendBonus` (`ascendBonus`)
    >

* #115 - Settings were not loaded if `--no-webservice` specified, which prevented background services from running in debug process
* `--debug` switch will now use an alternative gold icon in the taskbar to differentiate from other normal RTK processes.

## 2.2.x

* #114 - removed usage of get_ValueCap in skills data which for some reason seems to be missing from actual gasm dll.
* #109, #112 - Updated window APIs to allow for both WinForms and WinUI windows to be managed by RTK and provide lighter weight usage patterns.
* #111 - Handle extension re-installation
* #110 - Update logo graphics

## 2.1.x

* #107 - Changed account resources to return raw value rather than prematurely rounding. This was causing issues for some tools which use things like # of keys to determine whether there are enough resources.

## 2.0.x

* Introduced standalone installer which will install required .net dependencies and check for compatibility issues
* Adopted WinUI as default UI provider (old winforms UI can be used by launching with the `--render-engine WinForms` argument)
