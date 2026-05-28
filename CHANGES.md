# Release Notes

## 2.9.x

* Fixed BinaryTypeInfoProvider crash in v27+ metadata (NullReferenceException on null metadataUsageDic) — added separate v27+ code path that builds type name index from il2cpp.Types[] instead of metadataUsageDic; for GENERICINST types resolves Il2CppClass* via Il2CppGenericClass.cached_class (offset 24) at runtime; corrected static_fields offset from 96 to 176 (0xB0)

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
