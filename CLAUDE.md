# CLAUDE.md — Raid Toolkit SDK

This document covers architecture, build instructions, the account-dump JSON schema, and security audit findings for this fork (`asultan80/raid-toolkit-sdk`).

## Orientation

**What this is:** A Windows background service that attaches to the running RAID: Shadow Legends (Plarium Play) process and reads its IL2CPP memory. It exposes a local WebSocket API so tools can query the full account state — heroes, artifacts, great hall, arena, etc. — without having to deal with memory addresses or game updates themselves.

**Current stable branch:** `release/v2.8` (latest tag `v2.8.22.23238`, last commit 2024-02-10)
**In-progress:** `main` targets v3.0-unstable. Structural restructuring, not production-ready.
**Platform:** Windows only (WinUI3, Win32 P/Invoke, IL2CPP tooling). Cannot build on macOS/Linux.

## Project Map (`src/`)

| Project | Role |
|---|---|
| `Application/Raid.Toolkit` | WinUI3 tray app — the main executable |
| `Application/Raid.Toolkit.Application.Core` | DI host, commands, WebSocket setup, update service |
| `Application/Launcher` | Tiny WinForms launcher that checks .NET prereqs then starts the main app |
| `Shared/Common` | Utilities: `RegistrySettings`, `HttpClientExtensions`, `GitHub/Updater` |
| `Shared/DataModel` | **Public DTOs, enums, WebSocket message format, API contracts** — key folder for schema |
| `Shared/Extensibility*` | Plugin/extension infra: account manager, persisted storage, WebSocket dispatcher |
| `Shared/Model` | IL2CPP-generated assembly glue; `ModelLoader.cs` and `PlariumPlayAdapter.cs` |
| `Extensions/Account` | Account data extraction extension — `AccountApi`, `Extractor.cs`, `StaticDataApi` |
| `Extensions/Realtime` | Live in-battle telemetry |
| `ClientSDK/DotNet` | `Raid.Client` NuGet package |
| `ClientSDK/Node` | `@raid-toolkit/webclient` npm package |
| `ClientSDK/Python` | `raidtoolkit` PyPI package |
| `Setup/` | WiX installer → `RaidToolkitSetup.exe` |

## Build (Windows only)

```
# Requires Visual Studio 2019+ or .NET 6 SDK + WinAppSDK build tools
msbuild SDK.sln -t:"Build;Pack" -p:Configuration=Release -p:Platform=x64
```

