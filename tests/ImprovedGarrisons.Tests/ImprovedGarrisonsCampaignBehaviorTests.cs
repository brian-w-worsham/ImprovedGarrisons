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
            var recruitmentThresholdOverrides = new List<int>();
            var guardPartyMaxSizes = new List<int>();
            var guardPartyMaxSizeOverrides = new List<int>();
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
            recruitmentThresholdOverrides.AddRange(state.RecruitmentThresholdOverrides);
            guardPartyMaxSizes.AddRange(state.GuardPartyMaxSizes);
            guardPartyMaxSizeOverrides.AddRange(state.GuardPartyMaxSizeOverrides);
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
                RecruitmentThresholdOverrides = recruitmentThresholdOverrides,
                GuardPartyMaxSizes = guardPartyMaxSizes,
                GuardPartyMaxSizeOverrides = guardPartyMaxSizeOverrides,
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
            Assert.True(settings.GuardPartyEnabled);
            Assert.Equal(GarrisonSettings.AutomaticRecruitmentThreshold, settings.RecruitmentThreshold);
            Assert.Equal(425, settings.ResolveRecruitmentThreshold(425));
            Assert.Equal(GarrisonSettings.AutomaticGuardPartyMaxSize, settings.GuardPartyMaxSize);
            Assert.Equal(50, settings.ResolveGuardPartyMaxSize(200));
            Assert.True(settings.GuardPartyAutoRefill);
            Assert.False(settings.RecruitEliteOnly);
            Assert.Equal(0, settings.DailyRecruitBudget);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(200)]
        public void ReadSyncData_MigratesLegacyDefaultRecruitmentThresholdToAutomaticSizing(int legacyThreshold)
        {
            var restored = new GarrisonSettingsSyncState
            {
                SettlementIds = new List<string> { TownSettlementId },
                RecruitmentThresholds = new List<int> { legacyThreshold }
            }.ToDictionary();

            var settings = restored[TownSettlementId];

            Assert.Equal(GarrisonSettings.AutomaticRecruitmentThreshold, settings.RecruitmentThreshold);
            Assert.Equal(425, settings.ResolveRecruitmentThreshold(425));
        }

        [Fact]
        public void ReadSyncData_MigratesLegacyDefaultGuardPartyMaxSizeToAutomaticSizing()
        {
            var restored = new GarrisonSettingsSyncState
            {
                SettlementIds = new List<string> { TownSettlementId },
                GuardPartyMaxSizes = new List<int> { 30 }
            }.ToDictionary();

            var settings = restored[TownSettlementId];

            Assert.Equal(GarrisonSettings.AutomaticGuardPartyMaxSize, settings.GuardPartyMaxSize);
            Assert.Equal(75, settings.ResolveGuardPartyMaxSize(300));
        }

        [Fact]
        public void SyncDataHelpers_RoundTripAutomaticGuardPartyMaxSize()
        {
            var source = new Dictionary<string, GarrisonSettings>
            {
                [TownSettlementId] = new GarrisonSettings()
            };

            var restored = GarrisonSettingsSyncState.From(source).ToDictionary();
            var settings = restored[TownSettlementId];

            Assert.Equal(GarrisonSettings.AutomaticRecruitmentThreshold, settings.RecruitmentThreshold);
            Assert.Equal(425, settings.ResolveRecruitmentThreshold(425));
            Assert.Equal(GarrisonSettings.AutomaticGuardPartyMaxSize, settings.GuardPartyMaxSize);
            Assert.Equal(75, settings.ResolveGuardPartyMaxSize(300));
        }

        [Fact]
        public void BuildActivationStatusMessage_WithPlayerFiefs_DescribesActiveAutomation()
        {
            string message = ImprovedGarrisonsCampaignBehavior.BuildActivationStatusMessage(hasPlayerFiefs: true);

            Assert.Equal(
                "Improved Garrisons: Active for your towns and castles. Daily automation is running.",
                message);
        }

        [Fact]
        public void BuildActivationStatusMessage_WithoutPlayerFiefs_ExplainsWhenAutomationStarts()
        {
            string message = ImprovedGarrisonsCampaignBehavior.BuildActivationStatusMessage(hasPlayerFiefs: false);

            Assert.Equal(
                "Improved Garrisons: Active. Daily automation will begin after you own a town or castle.",
                message);
        }

        [Fact]
        public void BuildManagedSettlementMessage_WithEnabledFeatures_UsesProvidedSettlementName()
        {
            const string settlementName = "Player-Owned Settlement";

            var settings = new GarrisonSettings
            {
                AutoRecruitEnabled = true,
                AutoRecruitPrisonersEnabled = false,
                AutoTrainingEnabled = true,
                GuardPartyEnabled = false
            };

            string message = ImprovedGarrisonsCampaignBehavior.BuildManagedSettlementMessage(settlementName, settings);

            Assert.Equal(
                $"Improved Garrisons: Managing {settlementName} with village recruitment, training enabled.",
                message);
        }

        [Fact]
        public void BuildManagedSettlementMessage_WithAllAutomationDisabled_UsesProvidedSettlementName()
        {
            const string settlementName = "Player-Owned Settlement";

            var settings = new GarrisonSettings
            {
                AutoRecruitEnabled = false,
                AutoRecruitPrisonersEnabled = false,
                AutoTrainingEnabled = false,
                GuardPartyEnabled = false
            };

            string message = ImprovedGarrisonsCampaignBehavior.BuildManagedSettlementMessage(settlementName, settings);

            Assert.Equal(
                $"Improved Garrisons: {settlementName} is registered, but all current automation is disabled.",
                message);
        }

        [Fact]
        public void BuildManagedSettlementMessage_WithGuardPartiesEnabled_ListsGuardParties()
        {
            const string settlementName = "Player-Owned Settlement";

            var settings = new GarrisonSettings
            {
                AutoRecruitEnabled = false,
                AutoRecruitPrisonersEnabled = false,
                AutoTrainingEnabled = false,
                GuardPartyEnabled = true
            };

            string message = ImprovedGarrisonsCampaignBehavior.BuildManagedSettlementMessage(settlementName, settings);

            Assert.Equal(
                $"Improved Garrisons: Managing {settlementName} with guard parties enabled.",
                message);
        }
    }
}