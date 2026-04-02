using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
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

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
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
            dataStore.SyncData("_improvedGarrisonsGuardPartyMaxSizes", ref syncState.GuardPartyMaxSizes);
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
            return settings;
        }

        internal void OnDailyTickSettlement(Settlement settlement)
        {
            if (!IsPlayerFief(settlement)) return;
            if (!settlement.IsTown && !settlement.IsCastle) return;

            var settings = GetOrCreateSettings(settlement);
            ProcessSettlement(settlement, settings);
        }

        internal static void ProcessSettlement(Settlement settlement, GarrisonSettings settings)
        {
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
        }

        /// <summary>
        /// Determines whether the settlement belongs to the player's clan.
        /// </summary>
        internal static bool IsPlayerFief(Settlement settlement)
        {
            if (settlement?.OwnerClan == null) return false;
            return settlement.OwnerClan == Clan.PlayerClan;
        }
    }

    internal sealed class GarrisonSettingsSyncState
    {
        internal List<string> SettlementIds = new List<string>();
        internal List<int> AutoRecruitEnabled = new List<int>();
        internal List<int> AutoRecruitPrisonersEnabled = new List<int>();
        internal List<int> AutoTrainingEnabled = new List<int>();
        internal List<int> GuardPartyEnabled = new List<int>();
        internal List<int> RecruitmentThresholds = new List<int>();
        internal List<int> GuardPartyMaxSizes = new List<int>();
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

                restoredSettings[settlementId] = new GarrisonSettings
                {
                    AutoRecruitEnabled = GetValue(AutoRecruitEnabled, i, 1) != 0,
                    AutoRecruitPrisonersEnabled = GetValue(AutoRecruitPrisonersEnabled, i, 1) != 0,
                    AutoTrainingEnabled = GetValue(AutoTrainingEnabled, i, 1) != 0,
                    GuardPartyEnabled = GetValue(GuardPartyEnabled, i, 0) != 0,
                    RecruitmentThreshold = GetValue(RecruitmentThresholds, i, 100),
                    GuardPartyMaxSize = GetValue(GuardPartyMaxSizes, i, 30),
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
            GuardPartyMaxSizes.Add(settings.GuardPartyMaxSize);
            GuardPartyAutoRefillEnabled.Add(settings.GuardPartyAutoRefill ? 1 : 0);
            RecruitEliteOnly.Add(settings.RecruitEliteOnly ? 1 : 0);
            DailyRecruitBudgets.Add(settings.DailyRecruitBudget);
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