Or just download `RaidToolkitSetup.exe` from [GitHub Releases](https://github.com/raid-toolkit/raid-toolkit-sdk/releases) — that is the easiest path for end users.

`buildext.bat` — brute-force clean + rebuild for extension development.

## WebSocket API

**Endpoint:** `ws://localhost:9090` (the README/NITTYGRITTY says `wss://` but the actual code uses plain `ws://`)

**Wire format** (`src/Shared/DataModel/SocketMessage.cs`): JSON 3-element array `[scope, channel, message]`

**Channels:** `call`, `get`, `sub`, `unsub` (client→server); `set-promise`, `send-event` (server→client)

**Call message:** `{ promiseId, methodName, parameters: [...] }`

### Key scopes and methods

**`account-api`** (`IAccountApi.cs`):
- `getAccounts()` → `Account[]`
- `getAccountDump(accountId)` → `AccountDump` — **this is the main extraction endpoint**
- `getHeroes(accountId, snapshot=false)` → full hero DTOs (includes awaken/blessing, not in dump)
- `getArtifacts(accountId)` → full artifact DTOs (includes ascendLevel/ascendBonus, not in dump)
- `getArena(accountId)` → arena + great hall
- `getAllResources(accountId)` → shards, market

**`static-data`**: champion data, skill data, set definitions, localized strings

**`realtime-api`**: last battle response, current view

## Account Dump JSON Schema (Legacy Format)

This is what `getAccountDump(accountId)` returns. File: `src/Shared/DataModel/GameData/AccountDumpClient.cs`.

### Top-level `AccountDump`

```json
{
  "fileVersion": "1.3",
  "id": "...",
  "name": "PlayerName",
  "lastUpdated": "2024-01-15T12:00:00.0000000Z",
  "arenaLeague": "GoldIV",
  "artifacts": [...],
  "heroes": [...],
  "greatHall": { "magic": { "health": 3 }, "force": { ... }, ... },
  "factionGuardians": { "BannerLords": { "Epic": 2 }, ... },
  "shards": { "Mystery": { "count": 12 }, ... },
  "stagePresets": { "1": [12345, 67890], ... }
}
```

Note: no clan data, no blessing data, no champion skin data in this format.

### `Hero` fields

`id` int, `typeId` int, `grade` string ("Stars6"), `level` int, `empowerLevel` int, `experience` int, `fullExperience` int, `locked` bool, `inStorage` bool (= in vault), `isGuardian` bool, `marker` string ("None"/"Food"/"Frequent"/"Max"), `artifacts` int[] (equipped artifact IDs), `fraction` string-enum, `rarity` string-enum, `role` string-enum, `element` string-enum, `awakenLevel` int (0–6), `name` string, base stats: `health` double (×15!), `accuracy`, `attack`, `defense`, `criticalChance`, `criticalDamage`, `criticalHeal`, `resistance`, `speed`, `masteries` int[] (MasteryKindId values), `assignedMasteryScrolls` / `unassignedMasteryScrolls` / `totalMasteryScrolls` `{string: int}`, `skills` `Skill[]`.

**Warning:** `health` is raw (multiplied by 15 internally by Extractor.cs:111). Divide by 15 to get the stat you see in-game... actually the game stores it this way natively. The value in the dump IS the in-game HP shown.

### `Skill` fields

`id` int, `typeId` int, `level` int

### `Artifact` fields

`id` int, `sellPrice` int, `price` int, `level` int (0–16), `isActivated` bool, `kind` string (slot name), `rank` string ("One"…"Six"), `rarity` string, `setKind` string (e.g. "LifeSet", "OffenseSet", "BadenSet"), `isSeen` bool, `failedUpgrades` int, `requiredFraction` string (hero faction or "None"), `primaryBonus` ArtifactBonus, `secondaryBonuses` ArtifactBonus[]

### `ArtifactBonus` fields

`kind` string (stat name), `isAbsolute` bool (flat if true, % if false), `value` double, `enhancement` double (glyph bonus), `level` int (substat upgrade count, 0–4)

### Enum values

**Artifact slots (`kind`):** `Weapon`, `Helmet`, `Chest`, `Gloves`, `Boots`, `Shield`, `Ring`, `Cloak`, `Banner`, `UnknownArtifact`, `UnknownAccessory`

**Stats (`kind` in bonuses):** `Health`, `Attack`, `Defense`, `Speed`, `Resistance`, `Accuracy`, `CriticalChance`, `CriticalDamage`, `CriticalHeal`

**Elements:** `Magic`, `Force`, `Spirit`, `Void`

**Factions:** `BannerLords`, `HighElves`, `SacredOrder`, `CovenOfMagi`, `OgrynTribes`, `LizardMen`, `Skinwalkers`, `Orcs`, `Demonspawn`, `UndeadHordes`, `DarkElves`, `KnightsRevenant`, `Barbarians`, `NyresanElves`, `Samurai`, `Dwarves`

**Rarity:** `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythic`

**Artifact Rarity:** `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical` (note: Mythical for artifacts vs Mythic for heroes)

**Artifact Rank:** `One`, `Two`, `Three`, `Four`, `Five`, `Six`

## Triggering a Dump

Once RTK is installed and running on Windows:

1. **Tray → Accounts → Dump button** — prompts "Use legacy format?" → click Yes → Save dialog. Target: this JSON file.
2. **WebSocket programmatically** — connect to `ws://localhost:9090`, call `account-api.getAccountDump(accountId)`. See DEVELOPERS.md for Python/TS/C# examples.

There is no auto-export to a fixed file. Persisted internal state lives in `%LOCALAPPDATA%\RaidToolkit\data\` but is in an opaque per-extension format — don't parse it directly.

## How the IL2CPP auto-update works

On startup, RTK reads `%PlariumPlay%\gamestorage.gsfn` to find the installed game version, then looks for a locally-cached compiled DLL (`<outDir>/<gameVersion>/Raid.Toolkit.Interop.dll`). If the game version changed or the DLL is stale, it reads `GameAssembly.dll` + `global-metadata.dat` from the game installation and generates a new .NET assembly using Il2CppToolkit. This assembly is loaded with `Assembly.LoadFrom(dllPath)` — loading from local disk only, not downloaded.

The *RTK application itself* auto-updates via GitHub Releases API (see Security section below).

---

## Security Audit (release/v2.8, audited 2026-05-24)

### 🔴 FINDING: WebSocket binds to ALL interfaces (`0.0.0.0:9090`)

**File:** `src/Application/Raid.Toolkit.Application.Core/appsettings.json:22`
```json
"listeners": [{ "ip": "Any", "port": 9090 }]
```

The WebSocket server listens on **all network interfaces** — not just loopback. This means any device on your local network (home Wi-Fi, coffee shop, VPN peers) can connect to port 9090 and read your full account data, call any API method, and (via extensions) potentially trigger actions.

**Fix:** Change `"ip": "Any"` to `"ip": "127.0.0.1"` in both `appsettings.json` files. There is **no auth token** on the WebSocket, so localhost binding is the only access control.

This is a fork improvement opportunity — upstream has the same bug. A PR to upstream would benefit the whole community.

### 🟡 FINDING: Auto-update has no cryptographic signature check

**File:** `src/Shared/Extensibility.Host/Services/UpdateService.cs:101-125`

The update flow: polls GitHub Releases API (HTTPS) → downloads `RaidToolkitSetup.exe` into `%TEMP%` → runs it with `Process.Start(tempDownload, "/update ...")`. There is no hash verification or code-signing check on the downloaded installer.

The risk is mitigated by: HTTPS (no MITM without a trusted cert), GitHub's release infrastructure, and the update repo is configurable in registry (`HKCU\SOFTWARE\RaidToolkit\Repository`, defaults to `raid-toolkit/raid-toolkit-sdk`). If someone tampered with the registry setting they could redirect updates.

**Improvement:** Verify the GitHub release asset SHA256 against a published checksum, or check the PE code-signing certificate on the downloaded exe before launching.

### 🟢 Network calls — only GitHub API and raidtoolkit.com avatars

All outbound calls:
- `https://api.github.com/repos/raid-toolkit/raid-toolkit-sdk/releases` — update check
- `https://raidtoolkit.com/img/avatars/<id>.png` — avatar image URL embedded in AccountInfo (cosmetic only, fetched by the client UI)

No telemetry, no analytics, no Plarium API calls, no game-side exfiltration.

### 🟢 No credential or browser data access

Grepped for `password`, `cookie`, `keychain`, `chrome`, `firefox`, `browser` — zero hits in functional code. The only `%APPDATA%` access is to `RaidToolkit`'s own data directory and Plarium Play's install path.

### 🟢 Registry access is scoped

Reads/writes only:
- `HKCU\SOFTWARE\RaidToolkit` — RTK's own settings
- `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` — startup registration (user-opt-in)
- `HKU\.DEFAULT\SOFTWARE\PlariumPlayInstaller` — reads Plarium Play install path only

No access to credential stores, browser data, or other applications.

### 🟢 Assembly loading is local-only

`Assembly.LoadFrom(dllPath)` at `ModelLoader.cs:95` loads from a path derived from the local game installation. The DLL is built from the game's own IL2CPP metadata on the user's machine — not downloaded. No arbitrary remote code execution path exists here.

### 🟢 Dependencies are clean

NuGet: all Microsoft.*, CommunityToolkit.*, Newtonsoft.Json, SuperSocket (known), il2cpptoolkit (same org — open source at github.com/nickel-lang/il2cppToolkit), CommandLineParser, Karambolo (file logging). No typosquats or mystery packages.

npm (`@raid-toolkit/webclient` deps): `@remote-ioc/runtime` and `@remote-ioc/ws-router` are maintained by `dnchattan@gmail.com` — the same author as the main package. Not a supply-chain risk.

### 🟢 No obfuscation

Source is straightforward C#/TypeScript. No base64 blobs, no encoded URLs, no P/Invoke into hooking APIs beyond what WinUI3 requires (user32.dll for window management, shell32.dll for file association — both standard).

---

## Priority Improvements

1. **Fix `"ip": "Any"` → `"ip": "127.0.0.1"`** in both appsettings.json files — security fix, high priority
2. **Add checksum verification** in UpdateService before running the downloaded installer
3. **Export command** — add a `--dump-account <outputFile>` CLI flag so the tray UI isn't required
4. **Awaken/blessing in dump** — `Extractor.cs` doesn't populate `awakenRank`, `blessing`, or artifact `ascendLevel` even though the DTOs have the fields; fix for v2.8 parity with actual game data
5. **Claude.ai export script** — see `../` (the RAID-Helper parent project) for the Mac-side analysis tool
