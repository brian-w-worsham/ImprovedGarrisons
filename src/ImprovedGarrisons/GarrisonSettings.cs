using System;

namespace ImprovedGarrisons
{
    /// <summary>
    /// Per-fief configuration for garrison management features.
    /// </summary>
    internal class GarrisonSettings
    {
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
        /// Default 100 matches the original mod's default threshold.
        /// </summary>
        public int RecruitmentThreshold { get; set; } = 100;

        /// <summary>Maximum number of troops for the guard party.</summary>
        public int GuardPartyMaxSize { get; set; } = 30;

        /// <summary>Whether the guard party should automatically refill from the garrison.</summary>
        public bool GuardPartyAutoRefill { get; set; } = true;

        /// <summary>Whether to only recruit elite (tier 3+) troops.</summary>
        public bool RecruitEliteOnly { get; set; }

        /// <summary>Daily gold cost cap for auto-recruitment. 0 = unlimited.</summary>
        public int DailyRecruitBudget { get; set; }

        /// <summary>
        /// Clamps tunable numeric values into the supported in-game range.
        /// </summary>
        internal void Normalize()
        {
            RecruitmentThreshold = ClampRecruitmentThreshold(RecruitmentThreshold);
            GuardPartyMaxSize = ClampGuardPartyMaxSize(GuardPartyMaxSize);
        }

        /// <summary>
        /// Adjusts the recruitment threshold and clamps it to the supported range.
        /// </summary>
        /// <param name="delta">The amount to add to the current threshold.</param>
        /// <returns>The normalized threshold after the change.</returns>
        internal int AdjustRecruitmentThreshold(int delta)
        {
            RecruitmentThreshold = ClampRecruitmentThreshold(RecruitmentThreshold + delta);
            return RecruitmentThreshold;
        }

        /// <summary>
        /// Adjusts the guard party max size and clamps it to the supported range.
        /// </summary>
        /// <param name="delta">The amount to add to the current max size.</param>
        /// <returns>The normalized max size after the change.</returns>
        internal int AdjustGuardPartyMaxSize(int delta)
        {
            GuardPartyMaxSize = ClampGuardPartyMaxSize(GuardPartyMaxSize + delta);
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
