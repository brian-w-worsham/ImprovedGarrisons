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
        public void DefaultSettings_GuardPartyDisabled()
        {
            var settings = new GarrisonSettings();

            Assert.False(settings.GuardPartyEnabled);
        }

        [Fact]
        public void DefaultSettings_RecruitmentThresholdIs100()
        {
            var settings = new GarrisonSettings();

            Assert.Equal(100, settings.RecruitmentThreshold);
        }

        [Fact]
        public void DefaultSettings_GuardPartyMaxSizeIs30()
        {
            var settings = new GarrisonSettings();

            Assert.Equal(30, settings.GuardPartyMaxSize);
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
    }
}
