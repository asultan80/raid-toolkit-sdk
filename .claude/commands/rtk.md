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

## Build

```
msbuild SDK.sln -t:"Build;Pack" -p:Configuration=Release -p:Platform=x64
```

CI: `.github/workflows/app-publish.yml` triggers on push to `release/**` or `main` when source paths change. Creates a GitHub Release with `RaidToolkitSetup.exe` automatically.

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
