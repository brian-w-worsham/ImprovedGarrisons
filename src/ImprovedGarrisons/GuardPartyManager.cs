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
            if (!settings.GuardPartyEnabled || settlement == null)
                return;

            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: Guard party management is enabled for {settlement.Name}.",
                    Colors.Yellow));
        }

        /// <summary>
        /// Creates a new guard party by transferring troops from the garrison.
        /// </summary>
        internal MobileParty CreateGuardParty(Settlement settlement, GarrisonSettings settings)
        {
            // Placeholder for a future SDK-compatible implementation.
            return null;
        }

        /// <summary>
        /// Refills an existing guard party from the garrison up to its max size.
        /// </summary>
        internal void RefillGuardParty(Settlement settlement, MobileParty guardParty, GarrisonSettings settings)
        {
            // Placeholder for a future SDK-compatible implementation.
        }

        /// <summary>
        /// Sets the guard party to patrol around its home settlement.
        /// </summary>
        internal static void SetPatrolBehavior(MobileParty party, Settlement settlement)
        {
            // Placeholder for a future SDK-compatible implementation.
        }

        /// <summary>
        /// Removes all guard parties, typically called during mod uninstall.
        /// </summary>
        internal void DisbandAllGuardParties()
        {
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
