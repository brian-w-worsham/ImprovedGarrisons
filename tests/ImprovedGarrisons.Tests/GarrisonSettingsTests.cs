using ImprovedGarrisons;
using Xunit;

namespace ImprovedGarrisons.Tests
{
    public class GarrisonSettingsTests
    {
        [Fact]
        public void DefaultSettings_AutoRecruitEnabled()
        {
            var settings = new GarrisonSettings();

            Assert.True(settings.AutoRecruitEnabled);
        }

        [Fact]
        public void DefaultSettings_AutoRecruitPrisonersEnabled()
        {
            var settings = new GarrisonSettings();

            Assert.True(settings.AutoRecruitPrisonersEnabled);
        }

        [Fact]
        public void DefaultSettings_AutoTrainingEnabled()
        {
            var settings = new GarrisonSettings();

            Assert.True(settings.AutoTrainingEnabled);
        }

        [Fact]
        public void DefaultSettings_GuardPartyEnabled()
        {
            var settings = new GarrisonSettings();

            Assert.True(settings.GuardPartyEnabled);
        }

        [Fact]
        public void DefaultSettings_RecruitmentThresholdUsesSettlementCapacity()
        {
            var settings = new GarrisonSettings();

            Assert.Equal(GarrisonSettings.AutomaticRecruitmentThreshold, settings.RecruitmentThreshold);
            Assert.Equal(425, settings.ResolveRecruitmentThreshold(425));
        }

        [Theory]
        [InlineData(200, 50)]
        [InlineData(300, 75)]
        [InlineData(400, 100)]
        public void DefaultSettings_GuardPartyMaxSizeUsesQuarterOfDefensiveTroops(int defensiveTroopCount, int expected)
        {
            var settings = new GarrisonSettings();

            Assert.Equal(GarrisonSettings.AutomaticGuardPartyMaxSize, settings.GuardPartyMaxSize);
            Assert.Equal(expected, settings.ResolveGuardPartyMaxSize(defensiveTroopCount));
        }

        [Fact]
        public void DefaultSettings_GuardPartyAutoRefillEnabled()
        {
            var settings = new GarrisonSettings();

            Assert.True(settings.GuardPartyAutoRefill);
        }

        [Fact]
        public void DefaultSettings_EliteRecruitmentDisabled()
        {
            var settings = new GarrisonSettings();

            Assert.False(settings.RecruitEliteOnly);
        }

        [Fact]
        public void DefaultSettings_NoDailyBudgetLimit()
        {
            var settings = new GarrisonSettings();

            Assert.Equal(0, settings.DailyRecruitBudget);
        }

        [Fact]
        public void Settings_CanBeModified()
        {
            var settings = new GarrisonSettings
            {
                AutoRecruitEnabled = false,
                RecruitmentThreshold = 200,
                GuardPartyEnabled = true,
                GuardPartyMaxSize = 50,
                RecruitEliteOnly = true,
                DailyRecruitBudget = 500
            };

            Assert.False(settings.AutoRecruitEnabled);
            Assert.Equal(200, settings.RecruitmentThreshold);
            Assert.True(settings.GuardPartyEnabled);
            Assert.Equal(50, settings.GuardPartyMaxSize);
            Assert.True(settings.RecruitEliteOnly);
            Assert.Equal(500, settings.DailyRecruitBudget);
        }

        [Fact]
        public void AdjustGuardPartyMaxSize_ClampsToSupportedRange()
        {
            var settings = new GarrisonSettings
            {
                GuardPartyMaxSize = 30
            };

            settings.AdjustGuardPartyMaxSize(-500, 200);
            Assert.Equal(GarrisonSettings.MinGuardPartyMaxSize, settings.GuardPartyMaxSize);

            settings.AdjustGuardPartyMaxSize(1000, 200);
            Assert.Equal(GarrisonSettings.MaxGuardPartyMaxSize, settings.GuardPartyMaxSize);
        }

        [Fact]
        public void AdjustGuardPartyMaxSize_FromAutomaticDefaultUsesResolvedCurrentValue()
        {
            var settings = new GarrisonSettings();

            int newValue = settings.AdjustGuardPartyMaxSize(5, 200);

            Assert.Equal(55, newValue);
            Assert.Equal(55, settings.GuardPartyMaxSize);
        }

        [Fact]
        public void ResetGuardPartyMaxSize_RestoresAutomaticMode()
        {
            var settings = new GarrisonSettings
            {
                GuardPartyMaxSize = 55
            };

            settings.ResetGuardPartyMaxSize();

            Assert.True(settings.UsesAutomaticGuardPartyMaxSize);
            Assert.Equal(GarrisonSettings.AutomaticGuardPartyMaxSize, settings.GuardPartyMaxSize);
        }

        [Fact]
        public void AdjustRecruitmentThreshold_FromAutomaticDefaultUsesResolvedCurrentValue()
        {
            var settings = new GarrisonSettings();

            int newValue = settings.AdjustRecruitmentThreshold(-25, 425);

            Assert.Equal(400, newValue);
            Assert.Equal(400, settings.RecruitmentThreshold);
        }

        [Fact]
        public void ResetRecruitmentThreshold_RestoresAutomaticMode()
        {
            var settings = new GarrisonSettings
            {
                RecruitmentThreshold = 350
            };

            settings.ResetRecruitmentThreshold();

            Assert.True(settings.UsesAutomaticRecruitmentThreshold);
            Assert.Equal(GarrisonSettings.AutomaticRecruitmentThreshold, settings.RecruitmentThreshold);
        }

        [Fact]
        public void AdjustRecruitmentThreshold_ClampsToSupportedRange()
        {
            var settings = new GarrisonSettings
            {
                RecruitmentThreshold = 100
            };

            settings.AdjustRecruitmentThreshold(-500, 425);
            Assert.Equal(GarrisonSettings.MinRecruitmentThreshold, settings.RecruitmentThreshold);

            settings.AdjustRecruitmentThreshold(1000, 425);
            Assert.Equal(425, settings.RecruitmentThreshold);
        }

        [Fact]
        public void ResolveRecruitmentThreshold_ExplicitValuesClampToSettlementCapacity()
        {
            var settings = new GarrisonSettings
            {
                RecruitmentThreshold = 600
            };

            Assert.Equal(425, settings.ResolveRecruitmentThreshold(425));
        }
    }
}
