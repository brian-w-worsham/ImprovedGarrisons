namespace ImprovedGarrisons
{
    /// <summary>
    /// Per-fief configuration for garrison management features.
    /// </summary>
    internal class GarrisonSettings
    {
        /// <summary>Whether automatic recruitment is enabled for this garrison.</summary>
        public bool AutoRecruitEnabled { get; set; } = true;

        /// <summary>Whether automatic prisoner recruitment is enabled.</summary>
        public bool AutoRecruitPrisonersEnabled { get; set; } = true;

        /// <summary>Whether automatic troop training/upgrading is enabled.</summary>
        public bool AutoTrainingEnabled { get; set; } = true;

        /// <summary>Whether a guard party should be created for this garrison.</summary>
        public bool GuardPartyEnabled { get; set; }

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
    }
}
