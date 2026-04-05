using ImprovedGarrisons;
using Xunit;

namespace ImprovedGarrisons.Tests
{
    public class ImprovedGarrisonsMenuTests
    {
        [Fact]
        public void BuildGuardSettingsMenuText_ReportsCurrentSettingsAndActiveGuard()
        {
            var settings = new GarrisonSettings
            {
                GuardPartyEnabled = true,
                GuardPartyAutoRefill = false,
                GuardPartyMaxSize = 45,
                RecruitmentThreshold = 150
            };

            string menuText = ImprovedGarrisonsMenu.BuildGuardSettingsMenuText("Saneopa", settings, 18);

            Assert.Contains("Configure guard parties for Saneopa.", menuText);
            Assert.Contains("Guard parties: Enabled", menuText);
            Assert.Contains("Auto-refill: Disabled", menuText);
            Assert.Contains("Max guard size: 45", menuText);
            Assert.Contains("Garrison reserve threshold: 150", menuText);
            Assert.Contains("Active guard party: 18 troops deployed", menuText);
        }

        [Fact]
        public void BuildApplyGuardSettingsOptionText_WhenGuardPartyDisabled_ExplainsRequirement()
        {
            string optionText = ImprovedGarrisonsMenu.BuildApplyGuardSettingsOptionText(false, 0);

            Assert.Equal("Enable guard parties to create one", optionText);
        }

        [Fact]
        public void BuildApplyGuardSettingsOptionText_WhenGuardExists_UsesRefreshLabel()
        {
            string optionText = ImprovedGarrisonsMenu.BuildApplyGuardSettingsOptionText(true, 12);

            Assert.Equal("Refresh guard party now", optionText);
        }

        [Fact]
        public void BuildGuardSettingsInquiryText_IncludesActionPrompt()
        {
            var settings = new GarrisonSettings
            {
                GuardPartyEnabled = true,
                GuardPartyAutoRefill = true,
                GuardPartyMaxSize = 35,
                RecruitmentThreshold = 125
            };

            string inquiryText = ImprovedGarrisonsMenu.BuildGuardSettingsInquiryText("Epicrotea", settings, 0);

            Assert.Contains("Configure guard parties for Epicrotea.", inquiryText);
            Assert.Contains("Active guard party: None deployed", inquiryText);
            Assert.Contains("Select one action, then choose Apply.", inquiryText);
        }
    }
}