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
        private readonly Dictionary<string, GarrisonSettings> _settingsPerFief = new Dictionary<string, GarrisonSettings>();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_improvedGarrisonsSettings", ref _settingsPerFief);
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

        internal void ProcessSettlement(Settlement settlement, GarrisonSettings settings)
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
}
