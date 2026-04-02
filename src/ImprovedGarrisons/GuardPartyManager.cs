using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ImprovedGarrisons
{
    /// <summary>
    /// Manages the lifecycle and AI behavior of guard parties. Guard parties
    /// are spawned from garrisons to patrol and defend the settlement's region.
    /// </summary>
    internal class GuardPartyManager
    {
        private readonly Dictionary<string, MobileParty> _guardParties = new Dictionary<string, MobileParty>();

        /// <summary>
        /// Creates or maintains a guard party for the given settlement.
        /// If one already exists and is active, it will be refilled if configured.
        /// </summary>
        internal void MaintainGuardParty(Settlement settlement, GarrisonSettings settings)
        {
            if (!settings.GuardPartyEnabled) return;

            string key = settlement.StringId;

            if (_guardParties.TryGetValue(key, out var existingParty))
            {
                if (existingParty == null || existingParty.IsRemoved)
                {
                    _guardParties.Remove(key);
                }
                else
                {
                    if (settings.GuardPartyAutoRefill)
                    {
                        RefillGuardParty(settlement, existingParty, settings);
                    }
                    SetPatrolBehavior(existingParty, settlement);
                    return;
                }
            }

            var guardParty = CreateGuardParty(settlement, settings);
            if (guardParty != null)
            {
                _guardParties[key] = guardParty;
            }
        }

        /// <summary>
        /// Creates a new guard party by transferring troops from the garrison.
        /// </summary>
        internal MobileParty CreateGuardParty(Settlement settlement, GarrisonSettings settings)
        {
            if (settlement?.Town?.GarrisonParty == null) return null;

            var garrison = settlement.Town.GarrisonParty;
            int garrisonCount = garrison.MemberRoster.TotalManCount;
            int toTransfer = GarrisonManager.CalculateGuardPartyRefill(
                garrisonCount, 0, settings.GuardPartyMaxSize, settings.RecruitmentThreshold);

            if (toTransfer <= 0) return null;

            var component = new GuardPartyComponent(settlement);
            var party = MobileParty.CreateParty($"improved_garrisons_guard_{settlement.StringId}",
                component, delegate (MobileParty p) { p.ActualClan = settlement.OwnerClan; });

            TransferTroops(garrison.MemberRoster, party.MemberRoster, toTransfer);
            party.InitializeMobilePartyAtPosition(settlement.Culture.EliteBasicTroop,
                settlement.GatePosition);

            SetPatrolBehavior(party, settlement);

            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: Guard party created for {settlement.Name} with {party.MemberRoster.TotalManCount} troops.",
                    Colors.Green));

            return party;
        }

        /// <summary>
        /// Refills an existing guard party from the garrison up to its max size.
        /// </summary>
        internal void RefillGuardParty(Settlement settlement, MobileParty guardParty, GarrisonSettings settings)
        {
            if (settlement?.Town?.GarrisonParty == null) return;

            var garrison = settlement.Town.GarrisonParty;
            int garrisonCount = garrison.MemberRoster.TotalManCount;
            int guardCount = guardParty.MemberRoster.TotalManCount;

            int toTransfer = GarrisonManager.CalculateGuardPartyRefill(
                garrisonCount, guardCount, settings.GuardPartyMaxSize, settings.RecruitmentThreshold);

            if (toTransfer > 0)
            {
                TransferTroops(garrison.MemberRoster, guardParty.MemberRoster, toTransfer);
            }
        }

        /// <summary>
        /// Sets the guard party to patrol around its home settlement.
        /// </summary>
        internal static void SetPatrolBehavior(MobileParty party, Settlement settlement)
        {
            if (party == null || settlement == null) return;
            party.SetMovePatrolAroundSettlement(settlement);
        }

        /// <summary>
        /// Removes all guard parties, typically called during mod uninstall.
        /// </summary>
        internal void DisbandAllGuardParties()
        {
            foreach (var kvp in _guardParties.ToList())
            {
                if (kvp.Value != null && !kvp.Value.IsRemoved)
                {
                    kvp.Value.RemoveParty();
                }
            }
            _guardParties.Clear();
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
    }
}
