using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace ImprovedGarrisons
{
    /// <summary>
    /// A custom party component for guard parties that patrol around their
    /// home settlement, defending villages and engaging bandits.
    /// </summary>
    internal class GuardPartyComponent : PartyComponent
    {
        private Settlement _homeSettlement;

        public GuardPartyComponent(Settlement homeSettlement)
        {
            _homeSettlement = homeSettlement;
        }

        public Settlement HomeSettlement => _homeSettlement;

        public override Hero PartyOwner => _homeSettlement?.OwnerClan?.Leader;

        public override TextObject Name =>
            new TextObject($"{_homeSettlement?.Name} Guard Party");

        public override Settlement HomeSettlementOfMapFaction => _homeSettlement;
    }
}
