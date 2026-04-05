using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ImprovedGarrisons
{
    /// <summary>
    /// Manages the lifecycle and AI behavior of guard parties. Guard parties
    /// are spawned from garrisons to patrol and defend the settlement's region.
    /// </summary>
    internal static class GuardPartyManager
    {
        private const float GuardPartyBaseSpeed = 1f;
        private const float GuardPartySpawnRadius = 0.5f;
        private const float PositionEpsilon = 0.001f;
        private const string GuardPartyNameSuffix = " Guard Party";

        /// <summary>
        /// Creates or maintains a guard party for the given settlement.
        /// If one already exists and is active, it will be refilled if configured.
        /// </summary>
        internal static void MaintainGuardParty(Settlement settlement, GarrisonSettings settings)
        {
            if (settlement == null || settings == null)
                return;

            try
            {
                var guardParty = FindGuardParty(settlement);

                if (TryHandleDisabledGuardParty(settlement, settings, guardParty))
                {
                    return;
                }

                guardParty = NormalizeGuardParty(settlement, guardParty);
                guardParty = EnsureGuardParty(settlement, settings, guardParty);
                if (guardParty == null)
                {
                    return;
                }

                TrimGuardPartyIfNeeded(settlement, guardParty, settings);
                RefillGuardPartyIfEnabled(settlement, guardParty, settings);

                SetPatrolBehavior(guardParty, settlement);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $"Improved Garrisons guard party error at {settlement.Name}: {ex.Message}",
                        Colors.Red));
            }
        }

        /// <summary>
        /// Creates a new guard party by transferring troops from the garrison.
        /// </summary>
        internal static MobileParty CreateGuardParty(Settlement settlement, GarrisonSettings settings)
        {
            if (settlement?.Town?.GarrisonParty == null || settings == null || settlement.OwnerClan == null)
            {
                return null;
            }

            var garrison = settlement.Town.GarrisonParty;
            int troopsToTransfer = GarrisonManager.CalculateGuardPartyRefill(
                garrison.MemberRoster.TotalManCount,
                guardPartyCount: 0,
                settings.GuardPartyMaxSize,
                settings.RecruitmentThreshold);

            if (troopsToTransfer <= 0)
            {
                return null;
            }

            var guardPartyRoster = TroopRoster.CreateDummyTroopRoster();
            var prisonerRoster = TroopRoster.CreateDummyTroopRoster();

            TransferTroops(garrison.MemberRoster, guardPartyRoster, troopsToTransfer);
            if (guardPartyRoster.TotalManCount <= 0)
            {
                return null;
            }

            var partyName = BuildGuardPartyNameText(settlement);
            Hero owner = settlement.OwnerClan.Leader;

            try
            {
                var guardParty = CustomPartyComponent.CreateCustomPartyWithTroopRoster(
                    GetSpawnPosition(settlement),
                    GuardPartySpawnRadius,
                    settlement,
                    partyName,
                    settlement.OwnerClan,
                    guardPartyRoster,
                    prisonerRoster,
                    owner,
                    null,
                    null,
                    GuardPartyBaseSpeed,
                    avoidHostileActions: false);

                if (guardParty == null)
                {
                    TransferTroops(guardPartyRoster, garrison.MemberRoster, guardPartyRoster.TotalManCount);
                    return null;
                }

                guardParty.ActualClan = settlement.OwnerClan;
                guardParty.Aggressiveness = 0.75f;
                guardParty.ShouldJoinPlayerBattles = true;
                guardParty.SetCustomHomeSettlement(settlement);
                guardParty.Party.SetCustomName(partyName);
                guardParty.Party.SetCustomOwner(owner);
                if (settlement.OwnerClan.Banner != null)
                {
                    guardParty.Party.SetCustomBanner(settlement.OwnerClan.Banner);
                }

                guardParty.Party.SetVisualAsDirty();

                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $"Improved Garrisons: Created a guard party of {guardParty.MemberRoster.TotalManCount} troops for {settlement.Name}.",
                        Colors.Yellow));

                return guardParty;
            }
            catch
            {
                TransferTroops(guardPartyRoster, garrison.MemberRoster, guardPartyRoster.TotalManCount);
                throw;
            }
        }

        /// <summary>
        /// Refills an existing guard party from the garrison up to its max size.
        /// </summary>
        internal static int RefillGuardParty(Settlement settlement, MobileParty guardParty, GarrisonSettings settings)
        {
            if (settlement?.Town?.GarrisonParty == null || guardParty?.MemberRoster == null || settings == null)
            {
                return 0;
            }

            if (settlement.IsUnderSiege)
            {
                return 0;
            }

            var garrisonRoster = settlement.Town.GarrisonParty.MemberRoster;
            int troopsToTransfer = GarrisonManager.CalculateGuardPartyRefill(
                garrisonRoster.TotalManCount,
                guardParty.MemberRoster.TotalManCount,
                settings.GuardPartyMaxSize,
                settings.RecruitmentThreshold);

            if (troopsToTransfer <= 0)
            {
                return 0;
            }

            TransferTroops(garrisonRoster, guardParty.MemberRoster, troopsToTransfer);
            return troopsToTransfer;
        }

        /// <summary>
        /// Sets the guard party to patrol around its home settlement.
        /// </summary>
        internal static void SetPatrolBehavior(MobileParty party, Settlement settlement)
        {
            if (party == null || settlement == null || !party.IsActive)
            {
                return;
            }

            if (settlement.IsUnderSiege)
            {
                party.SetMoveDefendSettlement(settlement, false, MobileParty.NavigationType.Default);
                return;
            }

            party.SetMovePatrolAroundSettlement(settlement, MobileParty.NavigationType.Default, false);
        }

        /// <summary>
        /// Removes all guard parties, typically called during mod uninstall.
        /// </summary>
        internal static void CleanupOrphanedGuardParties()
        {
            foreach (var guardParty in MobileParty.AllCustomParties.Where(IsManagedGuardParty).ToList())
            {
                Settlement homeSettlement = guardParty.HomeSettlement;
                if (homeSettlement == null || (!homeSettlement.IsTown && !homeSettlement.IsCastle))
                {
                    DisbandGuardParty(homeSettlement, guardParty, returnTroopsToGarrison: false, showMessage: false);
                    continue;
                }

                if (!ImprovedGarrisonsCampaignBehavior.IsPlayerFief(homeSettlement))
                {
                    DisbandGuardParty(homeSettlement, guardParty, returnTroopsToGarrison: false, showMessage: false);
                }
            }
        }

        /// <summary>
        /// Removes all active guard parties created by this mod.
        /// </summary>
        internal static void DisbandAllGuardParties()
        {
            foreach (var guardParty in MobileParty.AllCustomParties.Where(IsManagedGuardParty).ToList())
            {
                DisbandGuardParty(guardParty.HomeSettlement, guardParty, returnTroopsToGarrison: false, showMessage: false);
            }
        }

        /// <summary>
        /// Transfers troops from one roster to another, up to the specified count.
        /// Takes troops proportionally from what is available.
        /// </summary>
        internal static void TransferTroops(TroopRoster source, TroopRoster destination, int count)
        {
            int transferred = 0;
            var elements = source.GetTroopRoster().ToList();

            foreach (var element in elements)
            {
                if (transferred >= count) break;
                if (element.Character == null || element.Character.IsHero) continue;
                if (element.Number <= 0) continue;

                int toTake = Math.Min(element.Number, count - transferred);
                source.AddToCounts(element.Character, -toTake);
                destination.AddToCounts(element.Character, toTake);
                transferred += toTake;
            }
        }

        /// <summary>
        /// Builds the user-visible name for a guard party belonging to a settlement.
        /// </summary>
        internal static string BuildGuardPartyName(string settlementName)
        {
            string resolvedSettlementName = string.IsNullOrWhiteSpace(settlementName)
                ? "Settlement"
                : settlementName;

            return $"{resolvedSettlementName}{GuardPartyNameSuffix}";
        }

        /// <summary>
        /// Returns the active troop count for the guard party assigned to a settlement.
        /// </summary>
        /// <param name="settlement">The settlement whose guard party should be inspected.</param>
        /// <returns>The number of active guard troops, or 0 when none exist.</returns>
        internal static int GetActiveGuardPartySize(Settlement settlement)
        {
            var guardParty = FindGuardParty(settlement);
            if (guardParty?.IsActive != true || guardParty.MemberRoster == null)
            {
                return 0;
            }

            return guardParty.MemberRoster.TotalManCount;
        }

        private static MobileParty FindGuardParty(Settlement settlement)
        {
            if (settlement == null)
            {
                return null;
            }

            foreach (var party in MobileParty.AllCustomParties)
            {
                if (party?.HomeSettlement == settlement && IsManagedGuardParty(party))
                {
                    return party;
                }
            }

            return null;
        }

        private static bool TryHandleDisabledGuardParty(Settlement settlement, GarrisonSettings settings, MobileParty guardParty)
        {
            if (settings.GuardPartyEnabled)
            {
                return false;
            }

            if (guardParty != null)
            {
                DisbandGuardParty(settlement, guardParty, returnTroopsToGarrison: true, showMessage: true);
            }

            return true;
        }

        private static MobileParty NormalizeGuardParty(Settlement settlement, MobileParty guardParty)
        {
            if (guardParty == null)
            {
                return null;
            }

            if (!guardParty.IsActive || guardParty.MemberRoster == null)
            {
                return null;
            }

            if (guardParty.MemberRoster.TotalManCount > 0)
            {
                return guardParty;
            }

            DisbandGuardParty(settlement, guardParty, returnTroopsToGarrison: false, showMessage: false);
            return null;
        }

        private static MobileParty EnsureGuardParty(Settlement settlement, GarrisonSettings settings, MobileParty guardParty)
        {
            if (guardParty != null || settlement.IsUnderSiege)
            {
                return guardParty;
            }

            return CreateGuardParty(settlement, settings);
        }

        private static void RefillGuardPartyIfEnabled(Settlement settlement, MobileParty guardParty, GarrisonSettings settings)
        {
            if (!settings.GuardPartyAutoRefill)
            {
                return;
            }

            int refilled = RefillGuardParty(settlement, guardParty, settings);
            if (refilled <= 0)
            {
                return;
            }

            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: Refilled {refilled} guard troops for {settlement.Name}.",
                    Colors.Yellow));
        }

        private static void TrimGuardPartyIfNeeded(Settlement settlement, MobileParty guardParty, GarrisonSettings settings)
        {
            int returnedTroops = ReturnExcessGuardTroopsToGarrison(settlement, guardParty, settings);
            if (returnedTroops <= 0)
            {
                return;
            }

            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: Returned {returnedTroops} guard troops to {settlement.Name}.",
                    Colors.Yellow));
        }

        private static int ReturnExcessGuardTroopsToGarrison(Settlement settlement, MobileParty guardParty, GarrisonSettings settings)
        {
            if (settlement?.Town?.GarrisonParty == null || guardParty?.MemberRoster == null || settings == null)
            {
                return 0;
            }

            int excessTroops = guardParty.MemberRoster.TotalManCount - settings.GuardPartyMaxSize;
            if (excessTroops <= 0)
            {
                return 0;
            }

            TransferTroops(
                guardParty.MemberRoster,
                settlement.Town.GarrisonParty.MemberRoster,
                excessTroops);

            return excessTroops;
        }

        private static bool IsManagedGuardParty(MobileParty party)
        {
            if (party == null || !party.IsActive)
            {
                return false;
            }

            if (!(party.PartyComponent is CustomPartyComponent customPartyComponent))
            {
                return false;
            }

            if (customPartyComponent.HomeSettlement == null)
            {
                return false;
            }

            string partyName = party.Name?.ToString();
            return !string.IsNullOrWhiteSpace(partyName)
                && partyName.EndsWith(GuardPartyNameSuffix, StringComparison.Ordinal)
                && party.ActualClan == customPartyComponent.HomeSettlement.OwnerClan;
        }

        private static void DisbandGuardParty(
            Settlement settlement,
            MobileParty guardParty,
            bool returnTroopsToGarrison,
            bool showMessage)
        {
            if (guardParty == null)
            {
                return;
            }

            if (returnTroopsToGarrison && settlement?.Town?.GarrisonParty != null)
            {
                if (guardParty.MemberRoster != null)
                {
                    TransferTroops(
                        guardParty.MemberRoster,
                        settlement.Town.GarrisonParty.MemberRoster,
                        guardParty.MemberRoster.TotalManCount);
                }

                if (guardParty.PrisonRoster != null && settlement.Party?.PrisonRoster != null)
                {
                    TransferTroops(
                        guardParty.PrisonRoster,
                        settlement.Party.PrisonRoster,
                        guardParty.PrisonRoster.TotalManCount);
                }
            }

            PartyBase destroyerParty = settlement?.Town?.GarrisonParty?.Party ?? PartyBase.MainParty;
            if (destroyerParty != null)
            {
                DestroyPartyAction.Apply(destroyerParty, guardParty);
            }
            else if (settlement != null)
            {
                DestroyPartyAction.ApplyForDisbanding(guardParty, settlement);
            }

            if (showMessage && settlement != null)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $"Improved Garrisons: Disbanded the guard party for {settlement.Name}.",
                        Colors.Yellow));
            }
        }

        private static TextObject BuildGuardPartyNameText(Settlement settlement)
        {
            return new TextObject(BuildGuardPartyName(settlement?.Name?.ToString()));
        }

        private static CampaignVec2 GetSpawnPosition(Settlement settlement)
        {
            CampaignVec2 gatePosition = settlement.GatePosition;
            if (Math.Abs(gatePosition.X) > PositionEpsilon || Math.Abs(gatePosition.Y) > PositionEpsilon)
            {
                return gatePosition;
            }

            return settlement.Position;
        }
    }
}
