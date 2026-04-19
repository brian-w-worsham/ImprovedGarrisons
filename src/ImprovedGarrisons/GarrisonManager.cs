using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ImprovedGarrisons
{
    /// <summary>
    /// Core logic for garrison recruitment, training, and guard party management.
    /// Stateless methods allow straightforward unit testing.
    /// </summary>
    internal static class GarrisonManager
    {
        /// <summary>
        /// Recruits available troops from nearby villages into the garrison
        /// of <paramref name="settlement"/> up to the configured threshold.
        /// </summary>
        /// <returns>The number of troops recruited.</returns>
        internal static int RecruitFromNearbyVillages(Settlement settlement, GarrisonSettings settings)
        {
            if (settlement?.Town == null) return 0;

            var garrison = settlement.Town.GarrisonParty;
            if (garrison == null) return 0;

            int currentCount = garrison.MemberRoster.TotalManCount;
            int recruitmentThreshold = settings.ResolveRecruitmentThreshold(settlement);
            int remaining = recruitmentThreshold - currentCount;
            if (remaining <= 0) return 0;

            int totalRecruited = 0;
            var villages = GetNearbyVillages(settlement);

            foreach (var village in villages)
            {
                if (remaining <= 0) break;
                if (village.Settlement?.Notables == null) continue;

                foreach (var notable in village.Settlement.Notables)
                {
                    if (remaining <= 0) break;

                    var recruitable = GetRecruitableCharacters(notable, settings.RecruitEliteOnly);
                    foreach (var character in recruitable)
                    {
                        if (remaining <= 0) break;

                        int cost = Campaign.Current.Models.PartyWageModel.GetCharacterWage(character);
                        if (settings.DailyRecruitBudget > 0 && cost > settings.DailyRecruitBudget)
                            continue;

                        garrison.MemberRoster.AddToCounts(character, 1);
                        totalRecruited++;
                        remaining--;
                    }
                }
            }

            return totalRecruited;
        }

        /// <summary>
        /// Recruits prisoners held in the garrison's settlement into the garrison roster.
        /// </summary>
        /// <returns>The number of prisoners recruited.</returns>
        internal static int RecruitPrisoners(Settlement settlement, GarrisonSettings settings)
        {
            if (settlement?.Town == null) return 0;

            var garrison = settlement.Town.GarrisonParty;
            if (garrison == null) return 0;

            int currentCount = garrison.MemberRoster.TotalManCount;
            int recruitmentThreshold = settings.ResolveRecruitmentThreshold(settlement);
            int remaining = recruitmentThreshold - currentCount;
            if (remaining <= 0) return 0;

            var prisonRoster = settlement.Party?.PrisonRoster;
            if (prisonRoster == null || prisonRoster.TotalManCount == 0) return 0;

            int totalRecruited = 0;
            var prisonersToRecruit = new List<(CharacterObject character, int count)>();

            foreach (var element in prisonRoster.GetTroopRoster())
            {
                if (remaining <= 0) break;
                if (element.Character == null || element.Character.IsHero) continue;

                int count = Math.Min(element.Number, remaining);
                prisonersToRecruit.Add((element.Character, count));
                remaining -= count;
                totalRecruited += count;
            }

            foreach (var (character, count) in prisonersToRecruit)
            {
                garrison.MemberRoster.AddToCounts(character, count);
                prisonRoster.AddToCounts(character, -count);
            }

            return totalRecruited;
        }

        /// <summary>
        /// Upgrades troops in the garrison to their next tier when possible.
        /// </summary>
        /// <returns>The number of troops upgraded.</returns>
        internal static int TrainGarrison(Settlement settlement)
        {
            if (settlement?.Town == null) return 0;

            var garrison = settlement.Town.GarrisonParty;
            if (garrison == null) return 0;

            int totalUpgraded = 0;
            var upgrades = new List<(CharacterObject from, CharacterObject to, int count)>();

            foreach (var element in garrison.MemberRoster.GetTroopRoster())
            {
                if (element.Character == null || element.Character.IsHero) continue;

                var upgradeTargets = element.Character.UpgradeTargets;
                if (upgradeTargets == null || upgradeTargets.Length == 0) continue;

                int xpNeeded = element.Character.GetUpgradeXpCost(garrison.Party, 0);
                if (xpNeeded <= 0) xpNeeded = 1;

                int availableXp = element.Xp;
                int canUpgrade = Math.Min(availableXp / xpNeeded, element.Number);

                if (canUpgrade > 0)
                {
                    var target = upgradeTargets[0];
                    upgrades.Add((element.Character, target, canUpgrade));
                    totalUpgraded += canUpgrade;
                }
            }

            foreach (var (from, to, count) in upgrades)
            {
                garrison.MemberRoster.AddToCounts(from, -count);
                garrison.MemberRoster.AddToCounts(to, count);
            }

            return totalUpgraded;
        }

        /// <summary>
        /// Determines the number of guard party troops to transfer from the
        /// garrison to maintain the guard party at its target size.
        /// </summary>
        /// <returns>
        /// Number of troops needed or 0 if the guard party is already at capacity
        /// or the garrison cannot spare troops.
        /// </returns>
        internal static int CalculateGuardPartyRefill(
            int garrisonCount,
            int guardPartyCount,
            int guardPartyMaxSize,
            int recruitmentThreshold)
        {
            int needed = guardPartyMaxSize - guardPartyCount;
            if (needed <= 0) return 0;

            int spare = garrisonCount - (recruitmentThreshold / 2);
            if (spare <= 0) return 0;

            return Math.Min(needed, spare);
        }

        /// <summary>
        /// Returns villages that are bound to the given settlement.
        /// </summary>
        internal static IEnumerable<Village> GetNearbyVillages(Settlement settlement)
        {
            if (settlement?.BoundVillages == null)
                return Enumerable.Empty<Village>();

            return settlement.BoundVillages;
        }

        /// <summary>
        /// Returns characters available for recruitment from a notable,
        /// optionally filtered to elite (tier 3+) only.
        /// </summary>
        internal static IEnumerable<CharacterObject> GetRecruitableCharacters(
            Hero notable,
            bool eliteOnly)
        {
            if (notable == null) yield break;

            for (int i = 0; i < notable.VolunteerTypes.Length; i++)
            {
                var character = notable.VolunteerTypes[i];
                if (character == null) continue;

                if (eliteOnly && character.Tier < 3)
                    continue;

                yield return character;
            }
        }
    }
}
