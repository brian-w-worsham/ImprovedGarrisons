using System.Collections.Generic;
using ImprovedGarrisons;
using Xunit;

namespace ImprovedGarrisons.Tests
{
    public class ImprovedGarrisonsCampaignBehaviorTests
    {
        private const string CastleSettlementId = "castle_b";
        private const string TownSettlementId = "town_a";

        [Fact]
        public void SyncDataHelpers_RoundTripAllSettingsFields()
        {
            var source = new Dictionary<string, GarrisonSettings>
            {
                [CastleSettlementId] = new GarrisonSettings
                {
                    AutoRecruitEnabled = false,
                    AutoRecruitPrisonersEnabled = true,
                    AutoTrainingEnabled = false,
                    GuardPartyEnabled = true,
                    RecruitmentThreshold = 180,
                    GuardPartyMaxSize = 45,
                    GuardPartyAutoRefill = false,
                    RecruitEliteOnly = true,
                    DailyRecruitBudget = 600
                },
                [TownSettlementId] = new GarrisonSettings
                {
                    AutoRecruitEnabled = true,
                    AutoRecruitPrisonersEnabled = false,
                    AutoTrainingEnabled = true,
                    GuardPartyEnabled = false,
                    RecruitmentThreshold = 125,
                    GuardPartyMaxSize = 25,
                    GuardPartyAutoRefill = true,
                    RecruitEliteOnly = false,
                    DailyRecruitBudget = 150
                }
            };

            var settlementIds = new List<string>();
            var autoRecruitEnabled = new List<int>();
            var autoRecruitPrisonersEnabled = new List<int>();
            var autoTrainingEnabled = new List<int>();
            var guardPartyEnabled = new List<int>();
            var recruitmentThresholds = new List<int>();
            var guardPartyMaxSizes = new List<int>();
            var guardPartyAutoRefillEnabled = new List<int>();
            var recruitEliteOnly = new List<int>();
            var dailyRecruitBudgets = new List<int>();

            var state = GarrisonSettingsSyncState.From(source);

            settlementIds.AddRange(state.SettlementIds);
            autoRecruitEnabled.AddRange(state.AutoRecruitEnabled);
            autoRecruitPrisonersEnabled.AddRange(state.AutoRecruitPrisonersEnabled);
            autoTrainingEnabled.AddRange(state.AutoTrainingEnabled);
            guardPartyEnabled.AddRange(state.GuardPartyEnabled);
            recruitmentThresholds.AddRange(state.RecruitmentThresholds);
            guardPartyMaxSizes.AddRange(state.GuardPartyMaxSizes);
            guardPartyAutoRefillEnabled.AddRange(state.GuardPartyAutoRefillEnabled);
            recruitEliteOnly.AddRange(state.RecruitEliteOnly);
            dailyRecruitBudgets.AddRange(state.DailyRecruitBudgets);

            var restored = new GarrisonSettingsSyncState
            {
                SettlementIds = settlementIds,
                AutoRecruitEnabled = autoRecruitEnabled,
                AutoRecruitPrisonersEnabled = autoRecruitPrisonersEnabled,
                AutoTrainingEnabled = autoTrainingEnabled,
                GuardPartyEnabled = guardPartyEnabled,
                RecruitmentThresholds = recruitmentThresholds,
                GuardPartyMaxSizes = guardPartyMaxSizes,
                GuardPartyAutoRefillEnabled = guardPartyAutoRefillEnabled,
                RecruitEliteOnly = recruitEliteOnly,
                DailyRecruitBudgets = dailyRecruitBudgets
            }.ToDictionary();

            Assert.Equal(2, restored.Count);

            var townSettings = restored[TownSettlementId];
            Assert.True(townSettings.AutoRecruitEnabled);
            Assert.False(townSettings.AutoRecruitPrisonersEnabled);
            Assert.True(townSettings.AutoTrainingEnabled);
            Assert.False(townSettings.GuardPartyEnabled);
            Assert.Equal(125, townSettings.RecruitmentThreshold);
            Assert.Equal(25, townSettings.GuardPartyMaxSize);
            Assert.True(townSettings.GuardPartyAutoRefill);
            Assert.False(townSettings.RecruitEliteOnly);
            Assert.Equal(150, townSettings.DailyRecruitBudget);

            var castleSettings = restored[CastleSettlementId];
            Assert.False(castleSettings.AutoRecruitEnabled);
            Assert.True(castleSettings.AutoRecruitPrisonersEnabled);
            Assert.False(castleSettings.AutoTrainingEnabled);
            Assert.True(castleSettings.GuardPartyEnabled);
            Assert.Equal(180, castleSettings.RecruitmentThreshold);
            Assert.Equal(45, castleSettings.GuardPartyMaxSize);
            Assert.False(castleSettings.GuardPartyAutoRefill);
            Assert.True(castleSettings.RecruitEliteOnly);
            Assert.Equal(600, castleSettings.DailyRecruitBudget);
        }

        [Fact]
        public void ReadSyncData_UsesDefaultsWhenOptionalListsAreMissing()
        {
            var restored = new GarrisonSettingsSyncState
            {
                SettlementIds = new List<string> { TownSettlementId }
            }.ToDictionary();

            var settings = restored[TownSettlementId];

            Assert.True(settings.AutoRecruitEnabled);
            Assert.True(settings.AutoRecruitPrisonersEnabled);
            Assert.True(settings.AutoTrainingEnabled);
            Assert.False(settings.GuardPartyEnabled);
            Assert.Equal(100, settings.RecruitmentThreshold);
            Assert.Equal(30, settings.GuardPartyMaxSize);
            Assert.True(settings.GuardPartyAutoRefill);
            Assert.False(settings.RecruitEliteOnly);
            Assert.Equal(0, settings.DailyRecruitBudget);
        }
    }
}