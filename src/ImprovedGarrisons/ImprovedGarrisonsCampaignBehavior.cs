using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ImprovedGarrisons
{
    /// <summary>
    /// Campaign behavior that runs garrison management tasks on each daily tick.
    /// Handles auto-recruitment from villages, prisoner recruitment, troop
    /// training/upgrading, and guard party maintenance for the player's fiefs.
    /// </summary>
    internal class ImprovedGarrisonsCampaignBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, GarrisonSettings> _settingsPerFief = new Dictionary<string, GarrisonSettings>();
        private readonly HashSet<string> _announcedManagedFiefs = new HashSet<string>(StringComparer.Ordinal);
        private bool _hasShownActivationStatus;

        public override void RegisterEvents()
        {
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, OnAfterSettlementEntered);
            CampaignEvents.BeforeGameMenuOpenedEvent.AddNonSerializedListener(this, OnBeforeGameMenuOpened);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
            CampaignEvents.OnPartySizeChangedEvent.AddNonSerializedListener(this, OnPartySizeChanged);
        }

        public override void SyncData(IDataStore dataStore)
        {
            var syncState = dataStore.IsSaving
                ? GarrisonSettingsSyncState.From(_settingsPerFief)
                : new GarrisonSettingsSyncState();

            dataStore.SyncData("_improvedGarrisonsSettlementIds", ref syncState.SettlementIds);
            dataStore.SyncData("_improvedGarrisonsAutoRecruitEnabled", ref syncState.AutoRecruitEnabled);
            dataStore.SyncData("_improvedGarrisonsAutoRecruitPrisonersEnabled", ref syncState.AutoRecruitPrisonersEnabled);
            dataStore.SyncData("_improvedGarrisonsAutoTrainingEnabled", ref syncState.AutoTrainingEnabled);
            dataStore.SyncData("_improvedGarrisonsGuardPartyEnabled", ref syncState.GuardPartyEnabled);
            dataStore.SyncData("_improvedGarrisonsRecruitmentThresholds", ref syncState.RecruitmentThresholds);
            dataStore.SyncData("_improvedGarrisonsRecruitmentThresholdOverrides", ref syncState.RecruitmentThresholdOverrides);
            dataStore.SyncData("_improvedGarrisonsGuardPartyMaxSizes", ref syncState.GuardPartyMaxSizes);
            dataStore.SyncData("_improvedGarrisonsGuardPartyMaxSizeOverrides", ref syncState.GuardPartyMaxSizeOverrides);
            dataStore.SyncData("_improvedGarrisonsGuardPartyAutoRefillEnabled", ref syncState.GuardPartyAutoRefillEnabled);
            dataStore.SyncData("_improvedGarrisonsRecruitEliteOnly", ref syncState.RecruitEliteOnly);
            dataStore.SyncData("_improvedGarrisonsDailyRecruitBudgets", ref syncState.DailyRecruitBudgets);

            if (dataStore.IsLoading)
            {
                _settingsPerFief = syncState.ToDictionary();
            }
        }

        /// <summary>
        /// Returns the <see cref="GarrisonSettings"/> for a fief, creating
        /// a default entry if one does not yet exist.
        /// </summary>
        internal GarrisonSettings GetOrCreateSettings(Settlement settlement)
        {
            string key = settlement.StringId;
            if (!_settingsPerFief.TryGetValue(key, out var settings))
            {
                settings = new GarrisonSettings();
                _settingsPerFief[key] = settings;
            }

            settings.Normalize();

            return settings;
        }

        internal void OnDailyTick()
        {
            GuardPartyManager.CleanupOrphanedGuardParties();

            if (_hasShownActivationStatus)
            {
                return;
            }

            _hasShownActivationStatus = true;

            bool hasPlayerFiefs = HasAnyPlayerFiefs();
            InformationManager.DisplayMessage(
                new InformationMessage(
                    BuildActivationStatusMessage(hasPlayerFiefs),
                    hasPlayerFiefs ? Colors.Green : Colors.Yellow));
        }

        internal void OnDailyTickSettlement(Settlement settlement)
        {
            if (!IsPlayerFief(settlement)) return;
            if (!settlement.IsTown && !settlement.IsCastle) return;

            var settings = GetOrCreateSettings(settlement);
            AnnounceManagedSettlement(settlement, settings);
            ProcessSettlement(settlement, settings);
        }

        internal void OnAfterSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (mobileParty != MobileParty.MainParty)
            {
                return;
            }

            if (!IsPlayerFief(settlement) || (!settlement.IsTown && !settlement.IsCastle))
            {
                return;
            }

            var settings = GetOrCreateSettings(settlement);
            AnnounceManagedSettlement(settlement, settings);
            GuardPartyManager.MaintainGuardParty(settlement, settings);
        }

        internal void OnBeforeGameMenuOpened(MenuCallbackArgs args)
        {
            if (args?.MenuContext?.GameMenu == null)
            {
                return;
            }

            try
            {
                ImprovedGarrisonsMenu.TryInjectGuardSettingsOption(args.MenuContext.GameMenu);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $"Improved Garrisons: Menu injection failed: {ex.Message}",
                        Colors.Red));
            }
        }

        internal void OnPartySizeChanged(PartyBase party)
        {
            GuardPartyManager.CleanupDepletedGuardParty(party);
        }

        internal void OnHourlyTickParty(MobileParty party)
        {
            GuardPartyManager.CleanupDepletedGuardParty(party?.Party);
        }

        internal void AnnounceManagedSettlement(Settlement settlement, GarrisonSettings settings)
        {
            if (settlement == null || string.IsNullOrEmpty(settlement.StringId))
            {
                return;
            }

            if (!_announcedManagedFiefs.Add(settlement.StringId))
            {
                return;
            }

            InformationManager.DisplayMessage(
                new InformationMessage(
                    BuildManagedSettlementMessage(settlement.Name?.ToString(), settings),
                    Colors.Cyan));
        }

        internal static void ProcessSettlement(Settlement settlement, GarrisonSettings settings)
        {
            if (settlement == null || settings == null) return;

            try
            {
                ProcessSettlementCore(settlement, settings);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $"Improved Garrisons: Error processing {settlement.Name}: {ex.Message}",
                        Colors.Red));
            }
        }

        private static void ProcessSettlementCore(Settlement settlement, GarrisonSettings settings)
        {
            settings.Normalize();

            if (settings.AutoRecruitEnabled)
            {
                int recruited = GarrisonManager.RecruitFromNearbyVillages(settlement, settings);
                if (recruited > 0)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage(
                            $"Improved Garrisons: Recruited {recruited} troops to {settlement.Name}.",
                            Colors.Cyan));
                }
            }

            if (settings.AutoRecruitPrisonersEnabled)
            {
                int recruited = GarrisonManager.RecruitPrisoners(settlement, settings);
                if (recruited > 0)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage(
                            $"Improved Garrisons: Recruited {recruited} prisoners at {settlement.Name}.",
                            Colors.Cyan));
                }
            }

            if (settings.AutoTrainingEnabled)
            {
                int upgraded = GarrisonManager.TrainGarrison(settlement);
                if (upgraded > 0)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage(
                            $"Improved Garrisons: Upgraded {upgraded} troops at {settlement.Name}.",
                            Colors.Cyan));
                }
            }

            GuardPartyManager.MaintainGuardParty(settlement, settings);
        }

        /// <summary>
        /// Determines whether the settlement belongs to the player's clan.
        /// </summary>
        internal static bool IsPlayerFief(Settlement settlement)
        {
            if (settlement?.OwnerClan == null) return false;
            return settlement.OwnerClan == Clan.PlayerClan;
        }

        internal static bool HasAnyPlayerFiefs()
        {
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement != null && (settlement.IsTown || settlement.IsCastle) && IsPlayerFief(settlement))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string BuildActivationStatusMessage(bool hasPlayerFiefs)
        {
            if (hasPlayerFiefs)
            {
                return "Improved Garrisons: Active for your towns and castles. Daily automation is running.";
            }

            return "Improved Garrisons: Active. Daily automation will begin after you own a town or castle.";
        }

        internal static string BuildManagedSettlementMessage(string settlementName, GarrisonSettings settings)
        {
            string resolvedSettlementName = string.IsNullOrWhiteSpace(settlementName)
                ? "your settlement"
                : settlementName;

            var enabledFeatures = new List<string>();

            if (settings?.AutoRecruitEnabled == true)
            {
                enabledFeatures.Add("village recruitment");
            }

            if (settings?.AutoRecruitPrisonersEnabled == true)
            {
                enabledFeatures.Add("prisoner recruitment");
            }

            if (settings?.AutoTrainingEnabled == true)
            {
                enabledFeatures.Add("training");
            }

            if (settings?.GuardPartyEnabled == true)
            {
                enabledFeatures.Add("guard parties");
            }

            if (enabledFeatures.Count == 0)
            {
                return $"Improved Garrisons: {resolvedSettlementName} is registered, but all current automation is disabled.";
            }

            return $"Improved Garrisons: Managing {resolvedSettlementName} with {string.Join(", ", enabledFeatures)} enabled.";
        }
    }

    internal sealed class GarrisonSettingsSyncState
    {
        private const int LegacyDefaultRecruitmentThreshold = 100;
        private const int LegacySecondaryDefaultRecruitmentThreshold = 200;
        private const int LegacyDefaultGuardPartyMaxSize = 30;

        internal List<string> SettlementIds = new List<string>();
        internal List<int> AutoRecruitEnabled = new List<int>();
        internal List<int> AutoRecruitPrisonersEnabled = new List<int>();
        internal List<int> AutoTrainingEnabled = new List<int>();
        internal List<int> GuardPartyEnabled = new List<int>();
        internal List<int> RecruitmentThresholds = new List<int>();
        internal List<int> RecruitmentThresholdOverrides = new List<int>();
        internal List<int> GuardPartyMaxSizes = new List<int>();
        internal List<int> GuardPartyMaxSizeOverrides = new List<int>();
        internal List<int> GuardPartyAutoRefillEnabled = new List<int>();
        internal List<int> RecruitEliteOnly = new List<int>();
        internal List<int> DailyRecruitBudgets = new List<int>();

        internal static GarrisonSettingsSyncState From(Dictionary<string, GarrisonSettings> settingsPerFief)
        {
            var state = new GarrisonSettingsSyncState();
            if (settingsPerFief == null || settingsPerFief.Count == 0)
            {
                return state;
            }

            var orderedSettlementIds = new List<string>(settingsPerFief.Keys);
            orderedSettlementIds.Sort(StringComparer.Ordinal);

            foreach (var settlementId in orderedSettlementIds)
            {
                if (!settingsPerFief.TryGetValue(settlementId, out var settings) || settings == null)
                {
                    continue;
                }

                state.Add(settlementId, settings);
            }

            return state;
        }

        internal Dictionary<string, GarrisonSettings> ToDictionary()
        {
            var restoredSettings = new Dictionary<string, GarrisonSettings>();

            for (int i = 0; i < SettlementIds.Count; i++)
            {
                var settlementId = SettlementIds[i];
                if (string.IsNullOrEmpty(settlementId))
                {
                    continue;
                }

                int guardPartyMaxSize = GetValue(GuardPartyMaxSizes, i, GarrisonSettings.AutomaticGuardPartyMaxSize);
                bool hasSavedGuardPartyMaxSize = HasValue(GuardPartyMaxSizes, i);
                int recruitmentThreshold = GetValue(RecruitmentThresholds, i, GarrisonSettings.AutomaticRecruitmentThreshold);
                bool hasSavedRecruitmentThreshold = HasValue(RecruitmentThresholds, i);
                bool hasRecruitmentThresholdOverride = HasValue(RecruitmentThresholdOverrides, i)
                    ? GetValue(RecruitmentThresholdOverrides, i, 0) != 0
                    : hasSavedRecruitmentThreshold && !IsLegacyDefaultRecruitmentThreshold(recruitmentThreshold);
                bool hasGuardPartyMaxSizeOverride = HasValue(GuardPartyMaxSizeOverrides, i)
                    ? GetValue(GuardPartyMaxSizeOverrides, i, 0) != 0
                    : hasSavedGuardPartyMaxSize && guardPartyMaxSize != LegacyDefaultGuardPartyMaxSize;

                restoredSettings[settlementId] = new GarrisonSettings
                {
                    AutoRecruitEnabled = GetValue(AutoRecruitEnabled, i, 1) != 0,
                    AutoRecruitPrisonersEnabled = GetValue(AutoRecruitPrisonersEnabled, i, 1) != 0,
                    AutoTrainingEnabled = GetValue(AutoTrainingEnabled, i, 1) != 0,
                    GuardPartyEnabled = GetValue(GuardPartyEnabled, i, 1) != 0,
                    RecruitmentThreshold = hasRecruitmentThresholdOverride
                        ? recruitmentThreshold
                        : GarrisonSettings.AutomaticRecruitmentThreshold,
                    GuardPartyMaxSize = hasGuardPartyMaxSizeOverride
                        ? guardPartyMaxSize
                        : GarrisonSettings.AutomaticGuardPartyMaxSize,
                    GuardPartyAutoRefill = GetValue(GuardPartyAutoRefillEnabled, i, 1) != 0,
                    RecruitEliteOnly = GetValue(RecruitEliteOnly, i, 0) != 0,
                    DailyRecruitBudget = GetValue(DailyRecruitBudgets, i, 0)
                };
            }

            return restoredSettings;
        }

        private void Add(string settlementId, GarrisonSettings settings)
        {
            SettlementIds.Add(settlementId);
            AutoRecruitEnabled.Add(settings.AutoRecruitEnabled ? 1 : 0);
            AutoRecruitPrisonersEnabled.Add(settings.AutoRecruitPrisonersEnabled ? 1 : 0);
            AutoTrainingEnabled.Add(settings.AutoTrainingEnabled ? 1 : 0);
            GuardPartyEnabled.Add(settings.GuardPartyEnabled ? 1 : 0);
            RecruitmentThresholds.Add(settings.RecruitmentThreshold);
            RecruitmentThresholdOverrides.Add(settings.RecruitmentThreshold > GarrisonSettings.AutomaticRecruitmentThreshold ? 1 : 0);
            GuardPartyMaxSizes.Add(settings.GuardPartyMaxSize);
            GuardPartyMaxSizeOverrides.Add(settings.GuardPartyMaxSize > GarrisonSettings.AutomaticGuardPartyMaxSize ? 1 : 0);
            GuardPartyAutoRefillEnabled.Add(settings.GuardPartyAutoRefill ? 1 : 0);
            RecruitEliteOnly.Add(settings.RecruitEliteOnly ? 1 : 0);
            DailyRecruitBudgets.Add(settings.DailyRecruitBudget);
        }

        private static bool IsLegacyDefaultRecruitmentThreshold(int value)
        {
            return value == GarrisonSettings.AutomaticRecruitmentThreshold
                || value == LegacyDefaultRecruitmentThreshold
                || value == LegacySecondaryDefaultRecruitmentThreshold;
        }

        private static bool HasValue(List<int> values, int index)
        {
            return values != null && index >= 0 && index < values.Count;
        }

        private static int GetValue(List<int> values, int index, int defaultValue)
        {
            if (values == null || index < 0 || index >= values.Count)
            {
                return defaultValue;
            }

            return values[index];
        }
    }
}
