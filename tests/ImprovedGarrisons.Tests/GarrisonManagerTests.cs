using ImprovedGarrisons;
using Xunit;

namespace ImprovedGarrisons.Tests
{
    public class GarrisonManagerTests
    {
        [Theory]
        [InlineData(80, 0, 30, 100, 30)]
        [InlineData(80, 20, 30, 100, 10)]
        [InlineData(80, 30, 30, 100, 0)]
        [InlineData(40, 0, 30, 100, 0)]
        [InlineData(200, 0, 30, 100, 30)]
        [InlineData(60, 10, 30, 100, 10)]
        public void CalculateGuardPartyRefill_ReturnsExpectedCount(
            int garrisonCount,
            int guardPartyCount,
            int guardPartyMaxSize,
            int recruitmentThreshold,
            int expected)
        {
            int result = GarrisonManager.CalculateGuardPartyRefill(
                garrisonCount, guardPartyCount, guardPartyMaxSize, recruitmentThreshold);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void CalculateGuardPartyRefill_GuardPartyFull_ReturnsZero()
        {
            int result = GarrisonManager.CalculateGuardPartyRefill(
                garrisonCount: 100,
                guardPartyCount: 30,
                guardPartyMaxSize: 30,
                recruitmentThreshold: 100);

            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateGuardPartyRefill_GarrisonTooSmall_ReturnsZero()
        {
            int result = GarrisonManager.CalculateGuardPartyRefill(
                garrisonCount: 20,
                guardPartyCount: 0,
                guardPartyMaxSize: 30,
                recruitmentThreshold: 100);

            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateGuardPartyRefill_LargeGarrison_CapsAtMaxSize()
        {
            int result = GarrisonManager.CalculateGuardPartyRefill(
                garrisonCount: 500,
                guardPartyCount: 0,
                guardPartyMaxSize: 30,
                recruitmentThreshold: 100);

            Assert.Equal(30, result);
        }

        [Fact]
        public void GetNearbyVillages_NullSettlement_ReturnsEmpty()
        {
            var result = GarrisonManager.GetNearbyVillages(null);

            Assert.Empty(result);
        }

        [Fact]
        public void RecruitFromNearbyVillages_NullSettlement_ReturnsZero()
        {
            int result = GarrisonManager.RecruitFromNearbyVillages(null, new GarrisonSettings());

            Assert.Equal(0, result);
        }

        [Fact]
        public void RecruitPrisoners_NullSettlement_ReturnsZero()
        {
            int result = GarrisonManager.RecruitPrisoners(null, new GarrisonSettings());

            Assert.Equal(0, result);
        }

        [Fact]
        public void TrainGarrison_NullSettlement_ReturnsZero()
        {
            int result = GarrisonManager.TrainGarrison(null);

            Assert.Equal(0, result);
        }
    }
}
