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

            string menuText = ImprovedGarrisonsMenu.BuildGuardSettingsMenuText("Saneopa", settings, 18, 200, 400);

            Assert.Contains("Configure guard parties for Saneopa.", menuText);
            Assert.Contains("Guard parties: Enabled", menuText);
            Assert.Contains("Auto-refill: Disabled", menuText);
            Assert.Contains("Max guard size: 45", menuText);
            Assert.Contains("Auto-recruit target: 150", menuText);
            Assert.Contains("Guard refill keeps at least: 75 troops in garrison", menuText);
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
        public void BuildGuardPartySizeOptionText_WithAutomaticValue_UsesAutoLabel()
        {
            string optionText = ImprovedGarrisonsMenu.BuildGuardPartySizeOptionText(75, increase: true, usesAutomaticValue: true);

            Assert.Equal("Increase max guard size (Auto (75) -> 80)", optionText);
        }

        [Fact]
        public void BuildResetGuardPartySizeOptionText_UsesAutoLabel()
        {
            string optionText = ImprovedGarrisonsMenu.BuildResetGuardPartySizeOptionText(75);

            Assert.Equal("Reset max guard size to Auto (75)", optionText);
        }

        [Fact]
        public void BuildReserveThresholdOptionText_WithAutomaticValue_UsesAutoLabel()
        {
            string optionText = ImprovedGarrisonsMenu.BuildReserveThresholdOptionText(425, increase: false, usesAutomaticValue: true, maximumValue: 425);

            Assert.Equal("Decrease auto-recruit target (Auto (425) -> 400)", optionText);
        }

        [Fact]
        public void BuildResetReserveThresholdOptionText_UsesAutoLabel()
        {
            string optionText = ImprovedGarrisonsMenu.BuildResetReserveThresholdOptionText(425);

            Assert.Equal("Reset auto-recruit target to Auto (425)", optionText);
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

            string inquiryText = ImprovedGarrisonsMenu.BuildGuardSettingsInquiryText("Epicrotea", settings, 0, 140, 400);

            Assert.Contains("Configure guard parties for Epicrotea.", inquiryText);
            Assert.Contains("Active guard party: None deployed", inquiryText);
            Assert.Contains("Select one action, then choose Apply.", inquiryText);
        }

        [Fact]
        public void BuildGuardSettingsMenuText_WithAutomaticGuardSize_UsesQuarterOfDefensiveTroops()
        {
            var settings = new GarrisonSettings();

            string menuText = ImprovedGarrisonsMenu.BuildGuardSettingsMenuText("Saneopa", settings, 25, 300, 425);

            Assert.Contains("Max guard size: Auto (75)", menuText);
            Assert.Contains("Auto-recruit target: Auto (425)", menuText);
            Assert.Contains("Guard refill keeps at least: 212 troops in garrison", menuText);
        }
    }
}