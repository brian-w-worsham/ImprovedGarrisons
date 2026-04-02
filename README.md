# Improved Garrisons

A Mount & Blade II: Bannerlord mod that improves the management of your castles and settlements. Adds automatic recruitment, garrison training, prisoner recruitment, and guard parties with custom AI to defend your properties.

Inspired by the [Improved Garrisons](https://www.nexusmods.com/mountandblade2bannerlord/mods/688) mod by Sidies.

## Features

- **Auto-Recruitment** — Automatically recruits troops from nearby villages into your garrison up to a configurable threshold
- **Prisoner Recruitment** — Converts prisoners in your settlement into garrison troops over time
- **Garrison Training** — Automatically upgrades garrisoned troops to higher tiers when they have enough XP
- **Guard Parties** — Spawns guard parties from your garrison that patrol and defend your settlement's region
- **Per-Fief Settings** — Each castle/town can be configured independently
- **Elite Recruitment** — Option to only recruit tier 3+ troops
- **Configurable Threshold** — Set max garrison size before recruitment stops (default: 100)
- **Guard Party Auto-Refill** — Guard parties automatically refill from the garrison when they take losses
- **Save-Compatible** — Settings are persisted with your save game

## How It Works

The mod adds a `CampaignBehavior` that listens to the daily settlement tick. For each player-owned town or castle it:

1. **Recruits** troops from village notables within the fief's bound villages
2. **Converts** prisoners in the settlement's prison into garrison troops
3. **Upgrades** garrison troops that have accumulated enough XP
4. **Maintains** guard parties that patrol around the settlement

Guard parties are created using a custom `PartyComponent` and are set to patrol around their home settlement, defending against bandits and raiders.

## Prerequisites

- **Mount & Blade II: Bannerlord** (Steam, tested with v1.2.x+)
- **.NET Framework 4.7.2 SDK** (included with Visual Studio 2022)
- **Harmony** (pulled automatically via NuGet)

## Project Structure

```
ImprovedGarrisons/
├── Module/
│   └── SubModule.xml              # Bannerlord module definition
├── src/
│   └── ImprovedGarrisons/
│       ├── ImprovedGarrisons.csproj
│       ├── SubModule.cs                          # Mod entry point
│       ├── GarrisonManager.cs                    # Core recruitment/training logic
│       ├── GarrisonSettings.cs                   # Per-fief configuration
│       ├── GuardPartyComponent.cs                # Custom party component
│       ├── GuardPartyManager.cs                  # Guard party lifecycle & AI
│       └── ImprovedGarrisonsCampaignBehavior.cs  # Daily tick handler
├── tests/
│   └── ImprovedGarrisons.Tests/
│       ├── ImprovedGarrisons.Tests.csproj
│       ├── GarrisonManagerTests.cs
│       └── GarrisonSettingsTests.cs
├── ImprovedGarrisons.sln
├── deploy.ps1
└── README.md
```

## Setup & Build

1. **Clone** this repository into your Bannerlord mods workspace.

2. **Update `GameFolder`** in `src/ImprovedGarrisons/ImprovedGarrisons.csproj` to point at your Bannerlord installation:
   ```xml
   <GameFolder>C:\Games\steamapps\common\Mount &amp; Blade II Bannerlord</GameFolder>
   ```

3. **Build**:
   ```
   dotnet build src\ImprovedGarrisons\ImprovedGarrisons.csproj -c Release
   ```

4. **Deploy** (copies DLL + SubModule.xml to the game's Modules folder):
   ```
   .\deploy.ps1
   ```

## Running Tests

```
dotnet test tests\ImprovedGarrisons.Tests\ImprovedGarrisons.Tests.csproj
```

## Compatibility

- Works with ongoing campaigns — safe to install mid-playthrough
- Standalone mod with no additional requirements beyond the base game modules
- Should be compatible with most other mods that don't also modify garrison behavior
