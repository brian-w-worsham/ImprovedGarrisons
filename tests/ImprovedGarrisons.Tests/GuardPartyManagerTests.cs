using ImprovedGarrisons;
using Xunit;

namespace ImprovedGarrisons.Tests
{
    public class GuardPartyManagerTests
    {
        [Fact]
        public void ShouldCleanupDepletedGuardParty_NullParty_ReturnsFalse()
        {
            bool shouldCleanup = GuardPartyManager.ShouldCleanupDepletedGuardParty(null);

            Assert.False(shouldCleanup);
        }

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(5, 0, true)]
        [InlineData(5, 2, false)]
        public void ShouldCleanupDepletedGuardParty_CountsReflectWoundedOnlyZeroSizeBehavior(int totalTroops, int healthyTroops, bool expected)
        {
            bool shouldCleanup = GuardPartyManager.ShouldCleanupDepletedGuardParty(totalTroops, healthyTroops);

            Assert.Equal(expected, shouldCleanup);
        }

        [Fact]
        public void BuildGuardPartyName_UsesSettlementName()
        {
            string partyName = GuardPartyManager.BuildGuardPartyName("Ortysia");

            Assert.Equal("Ortysia Guard Party", partyName);
        }

        [Fact]
        public void BuildGuardPartyName_UsesFallbackWhenSettlementNameMissing()
        {
            string partyName = GuardPartyManager.BuildGuardPartyName(null);

            Assert.Equal("Settlement Guard Party", partyName);
        }
    }
}