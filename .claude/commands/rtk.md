# RTK — Raid Toolkit SDK

Use this skill when working on the Raid Toolkit SDK source code.

## Key facts

- **Stable branch:** `release/v2.8` — build and ship from here
- **In progress:** `main` — v3.0 restructuring, not production-ready
- **Platform:** Windows only (WinUI3, Win32, IL2CPP). Cannot build on macOS/Linux.
- **Full docs:** See `CLAUDE.md` in the repo root — architecture, JSON schema, security audit, API reference

## Project layout

| Path | What it is |
|---|---|
| `src/Application/Raid.Toolkit/` | WinUI3 tray app — main entry point |
| `src/Application/Raid.Toolkit.Application.Core/` | DI host, WebSocket setup, update service |
| `src/Shared/DataModel/` | DTOs, enums, WebSocket message format, API contracts |
| `src/Shared/Model/` | IL2CPP glue — `ModelLoader.cs`, `PlariumPlayAdapter.cs` |
| `src/Extensions/Account/` | Account extraction — `AccountApi.cs`, `Extractor.cs` |
| `src/Shared/Common/GitHub/` | Auto-update logic |
| `src/Setup/` | WiX installer → `RaidToolkitSetup.exe` |

## Build (Windows only)

```
msbuild SDK.sln -t:"Build;Pack" -p:Configuration=Release -p:Platform=x64
```

## Checking CI status

Use `WebFetch` on these GitHub HTML pages (not the API — no auth needed):

```
# All recent runs
WebFetch: https://github.com/asultan80/raid-toolkit-sdk/actions
prompt: "List runs — name, status, branch, commit SHA, started how long ago"

# Specific workflow pass/fail
WebFetch: https://github.com/asultan80/raid-toolkit-sdk/actions/workflows/app-publish.yml
prompt: "Show run number, final status (success/failure), branch, commit SHA, duration"

# Confirm release published
WebFetch: https://github.com/asultan80/raid-toolkit-sdk/releases
prompt: "List top 3 releases — tag, date, asset file names"

# Specific release assets
WebFetch: https://github.com/asultan80/raid-toolkit-sdk/releases/tag/<version>
prompt: "List all release assets (file names and sizes)"
```

## CI / Releasing

**No manual tags needed.** Version auto-calculated by Nerdbank.GitVersioning (`version.json` major.minor + commit count).

To trigger a release: push any source change to `release/**` or `main`. Doc-only commits (`*.md`) are excluded from the trigger.

To force a release with no other changes: bump `version.json`.

CI workflow: `.github/workflows/app-publish.yml`
- Builds on `windows-latest`
- Produces `RaidToolkitSetup.exe` (WiX installer) + `Raid.Toolkit.exe`
- Creates a GitHub Release automatically
- NuGet/npm/PyPI publish steps removed (no secrets configured in this fork)

### Key CI fixes in this fork (do not revert)

| What | Why |
|---|---|
| Removed `mickem/clean-after-action@v1` | Repo deleted upstream — caused "Set up job" failure |
| Replaced `iamtheyammer/branch-env-vars@v1.0.4` | Personal repo action gone — now inline PowerShell |
| All actions bumped to current versions | v1/v2 deprecated and removed by GitHub |
| Added `permissions: contents: write` | Required for release creation — missing = 403 |
| Added `fail_on_unmatched_files: false` | VSIX not always present — without this, release step errors |

## Installing the release on Windows

Prerequisites:
- Windows 10 build 19041+ or Windows 11
- .NET 6 Desktop Runtime: `winget install Microsoft.DotNet.DesktopRuntime.6`
- Windows App SDK 1.4: `winget install Microsoft.WindowsAppRuntime.1.4`
- Plarium Play + RAID installed

