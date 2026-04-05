using ImprovedGarrisons;
using Xunit;

namespace ImprovedGarrisons.Tests
{
    public class GuardPartyManagerTests
    {
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