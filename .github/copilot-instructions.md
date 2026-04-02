# ImprovedGarrisons — Copilot Instructions

## Project Overview

Bannerlord mod that improves castle and settlement garrison management. Adds automatic troop recruitment from nearby villages, prisoner recruitment, garrison troop training/upgrading, and guard parties that patrol and defend the player's fiefs. Uses a `CampaignBehavior` driven by the daily settlement tick with per-fief configurable settings.

## Tech Stack

- **Language:** C# 9.0 targeting .NET Framework 4.7.2
- **Game SDK:** TaleWorlds Mount & Blade II: Bannerlord (`TaleWorlds.Core`, `TaleWorlds.CampaignSystem`, `TaleWorlds.Library`, `TaleWorlds.MountAndBlade`, `TaleWorlds.Localization`, `TaleWorlds.ObjectSystem`, `TaleWorlds.InputSystem`)
- **Patching:** Harmony 2.2.2 for runtime method patching
- **Testing:** xUnit 2.6.6
- **Nullable:** Disabled project-wide

## Build, Test & Deploy Commands

```powershell
# Build
dotnet build src\ImprovedGarrisons\ImprovedGarrisons.csproj -c Release

# Run tests
dotnet test tests\ImprovedGarrisons.Tests\ImprovedGarrisons.Tests.csproj

# Deploy to game
./deploy.ps1
```

Both the main project and the test project depend on the `GameFolder` MSBuild property pointing at a local Bannerlord installation. Keep that path valid before building or testing.

## Architecture

| File | Role |
|------|------|
| `SubModule.cs` | Mod entry point — applies Harmony patches, registers `ImprovedGarrisonsCampaignBehavior` for campaigns, unpatches on unload |
| `ImprovedGarrisonsCampaignBehavior.cs` | `CampaignBehaviorBase` — listens to `DailyTickSettlementEvent`, processes recruitment/training/guard parties per player-owned fief, persists per-fief settings via `SyncData` |
| `GarrisonManager.cs` | Stateless core logic — village recruitment, prisoner recruitment, garrison training/upgrading, guard party refill calculations |
| `GarrisonSettings.cs` | Per-fief configuration POCO — toggles for auto-recruit, prisoner recruit, training, guard parties; thresholds for garrison size and guard party size |
| `GuardPartyComponent.cs` | Custom `PartyComponent` for guard parties — provides owner, name, and home settlement |
| `GuardPartyManager.cs` | Guard party lifecycle — creation, refill from garrison, patrol behavior, disbanding |

### Key Design Decisions

- **Daily tick driven:** All garrison management runs once per day via `CampaignEvents.DailyTickSettlementEvent`, keeping performance impact low.
- **Per-fief settings:** Each town and castle gets its own `GarrisonSettings` instance keyed by `Settlement.StringId`. Settings are serialized with the save game through `SyncData`.
- **Stateless core logic:** `GarrisonManager` methods are `internal static` with no shared state, making them directly testable without the game runtime.
- **Recruitment threshold:** Auto-recruitment stops when the garrison reaches the configured threshold (default 100). Guard party refill only draws from troops above half the threshold.
- **Guard parties use custom PartyComponent:** `GuardPartyComponent` extends `PartyComponent` to provide Bannerlord with the party's owner, name, and home settlement while keeping the guard party distinct from clan or caravan parties.
- **Player-only scope:** `IsPlayerFief` checks `settlement.OwnerClan == Clan.PlayerClan` to restrict all behavior to the player's own fiefs.
- **User-visible feedback:** Recruitment, training, and guard party actions display colored messages via `InformationManager.DisplayMessage`.

## Code Conventions

- **Namespace:** `ImprovedGarrisons` for all classes (flat namespace)
- **XML documentation:** Public and internal types and methods must have `<summary>`, `<param>`, and `<returns>` documentation
- **Patch classes:** Harmony patches are `internal static` classes with small `Prefix` or `Postfix` methods, one responsibility per file, placed in a `Patches/` subfolder if added
- **Campaign-only logic:** Guard campaign-specific operations with `game.GameType is Campaign` before casting to `CampaignGameStarter`
- **Null-safe game access:** Always null-check `settlement?.Town`, `settlement?.Town?.GarrisonParty`, and roster elements before accessing counts or characters
- **Hero exclusion:** Skip `element.Character.IsHero` in all roster iteration to avoid moving hero characters between rosters
- **Error handling:** Catch exceptions close to the Bannerlord API boundary and surface readable messages to the player through `InformationManager.DisplayMessage`

## Module Metadata

`Module/SubModule.xml` defines the Bannerlord module:

- **Name:** `Improved Garrisons`
- **Id:** `ImprovedGarrisons`
- **Dependencies:** `Native`, `SandBoxCore`, `Sandbox`
- **Entry point:** `ImprovedGarrisons.SubModule`

Keep `Module/SubModule.xml` aligned with any assembly, namespace, or entry point changes.

## Post-Change Workflow

After making code changes, follow this order:

1. **Build:** `dotnet build src\ImprovedGarrisons\ImprovedGarrisons.csproj -c Release`
2. **Write or update tests:** Cover each changed behavior path with targeted xUnit tests
3. **Test:** `dotnet test tests\ImprovedGarrisons.Tests\ImprovedGarrisons.Tests.csproj`
4. **Deploy:** `./deploy.ps1`

Do not deploy if the build fails or tests are failing.

## Testing Guidelines

- Tests rely on `InternalsVisibleTo` so they can call `internal` methods directly
- `GarrisonManager.CalculateGuardPartyRefill` is a pure function — test with `[Theory]`/`[InlineData]` for boundary conditions
- Methods that require `Settlement`/`Town`/`MobileParty` objects are tested for null-safety (verify they return 0 or empty results without the game runtime)
- Keep tests small and behavior-focused: one responsibility per test method
- Avoid reproducing the full Bannerlord runtime; test logic boundaries and edge cases through the stateless `GarrisonManager` methods