Steps:
1. Download `RaidToolkitSetup.exe` from [releases](https://github.com/asultan80/raid-toolkit-sdk/releases/latest)
2. Right-click → **Run as administrator**
3. SmartScreen prompt → **More info → Run anyway** (build is unsigned)
4. Launch RAID via Plarium Play, wait for lobby
5. RTK tray icon should appear

## Debugging — always start here

### 1. Read the RTK log (most useful)

```powershell
# List log files, newest first
Get-ChildItem "$env:LOCALAPPDATA\RaidToolkit\Logs" | Sort-Object LastWriteTime -Descending | Select-Object -First 3

# Read last 80 lines of the latest non-empty log
$log = Get-ChildItem "$env:LOCALAPPDATA\RaidToolkit\Logs" | Sort-Object LastWriteTime -Descending | Where-Object { $_.Length -gt 0 } | Select-Object -First 1
Get-Content $log.FullName | Select-Object -Last 80
```

Log location: `%LOCALAPPDATA%\RaidToolkit\Logs\YYYYMMDD-000.log`

If the log file is **0 bytes**: the app crashed before the logger initialized — go to step 2.

### 2. Check Windows Event Log for crash/hang

```powershell
Get-WinEvent -LogName Application -MaxEvents 200 |
  Where-Object { $_.TimeCreated -gt (Get-Date).AddHours(-1) -and
    ($_.LevelDisplayName -eq "Error" -or $_.Id -in 1000,1002,1026) } |
  Format-List TimeCreated, Id, LevelDisplayName, Message
```

### 3. Check installed DLL versions match

```powershell
Get-ChildItem "$env:LOCALAPPDATA\RaidToolkit\bin" -Filter "Il2CppToolkit*.dll" |
  ForEach-Object { $v = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName); "$($_.Name) — $($v.FileVersion)" }
# All Il2CppToolkit.*.dll should match Raid.Model.dll version
[System.Diagnostics.FileVersionInfo]::GetVersionInfo("$env:LOCALAPPDATA\RaidToolkit\bin\Raid.Model.dll").FileVersion
```

Mismatch = version mismatch bug (old NuGet DLLs in installer). Fix: switch csproj from PackageReference to ProjectReference for il2cpptoolkit projects.

### 4. Check if app is running (not crashed, just silent)

```powershell
Get-Process | Where-Object { $_.Name -match "Raid|Launcher" } | Select-Object Name, Id, CPU, WorkingSet, StartTime
```

## Diagnosing launch failures — read logs first

```powershell
# RTK log
Get-ChildItem "$env:LOCALAPPDATA\RaidToolkit\Logs" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content | Select-Object -Last 50

# Windows crash events
Get-WinEvent -LogName Application -MaxEvents 50 | Where-Object { $_.LevelDisplayName -eq "Error" } | Select-Object -First 5 | Format-List

# Check prerequisites
Get-AppxPackage -Name "Microsoft.WindowsAppRuntime*" | Select-Object Name, Version
dotnet --list-runtimes | Select-String "Microsoft.WindowsDesktop"
Get-ItemProperty "Registry::HKEY_USERS\.DEFAULT\SOFTWARE\PlariumPlayInstaller" -ErrorAction SilentlyContinue
```

## WebSocket API (`ws://localhost:9090`)

Wire format: `[scope, channel, message]` (3-element JSON array).

Key methods on `account-api` scope:
- `getAccounts()` → account list
- `getAccountDump(accountId)` → full legacy dump (the main extraction endpoint)
- `getHeroes(accountId)` / `getArtifacts(accountId)` → richer DTOs with awaken/blessing/ascend fields

Full schema in `CLAUDE.md`.

## Known issues / open improvements

1. `Extractor.cs` does not populate `awakenRank`, `blessing`, or artifact `ascendLevel` in the legacy dump even though the DTOs have these fields
2. No `--dump-account <file>` CLI flag — requires the tray UI to export
3. Auto-update (`UpdateService.cs`) downloads and runs `RaidToolkitSetup.exe` without verifying a cryptographic signature

## Security notes

- WebSocket binds to `127.0.0.1` in this fork (fixed from upstream's `Any`/`0.0.0.0`)
- No telemetry, no credential access, no browser data access — verified in audit (see `CLAUDE.md`)
- IL2CPP assembly generation reads only local game files, no remote code loading
