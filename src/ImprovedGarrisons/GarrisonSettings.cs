using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedGarrisons
{
    /// <summary>
    /// Per-fief configuration for recruitment, training, and guard-party behavior.
    /// Stores either explicit numeric overrides or automatic per-settlement defaults.
    /// </summary>
    internal class GarrisonSettings
    {
        internal const int AutomaticRecruitmentThreshold = 0;
        internal const int AutomaticGuardPartyMaxSize = 0;
        internal const int MinRecruitmentThreshold = 25;
        internal const int MaxRecruitmentThreshold = 500;
        internal const int RecruitmentThresholdStep = 25;
        internal const int MinGuardPartyMaxSize = 10;
        internal const int MaxGuardPartyMaxSize = 200;
        internal const int GuardPartyMaxSizeStep = 5;

        /// <summary>Whether automatic recruitment is enabled for this garrison.</summary>
        public bool AutoRecruitEnabled { get; set; } = true;

        /// <summary>Whether automatic prisoner recruitment is enabled.</summary>
        public bool AutoRecruitPrisonersEnabled { get; set; } = true;

        /// <summary>Whether automatic troop training/upgrading is enabled.</summary>
        public bool AutoTrainingEnabled { get; set; } = true;

        /// <summary>Whether a guard party should be created for this garrison.</summary>
        public bool GuardPartyEnabled { get; set; } = true;

        /// <summary>
        /// Maximum garrison size before auto-recruitment stops.
        /// A value of 0 keeps the threshold automatic and resolves to the
        /// settlement's current garrison party size limit.
        /// </summary>
        public int RecruitmentThreshold { get; set; } = AutomaticRecruitmentThreshold;

        /// <summary>
        /// Maximum number of troops allowed in the guard party.
        /// A value of 0 keeps the size automatic and resolves to 25% of the
        /// settlement's current defensive troops, clamped to the supported range.
        /// </summary>
        public int GuardPartyMaxSize { get; set; } = AutomaticGuardPartyMaxSize;

        /// <summary>Whether the guard party should automatically refill from the garrison.</summary>
        public bool GuardPartyAutoRefill { get; set; } = true;

        /// <summary>Whether to only recruit elite (tier 3+) troops.</summary>
        public bool RecruitEliteOnly { get; set; }

        /// <summary>Daily gold cost cap for auto-recruitment. 0 = unlimited.</summary>
        public int DailyRecruitBudget { get; set; }

        /// <summary>
        /// Normalizes persisted numeric values into the supported in-game range while
        /// preserving the automatic guard-party size sentinel.
        /// </summary>
        internal void Normalize()
        {
            RecruitmentThreshold = UsesAutomaticRecruitmentThreshold
                ? AutomaticRecruitmentThreshold
                : Math.Max(MinRecruitmentThreshold, RecruitmentThreshold);
            GuardPartyMaxSize = UsesAutomaticGuardPartyMaxSize
                ? AutomaticGuardPartyMaxSize
                : ClampGuardPartyMaxSize(GuardPartyMaxSize);
        }

        /// <summary>
        /// Gets a value indicating whether the recruitment threshold is using the
        /// automatic per-settlement default instead of an explicit override.
        /// </summary>
        internal bool UsesAutomaticRecruitmentThreshold => RecruitmentThreshold <= AutomaticRecruitmentThreshold;

        /// <summary>
        /// Gets a value indicating whether the guard-party max size is using the
        /// automatic per-settlement default instead of an explicit override.
        /// </summary>
        internal bool UsesAutomaticGuardPartyMaxSize => GuardPartyMaxSize <= AutomaticGuardPartyMaxSize;

        /// <summary>
        /// Resolves the effective recruitment threshold for the given settlement.
        /// Uses the stored override when present; otherwise uses the settlement's
        /// current garrison party size limit.
        /// </summary>
        /// <param name="settlement">The settlement whose garrison capacity should be used.</param>
        /// <returns>The effective threshold after applying automatic defaults and clamps.</returns>
        internal int ResolveRecruitmentThreshold(Settlement settlement)
        {
            return ResolveRecruitmentThreshold(GetMaximumGarrisonCapacity(settlement));
        }

        /// <summary>
        /// Resolves the effective recruitment threshold for the current maximum
        /// garrison capacity.
        /// </summary>
        /// <param name="maximumGarrisonCapacity">The current garrison troop limit for the settlement.</param>
        /// <returns>The effective threshold after applying automatic defaults and clamps.</returns>
        internal int ResolveRecruitmentThreshold(int maximumGarrisonCapacity)
        {
            return UsesAutomaticRecruitmentThreshold
                ? CalculateAutomaticRecruitmentThreshold(maximumGarrisonCapacity)
                : ClampRecruitmentThreshold(RecruitmentThreshold, maximumGarrisonCapacity);
        }

        /// <summary>
        /// Resolves the effective guard-party max size for the current settlement troop pool.
        /// Uses the stored override when present; otherwise computes the automatic 25% value.
        /// </summary>
        /// <param name="totalDefensiveTroopCount">Current troops across the garrison and guard party.</param>
        /// <returns>The effective max size after applying automatic defaults and clamps.</returns>
        internal int ResolveGuardPartyMaxSize(int totalDefensiveTroopCount)
        {
            return UsesAutomaticGuardPartyMaxSize
                ? CalculateAutomaticGuardPartyMaxSize(totalDefensiveTroopCount)
                : ClampGuardPartyMaxSize(GuardPartyMaxSize);
        }

        /// <summary>
        /// Clears any explicit guard-party max size override and restores automatic sizing.
        /// </summary>
        internal void ResetGuardPartyMaxSize()
        {
            GuardPartyMaxSize = AutomaticGuardPartyMaxSize;
        }

        /// <summary>
        /// Clears any explicit recruitment threshold override and restores the
        /// automatic settlement-capacity default.
        /// </summary>
        internal void ResetRecruitmentThreshold()
        {
            RecruitmentThreshold = AutomaticRecruitmentThreshold;
        }

        /// <summary>
        /// Adjusts the recruitment threshold and clamps it to the supported range.
        /// </summary>
        /// <param name="delta">The amount to add to the current threshold.</param>
        /// <param name="maximumGarrisonCapacity">The current garrison troop limit for the settlement.</param>
        /// <returns>The normalized threshold after the change.</returns>
        internal int AdjustRecruitmentThreshold(int delta, int maximumGarrisonCapacity)
        {
            RecruitmentThreshold = ClampRecruitmentThreshold(
                ResolveRecruitmentThreshold(maximumGarrisonCapacity) + delta,
                maximumGarrisonCapacity);
            return RecruitmentThreshold;
        }

        /// <summary>
        /// Adjusts the effective guard-party max size and stores the result as an explicit override.
        /// If the setting is currently automatic, the resolved automatic size is used as the starting value.
        /// </summary>
        /// <param name="delta">The amount to add to the current max size.</param>
        /// <param name="totalDefensiveTroopCount">Current troops across the garrison and guard party.</param>
        /// <returns>The normalized explicit max size after the change.</returns>
        internal int AdjustGuardPartyMaxSize(int delta, int totalDefensiveTroopCount)
        {
            GuardPartyMaxSize = ClampGuardPartyMaxSize(ResolveGuardPartyMaxSize(totalDefensiveTroopCount) + delta);
            return GuardPartyMaxSize;
        }

        /// <summary>
        /// Clamps a recruitment threshold into the supported range.
        /// </summary>
        /// <param name="value">The requested threshold.</param>
        /// <returns>A valid threshold value.</returns>
        internal static int ClampRecruitmentThreshold(int value)
        {
            return Math.Max(MinRecruitmentThreshold, Math.Min(MaxRecruitmentThreshold, value));
        }

        /// <summary>
        /// Clamps a recruitment threshold into the supported range for the
        /// current settlement's garrison capacity.
        /// </summary>
        /// <param name="value">The requested threshold.</param>
        /// <param name="maximumGarrisonCapacity">The current garrison troop limit for the settlement.</param>
        /// <returns>A valid threshold value.</returns>
        internal static int ClampRecruitmentThreshold(int value, int maximumGarrisonCapacity)
        {
            int maximumThreshold = GetMaximumRecruitmentThreshold(maximumGarrisonCapacity);
            return Math.Max(MinRecruitmentThreshold, Math.Min(maximumThreshold, value));
        }

        /// <summary>
        /// Calculates the automatic recruitment threshold from the settlement's
        /// current garrison party size limit.
        /// </summary>
        /// <param name="settlement">The settlement whose garrison capacity should be used.</param>
        /// <returns>A valid automatic recruitment threshold.</returns>
        internal static int CalculateAutomaticRecruitmentThreshold(Settlement settlement)
        {
            return CalculateAutomaticRecruitmentThreshold(GetMaximumGarrisonCapacity(settlement));
        }

        /// <summary>
        /// Calculates the automatic recruitment threshold from the current
        /// garrison party size limit.
        /// </summary>
        /// <param name="maximumGarrisonCapacity">The current garrison troop limit for the settlement.</param>
        /// <returns>A valid automatic recruitment threshold.</returns>
        internal static int CalculateAutomaticRecruitmentThreshold(int maximumGarrisonCapacity)
        {
            return GetMaximumRecruitmentThreshold(maximumGarrisonCapacity);
        }

        /// <summary>
        /// Resolves the current garrison troop limit for a settlement.
        /// Falls back to the legacy maximum when the game model is unavailable.
        /// </summary>
        /// <param name="settlement">The settlement whose capacity should be resolved.</param>
        /// <returns>The settlement's current garrison troop limit.</returns>
        internal static int GetMaximumGarrisonCapacity(Settlement settlement)
        {
            int livePartyLimit = settlement?.Town?.GarrisonParty?.Party?.PartySizeLimit ?? 0;
            if (livePartyLimit > 0)
            {
                return Math.Max(MinRecruitmentThreshold, livePartyLimit);
            }

            if (settlement != null)
            {
                var partySizeLimitModel = Campaign.Current?.Models?.PartySizeLimitModel;
                if (partySizeLimitModel != null)
                {
                    float modeledPartyLimit = partySizeLimitModel
                        .CalculateGarrisonPartySizeLimit(settlement)
                        .ResultNumber;
                    if (modeledPartyLimit > 0f)
                    {
                        return Math.Max(MinRecruitmentThreshold, (int)Math.Round(modeledPartyLimit));
                    }
                }
            }

            return MaxRecruitmentThreshold;
        }

        /// <summary>
        /// Returns the highest valid recruitment threshold for the current
        /// settlement's garrison capacity.
        /// </summary>
        /// <param name="maximumGarrisonCapacity">The current garrison troop limit for the settlement.</param>
        /// <returns>The maximum supported recruitment threshold.</returns>
        internal static int GetMaximumRecruitmentThreshold(int maximumGarrisonCapacity)
        {
            return Math.Max(MinRecruitmentThreshold, maximumGarrisonCapacity);
        }

        /// <summary>
        /// Calculates the automatic guard-party max size from the current defensive troop count.
        /// The automatic value is one quarter of the troop count, clamped to the supported range.
        /// </summary>
        /// <param name="totalDefensiveTroopCount">Current troops across the garrison and guard party.</param>
        /// <returns>A valid automatic guard-party size.</returns>
        internal static int CalculateAutomaticGuardPartyMaxSize(int totalDefensiveTroopCount)
        {
            return ClampGuardPartyMaxSize(Math.Max(0, totalDefensiveTroopCount) / 4);
        }

        /// <summary>
        /// Clamps a guard party size into the supported range.
        /// </summary>
        /// <param name="value">The requested max size.</param>
        /// <returns>A valid guard party size.</returns>
        internal static int ClampGuardPartyMaxSize(int value)
        {
            return Math.Max(MinGuardPartyMaxSize, Math.Min(MaxGuardPartyMaxSize, value));
        }
    }
}
