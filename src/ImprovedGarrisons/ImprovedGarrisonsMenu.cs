using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ImprovedGarrisons
{
    /// <summary>
    /// Registers settlement menu entries that let the player configure guard parties per fief.
    /// </summary>
    internal static class ImprovedGarrisonsMenu
    {
        private enum GuardSettingsInquiryAction
        {
            ToggleGuardParties,
            ToggleAutoRefill,
            IncreaseGuardPartySize,
            DecreaseGuardPartySize,
            ResetGuardPartySize,
            IncreaseReserveThreshold,
            DecreaseReserveThreshold,
            ResetReserveThreshold,
            ApplyNow
        }

        private const string TownMenuId = "town_keep";
        private const string CastleMenuId = "castle";
        private const string GuardSettingsMenuId = "improved_garrisons_guard_settings";
        private const string GuardSettingsEntryText = "Improved Garrisons guard settings";
        private const string TownEntryOptionId = "improved_garrisons_guard_settings_town";
        private const string CastleEntryOptionId = "improved_garrisons_guard_settings_castle";
        private const string DynamicEntryOptionId = "improved_garrisons_guard_settings_dynamic";
        private const string ToggleGuardPartiesOptionId = "improved_garrisons_toggle_guard_parties";
        private const string ToggleAutoRefillOptionId = "improved_garrisons_toggle_guard_auto_refill";
        private const string IncreaseGuardPartySizeOptionId = "improved_garrisons_increase_guard_party_size";
        private const string DecreaseGuardPartySizeOptionId = "improved_garrisons_decrease_guard_party_size";
        private const string ResetGuardPartySizeOptionId = "improved_garrisons_reset_guard_party_size";
        private const string IncreaseReserveThresholdOptionId = "improved_garrisons_increase_reserve_threshold";
        private const string DecreaseReserveThresholdOptionId = "improved_garrisons_decrease_reserve_threshold";
        private const string ResetReserveThresholdOptionId = "improved_garrisons_reset_reserve_threshold";
        private const string ApplyNowOptionId = "improved_garrisons_apply_guard_settings";
        private const string BackOptionId = "improved_garrisons_guard_settings_back";
        private static readonly MethodInfo GameMenuAddOptionMethod = typeof(GameMenu).GetMethod(
            "AddOption",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(string),
                typeof(TextObject),
                typeof(GameMenuOption.OnConditionDelegate),
                typeof(GameMenuOption.OnConsequenceDelegate),
                typeof(int),
                typeof(bool),
                typeof(bool),
                typeof(object)
            },
            null);

        /// <summary>
        /// Adds the guard settings submenu and entry points to the campaign settlement menus.
        /// </summary>
        /// <param name="campaignStarter">The active campaign starter.</param>
        internal static void AddGameMenus(CampaignGameStarter campaignStarter)
        {
            if (campaignStarter == null)
            {
                return;
            }

            campaignStarter.AddGameMenu(
                GuardSettingsMenuId,
                "Configure guard parties for this fief.",
                OnGuardSettingsMenuInit,
                GameMenu.MenuOverlayType.None,
                GameMenu.MenuFlags.AutoSelectFirst);

            campaignStarter.AddGameMenuOption(
                TownMenuId,
                TownEntryOptionId,
                GuardSettingsEntryText,
                CanOpenGuardSettingsMenu,
                OpenGuardSettingsMenu,
                false,
                5,
                false);

            campaignStarter.AddGameMenuOption(
                CastleMenuId,
                CastleEntryOptionId,
                GuardSettingsEntryText,
                CanOpenGuardSettingsMenu,
                OpenGuardSettingsMenu,
                false,
                5,
                false);

            campaignStarter.AddGameMenuOption(
                GuardSettingsMenuId,
                ToggleGuardPartiesOptionId,
                "Toggle guard parties",
                CanToggleGuardParties,
                ToggleGuardParties,
                false,
                0,
                false);

            campaignStarter.AddGameMenuOption(
                GuardSettingsMenuId,
                ToggleAutoRefillOptionId,
                "Toggle auto-refill",
                CanToggleAutoRefill,
                ToggleAutoRefill,
                false,
                1,
                false);

            campaignStarter.AddGameMenuOption(
                GuardSettingsMenuId,
                IncreaseGuardPartySizeOptionId,
                "Increase max guard size",
                CanIncreaseGuardPartySize,
                IncreaseGuardPartySize,
                false,
                2,
                false);

            campaignStarter.AddGameMenuOption(
                GuardSettingsMenuId,
                DecreaseGuardPartySizeOptionId,
                "Decrease max guard size",
                CanDecreaseGuardPartySize,
                DecreaseGuardPartySize,
                false,
                3,
                false);

            campaignStarter.AddGameMenuOption(
                GuardSettingsMenuId,
                ResetGuardPartySizeOptionId,
                "Reset max guard size",
                CanResetGuardPartySize,
                ResetGuardPartySize,
                false,
                4,
                false);

            campaignStarter.AddGameMenuOption(
                GuardSettingsMenuId,
                IncreaseReserveThresholdOptionId,
                "Increase reserve threshold",
                CanIncreaseReserveThreshold,
                IncreaseReserveThreshold,
                false,
                5,
                false);

            campaignStarter.AddGameMenuOption(
                GuardSettingsMenuId,
                DecreaseReserveThresholdOptionId,
                "Decrease reserve threshold",
                CanDecreaseReserveThreshold,
                DecreaseReserveThreshold,
                false,
                6,
                false);

            campaignStarter.AddGameMenuOption(
                GuardSettingsMenuId,
                ResetReserveThresholdOptionId,
                "Reset reserve threshold",
                CanResetReserveThreshold,
                ResetReserveThreshold,
                false,
                7,
                false);

            campaignStarter.AddGameMenuOption(
                GuardSettingsMenuId,
                ApplyNowOptionId,
                "Apply current guard settings",
                CanApplyGuardSettingsNow,
                ApplyGuardSettingsNow,
                false,
                8,
                false);

            campaignStarter.AddGameMenuOption(
                GuardSettingsMenuId,
                BackOptionId,
                "Back",
                CanGoBack,
                GoBack,
                true,
                99,
                false);
        }

        /// <summary>
        /// Adds the guard settings option directly to an already initialized fortification menu.
        /// This avoids hard dependencies on specific castle menu ids across Bannerlord versions.
        /// </summary>
        /// <param name="gameMenu">The live game menu being initialized.</param>
        /// <returns><c>true</c> when the option was injected; otherwise <c>false</c>.</returns>
        internal static bool TryInjectGuardSettingsOption(GameMenu gameMenu)
        {
            if (gameMenu == null || string.Equals(gameMenu.StringId, GuardSettingsMenuId, StringComparison.Ordinal))
            {
                return false;
            }

            Settlement settlement = ResolveManagedSettlement(gameMenu);
            if (settlement == null)
            {
                return false;
            }

            bool alreadyPresent = gameMenu.MenuOptions.Any(option =>
                string.Equals(option.IdString, DynamicEntryOptionId, StringComparison.Ordinal)
                || string.Equals(option.IdString, TownEntryOptionId, StringComparison.Ordinal)
                || string.Equals(option.IdString, CastleEntryOptionId, StringComparison.Ordinal));

            if (alreadyPresent)
            {
                return false;
            }

            if (GameMenuAddOptionMethod == null)
            {
                return false;
            }

            GameMenuAddOptionMethod.Invoke(
                gameMenu,
                new object[]
                {
                    DynamicEntryOptionId,
                    new TextObject(GuardSettingsEntryText),
                    new GameMenuOption.OnConditionDelegate(CanOpenGuardSettingsMenu),
                    new GameMenuOption.OnConsequenceDelegate(OpenGuardSettingsMenu),
                    5,
                    false,
                    false,
                    settlement
                });

            return true;
        }

        /// <summary>
        /// Builds the title shown at the top of the guard settings menu.
        /// </summary>
        /// <param name="settlementName">The current settlement name.</param>
        /// <returns>A player-facing menu title.</returns>
        internal static string BuildGuardSettingsMenuTitle(string settlementName)
        {
            return $"Improved Garrisons: {ResolveSettlementName(settlementName)}";
        }

        /// <summary>
        /// Builds the body text for the guard settings submenu.
        /// </summary>
        /// <param name="settlementName">The current settlement name.</param>
        /// <param name="settings">The configured settings for the settlement.</param>
        /// <param name="activeGuardPartySize">The current active guard party size.</param>
        /// <param name="defensiveTroopCount">Current troops across the garrison and guard party.</param>
        /// <param name="maximumGarrisonCapacity">The settlement's current garrison troop limit.</param>
        /// <returns>A summary of the active settings and guard status.</returns>
        internal static string BuildGuardSettingsMenuText(string settlementName, GarrisonSettings settings, int activeGuardPartySize, int defensiveTroopCount, int maximumGarrisonCapacity)
        {
            var effectiveSettings = settings ?? new GarrisonSettings();
            string enabledText = effectiveSettings.GuardPartyEnabled ? "Enabled" : "Disabled";
            string autoRefillText = effectiveSettings.GuardPartyAutoRefill ? "Enabled" : "Disabled";
            int maxSize = effectiveSettings.ResolveGuardPartyMaxSize(defensiveTroopCount);
            string maxSizeText = BuildGuardPartySizeValueText(maxSize, effectiveSettings.UsesAutomaticGuardPartyMaxSize);
            int reserveThreshold = effectiveSettings.ResolveRecruitmentThreshold(maximumGarrisonCapacity);
            string reserveThresholdText = BuildReserveThresholdValueText(reserveThreshold, effectiveSettings.UsesAutomaticRecruitmentThreshold);
            string protectedGarrisonText = BuildProtectedGarrisonValueText(reserveThreshold);
            string activeGuardText = activeGuardPartySize > 0
                ? $"{activeGuardPartySize} troops deployed"
                : "None deployed";

            return $"Configure guard parties for {ResolveSettlementName(settlementName)}.{Environment.NewLine}{Environment.NewLine}"
                + $"Guard parties: {enabledText}{Environment.NewLine}"
                + $"Auto-refill: {autoRefillText}{Environment.NewLine}"
                + $"Max guard size: {maxSizeText}{Environment.NewLine}"
                + $"Auto-recruit target: {reserveThresholdText}{Environment.NewLine}"
                + $"Guard refill keeps at least: {protectedGarrisonText}{Environment.NewLine}"
                + $"Active guard party: {activeGuardText}";
        }

        /// <summary>
        /// Builds the display text for a guard-party max size value.
        /// </summary>
        /// <param name="currentValue">The effective current max size.</param>
        /// <param name="usesAutomaticValue">Whether the size is using automatic per-settlement sizing.</param>
        /// <returns>A player-facing display value.</returns>
        internal static string BuildGuardPartySizeValueText(int currentValue, bool usesAutomaticValue)
        {
            int normalizedCurrent = GarrisonSettings.ClampGuardPartyMaxSize(currentValue);
            return usesAutomaticValue
                ? $"Auto ({normalizedCurrent})"
                : normalizedCurrent.ToString();
        }

        /// <summary>
        /// Builds the display text for a reserve threshold value.
        /// </summary>
        /// <param name="currentValue">The effective current threshold.</param>
        /// <param name="usesAutomaticValue">Whether the threshold is using automatic settlement-capacity sizing.</param>
        /// <returns>A player-facing display value.</returns>
        internal static string BuildReserveThresholdValueText(int currentValue, bool usesAutomaticValue)
        {
            int normalizedCurrent = Math.Max(GarrisonSettings.MinRecruitmentThreshold, currentValue);
            return usesAutomaticValue
                ? $"Auto ({normalizedCurrent})"
                : normalizedCurrent.ToString();
        }

        /// <summary>
        /// Builds the display text for the number of troops guard refill will keep in the garrison.
        /// </summary>
        /// <param name="reserveThreshold">The effective recruitment threshold.</param>
        /// <returns>A player-facing description of the protected garrison amount.</returns>
        internal static string BuildProtectedGarrisonValueText(int reserveThreshold)
        {
            int protectedTroops = Math.Max(0, reserveThreshold / 2);
            return $"{protectedTroops} troops in garrison";
        }

        /// <summary>
        /// Builds the option label for toggling guard parties on or off.
        /// </summary>
        /// <param name="guardPartyEnabled">Whether guard parties are currently enabled.</param>
        /// <returns>A player-facing option label.</returns>
        internal static string BuildToggleGuardPartyOptionText(bool guardPartyEnabled)
        {
            return guardPartyEnabled ? "Disable guard parties" : "Enable guard parties";
        }

        /// <summary>
        /// Builds the option label for toggling automatic guard refills.
        /// </summary>
        /// <param name="autoRefillEnabled">Whether auto-refill is currently enabled.</param>
        /// <returns>A player-facing option label.</returns>
        internal static string BuildToggleAutoRefillOptionText(bool autoRefillEnabled)
        {
            return autoRefillEnabled ? "Turn auto-refill off" : "Turn auto-refill on";
        }

        /// <summary>
        /// Builds a guard party size adjustment label using the current and next values.
        /// </summary>
        /// <param name="currentValue">The current max size.</param>
        /// <param name="increase">Whether the action increases or decreases the value.</param>
        /// <returns>A player-facing option label.</returns>
        internal static string BuildGuardPartySizeOptionText(int currentValue, bool increase, bool usesAutomaticValue)
        {
            int normalizedCurrent = GarrisonSettings.ClampGuardPartyMaxSize(currentValue);
            int delta = increase ? GarrisonSettings.GuardPartyMaxSizeStep : -GarrisonSettings.GuardPartyMaxSizeStep;
            int nextValue = GarrisonSettings.ClampGuardPartyMaxSize(normalizedCurrent + delta);
            string action = increase ? "Increase" : "Decrease";
            string currentValueText = BuildGuardPartySizeValueText(normalizedCurrent, usesAutomaticValue);
            return $"{action} max guard size ({currentValueText} -> {nextValue})";
        }

        /// <summary>
        /// Builds the option label for reverting a fixed guard-party max size back to automatic sizing.
        /// </summary>
        /// <param name="automaticValue">The current automatic guard-party max size.</param>
        /// <returns>A player-facing option label.</returns>
        internal static string BuildResetGuardPartySizeOptionText(int automaticValue)
        {
            return $"Reset max guard size to {BuildGuardPartySizeValueText(automaticValue, usesAutomaticValue: true)}";
        }

        /// <summary>
        /// Builds a reserve threshold adjustment label using the current and next values.
        /// </summary>
        /// <param name="currentValue">The current effective reserve threshold.</param>
        /// <param name="increase">Whether the action increases or decreases the value.</param>
        /// <param name="usesAutomaticValue">Whether the threshold is currently automatic.</param>
        /// <param name="maximumValue">The settlement's current maximum garrison capacity.</param>
        /// <returns>A player-facing option label.</returns>
        internal static string BuildReserveThresholdOptionText(int currentValue, bool increase, bool usesAutomaticValue, int maximumValue)
        {
            int normalizedCurrent = GarrisonSettings.ClampRecruitmentThreshold(currentValue, maximumValue);
            int delta = increase ? GarrisonSettings.RecruitmentThresholdStep : -GarrisonSettings.RecruitmentThresholdStep;
            int nextValue = GarrisonSettings.ClampRecruitmentThreshold(normalizedCurrent + delta, maximumValue);
            string action = increase ? "Increase" : "Decrease";
            string currentValueText = BuildReserveThresholdValueText(normalizedCurrent, usesAutomaticValue);
            return $"{action} auto-recruit target ({currentValueText} -> {nextValue})";
        }

        /// <summary>
        /// Builds the option label for reverting a fixed reserve threshold back to the automatic settlement capacity.
        /// </summary>
        /// <param name="automaticValue">The current automatic reserve threshold.</param>
        /// <returns>A player-facing option label.</returns>
        internal static string BuildResetReserveThresholdOptionText(int automaticValue)
        {
            return $"Reset auto-recruit target to {BuildReserveThresholdValueText(automaticValue, usesAutomaticValue: true)}";
        }

        /// <summary>
        /// Builds the apply-now option label based on the current guard party state.
        /// </summary>
        /// <param name="guardPartyEnabled">Whether guard parties are currently enabled.</param>
        /// <param name="activeGuardPartySize">The current active guard party size.</param>
        /// <returns>A player-facing option label.</returns>
        internal static string BuildApplyGuardSettingsOptionText(bool guardPartyEnabled, int activeGuardPartySize)
        {
            if (!guardPartyEnabled)
            {
                return "Enable guard parties to create one";
            }

            return activeGuardPartySize > 0
                ? "Refresh guard party now"
                : "Create guard party now";
        }

        internal static string BuildGuardSettingsInquiryText(string settlementName, GarrisonSettings settings, int activeGuardPartySize, int defensiveTroopCount, int maximumGarrisonCapacity)
        {
            return BuildGuardSettingsMenuText(settlementName, settings, activeGuardPartySize, defensiveTroopCount, maximumGarrisonCapacity)
                + $"{Environment.NewLine}{Environment.NewLine}Select one action, then choose Apply.";
        }

        private static void OnGuardSettingsMenuInit(MenuCallbackArgs args)
        {
            args.IsEnabled = true;

            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                args.MenuTitle = new TextObject("Improved Garrisons");
                args.Text = new TextObject("Guard settings are only available while visiting one of your own towns or castles.");
                return;
            }

            int activeGuardPartySize = GuardPartyManager.GetActiveGuardPartySize(settlement);
            int defensiveTroopCount = GetTotalDefensiveTroopCount(settlement);
            int maximumGarrisonCapacity = GetMaximumGarrisonCapacity(settlement);

            args.MenuTitle = new TextObject(BuildGuardSettingsMenuTitle(settlement.Name?.ToString()));
            args.Text = new TextObject(
                BuildGuardSettingsMenuText(
                    settlement.Name?.ToString(),
                    settings,
                    activeGuardPartySize,
                    defensiveTroopCount,
                    maximumGarrisonCapacity));
        }

        private static bool CanOpenGuardSettingsMenu(MenuCallbackArgs args)
        {
            if (!TryGetManagedSettlement(args, out Settlement settlement))
            {
                return false;
            }

            args.IsEnabled = true;
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            args.Text = new TextObject(GuardSettingsEntryText);
            args.Tooltip = new TextObject($"Configure guard parties for {ResolveSettlementName(settlement.Name?.ToString())}.");
            return true;
        }

        private static void OpenGuardSettingsMenu(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return;
            }

            ShowGuardSettingsInquiry(settlement, settings);
        }

        private static bool CanToggleGuardParties(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return false;
            }

            args.IsEnabled = true;
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            args.Text = new TextObject(BuildToggleGuardPartyOptionText(settings.GuardPartyEnabled));
            args.Tooltip = new TextObject(settings.GuardPartyEnabled
                ? $"Disable guard parties for {ResolveSettlementName(settlement.Name?.ToString())} and return any active guard troops to the garrison."
                : $"Allow {ResolveSettlementName(settlement.Name?.ToString())} to create and maintain a guard party.");
            return true;
        }

        private static void ToggleGuardParties(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return;
            }

            settings.GuardPartyEnabled = !settings.GuardPartyEnabled;
            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    settings.GuardPartyEnabled
                        ? $"Improved Garrisons: Guard parties enabled for {settlement.Name}."
                        : $"Improved Garrisons: Guard parties disabled for {settlement.Name}.",
                    settings.GuardPartyEnabled ? Colors.Green : Colors.Yellow));
            RefreshGuardSettingsMenu(args);
        }

        private static bool CanToggleAutoRefill(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return false;
            }

            args.IsEnabled = true;
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            args.Text = new TextObject(BuildToggleAutoRefillOptionText(settings.GuardPartyAutoRefill));
            args.Tooltip = new TextObject(settings.GuardPartyAutoRefill
                ? $"Stop refilling {ResolveSettlementName(settlement.Name?.ToString())}'s guard party from the garrison."
                : $"Allow the guard party to refill from troops above the reserve threshold.");
            return true;
        }

        private static void ToggleAutoRefill(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return;
            }

            settings.GuardPartyAutoRefill = !settings.GuardPartyAutoRefill;
            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    settings.GuardPartyAutoRefill
                        ? $"Improved Garrisons: Auto-refill enabled for {settlement.Name}'s guard party."
                        : $"Improved Garrisons: Auto-refill disabled for {settlement.Name}'s guard party.",
                    Colors.Cyan));
            RefreshGuardSettingsMenu(args);
        }

        private static bool CanIncreaseGuardPartySize(MenuCallbackArgs args)
        {
            return ConfigureGuardPartySizeOption(args, increase: true);
        }

        private static void IncreaseGuardPartySize(MenuCallbackArgs args)
        {
            AdjustGuardPartySize(args, GarrisonSettings.GuardPartyMaxSizeStep);
        }

        private static bool CanDecreaseGuardPartySize(MenuCallbackArgs args)
        {
            return ConfigureGuardPartySizeOption(args, increase: false);
        }

        private static void DecreaseGuardPartySize(MenuCallbackArgs args)
        {
            AdjustGuardPartySize(args, -GarrisonSettings.GuardPartyMaxSizeStep);
        }

        private static bool CanResetGuardPartySize(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return false;
            }

            int automaticGuardPartyMaxSize = GarrisonSettings.CalculateAutomaticGuardPartyMaxSize(GetTotalDefensiveTroopCount(settlement));
            args.IsEnabled = !settings.UsesAutomaticGuardPartyMaxSize;
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            args.Text = new TextObject(BuildResetGuardPartySizeOptionText(automaticGuardPartyMaxSize));
            args.Tooltip = new TextObject(settings.UsesAutomaticGuardPartyMaxSize
                ? $"The guard-party max size already uses the automatic value of {automaticGuardPartyMaxSize}."
                : "Return the guard-party max size to the automatic value based on 25% of the current defensive troops.");
            return true;
        }

        private static void ResetGuardPartySize(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings) || settings.UsesAutomaticGuardPartyMaxSize)
            {
                return;
            }

            int defensiveTroopCount = GetTotalDefensiveTroopCount(settlement);
            settings.ResetGuardPartyMaxSize();
            int automaticValue = settings.ResolveGuardPartyMaxSize(defensiveTroopCount);

            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: {settlement.Name} guard-party max size reset to Auto ({automaticValue}).",
                    Colors.Yellow));
            RefreshGuardSettingsMenu(args);
        }

        private static bool CanIncreaseReserveThreshold(MenuCallbackArgs args)
        {
            return ConfigureReserveThresholdOption(args, increase: true);
        }

        private static void IncreaseReserveThreshold(MenuCallbackArgs args)
        {
            AdjustReserveThreshold(args, GarrisonSettings.RecruitmentThresholdStep);
        }

        private static bool CanDecreaseReserveThreshold(MenuCallbackArgs args)
        {
            return ConfigureReserveThresholdOption(args, increase: false);
        }

        private static void DecreaseReserveThreshold(MenuCallbackArgs args)
        {
            AdjustReserveThreshold(args, -GarrisonSettings.RecruitmentThresholdStep);
        }

        private static bool CanResetReserveThreshold(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return false;
            }

            int automaticReserveThreshold = GarrisonSettings.CalculateAutomaticRecruitmentThreshold(settlement);
            string protectedGarrisonText = BuildProtectedGarrisonValueText(automaticReserveThreshold);
            args.IsEnabled = !settings.UsesAutomaticRecruitmentThreshold;
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            args.Text = new TextObject(BuildResetReserveThresholdOptionText(automaticReserveThreshold));
            args.Tooltip = new TextObject(settings.UsesAutomaticRecruitmentThreshold
                ? $"Auto-recruitment already stops at {automaticReserveThreshold}, and guard refill keeps at least {protectedGarrisonText}."
                : "Return the auto-recruit target to the settlement's garrison capacity. Guard refill keeps at least half of that value in the garrison.");
            return true;
        }

        private static void ResetReserveThreshold(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings) || settings.UsesAutomaticRecruitmentThreshold)
            {
                return;
            }

            settings.ResetRecruitmentThreshold();
            int automaticValue = settings.ResolveRecruitmentThreshold(settlement);
            string protectedGarrisonText = BuildProtectedGarrisonValueText(automaticValue);

            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: {settlement.Name} auto-recruit target reset to Auto ({automaticValue}). Guard refill now keeps at least {protectedGarrisonText}.",
                    Colors.Yellow));
            RefreshGuardSettingsMenu(args);
        }

        private static bool CanApplyGuardSettingsNow(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return false;
            }

            int activeGuardPartySize = GuardPartyManager.GetActiveGuardPartySize(settlement);
            args.IsEnabled = settings.GuardPartyEnabled;
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            args.Text = new TextObject(BuildApplyGuardSettingsOptionText(settings.GuardPartyEnabled, activeGuardPartySize));
            args.Tooltip = new TextObject(settings.GuardPartyEnabled
                ? $"Apply the current guard party settings immediately for {ResolveSettlementName(settlement.Name?.ToString())}."
                : "Enable guard parties before creating or refreshing one.");
            return true;
        }

        private static void ApplyGuardSettingsNow(MenuCallbackArgs args)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings) || !settings.GuardPartyEnabled)
            {
                return;
            }

            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: Applied guard settings for {settlement.Name}.",
                    Colors.Green));
            RefreshGuardSettingsMenu(args);
        }

        private static bool CanGoBack(MenuCallbackArgs args)
        {
            if (ResolveCurrentSettlement(args) == null)
            {
                return false;
            }

            args.IsEnabled = true;
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            args.Text = new TextObject("Back");
            args.Tooltip = new TextObject("Return to the settlement menu.");
            return true;
        }

        private static void GoBack(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu(GetParentMenuId(ResolveCurrentSettlement(args)));
        }

        private static bool ConfigureGuardPartySizeOption(MenuCallbackArgs args, bool increase)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return false;
            }

            int currentGuardPartyMaxSize = ResolveGuardPartyMaxSize(settlement, settings);
            bool isEnabled = increase
                ? currentGuardPartyMaxSize < GarrisonSettings.MaxGuardPartyMaxSize
                : currentGuardPartyMaxSize > GarrisonSettings.MinGuardPartyMaxSize;

            args.IsEnabled = isEnabled;
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            args.Text = new TextObject(BuildGuardPartySizeOptionText(currentGuardPartyMaxSize, increase, settings.UsesAutomaticGuardPartyMaxSize));
            args.Tooltip = new TextObject(increase
                ? $"Raise the maximum number of troops assigned to the guard party. Maximum: {GarrisonSettings.MaxGuardPartyMaxSize}."
                : $"Lower the maximum number of troops assigned to the guard party. Minimum: {GarrisonSettings.MinGuardPartyMaxSize}.");
            return true;
        }

        private static bool ConfigureReserveThresholdOption(MenuCallbackArgs args, bool increase)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return false;
            }

            int maximumGarrisonCapacity = GetMaximumGarrisonCapacity(settlement);
            int currentReserveThreshold = settings.ResolveRecruitmentThreshold(maximumGarrisonCapacity);
            int maximumReserveThreshold = GarrisonSettings.GetMaximumRecruitmentThreshold(maximumGarrisonCapacity);
            bool isEnabled = increase
                ? currentReserveThreshold < maximumReserveThreshold
                : currentReserveThreshold > GarrisonSettings.MinRecruitmentThreshold;

            args.IsEnabled = isEnabled;
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            args.Text = new TextObject(BuildReserveThresholdOptionText(currentReserveThreshold, increase, settings.UsesAutomaticRecruitmentThreshold, maximumGarrisonCapacity));
            args.Tooltip = new TextObject(increase
                ? $"Raise the garrison size where auto-recruitment stops. Guard refill only takes troops above half this value. Maximum: {maximumReserveThreshold}."
                : $"Lower the garrison size where auto-recruitment stops. Guard refill only takes troops above half this value. Minimum: {GarrisonSettings.MinRecruitmentThreshold}.");
            return true;
        }

        private static void AdjustGuardPartySize(MenuCallbackArgs args, int delta)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return;
            }

            int defensiveTroopCount = GetTotalDefensiveTroopCount(settlement);
            int previousValue = settings.ResolveGuardPartyMaxSize(defensiveTroopCount);
            int newValue = settings.AdjustGuardPartyMaxSize(delta, defensiveTroopCount);
            if (newValue == previousValue)
            {
                return;
            }

            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: {settlement.Name} guard-party max size set to {newValue}.",
                    Colors.Yellow));
            RefreshGuardSettingsMenu(args);
        }

        private static void AdjustReserveThreshold(MenuCallbackArgs args, int delta)
        {
            if (!TryGetMenuContext(args, out Settlement settlement, out GarrisonSettings settings))
            {
                return;
            }

            int maximumGarrisonCapacity = GetMaximumGarrisonCapacity(settlement);
            int previousValue = settings.ResolveRecruitmentThreshold(maximumGarrisonCapacity);
            int newValue = settings.AdjustRecruitmentThreshold(delta, maximumGarrisonCapacity);
            if (newValue == previousValue)
            {
                return;
            }

            string protectedGarrisonText = BuildProtectedGarrisonValueText(newValue);

            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: {settlement.Name} auto-recruit target set to {newValue}. Guard refill now keeps at least {protectedGarrisonText}.",
                    Colors.Yellow));
            RefreshGuardSettingsMenu(args);
        }

        private static void ApplyGuardSettings(Settlement settlement, GarrisonSettings settings)
        {
            settings.Normalize();
            GuardPartyManager.MaintainGuardParty(settlement, settings);
        }

        private static void ShowGuardSettingsInquiry(Settlement settlement, GarrisonSettings settings = null)
        {
            if (settlement == null)
            {
                return;
            }

            ImprovedGarrisonsCampaignBehavior behavior = Campaign.Current?.GetCampaignBehavior<ImprovedGarrisonsCampaignBehavior>();
            if (behavior == null)
            {
                return;
            }

            GarrisonSettings effectiveSettings = settings ?? behavior.GetOrCreateSettings(settlement);
            effectiveSettings.Normalize();

            int activeGuardPartySize = GuardPartyManager.GetActiveGuardPartySize(settlement);
            int defensiveTroopCount = GetTotalDefensiveTroopCount(settlement);
            int maximumGarrisonCapacity = GetMaximumGarrisonCapacity(settlement);
            var inquiry = new MultiSelectionInquiryData(
                BuildGuardSettingsMenuTitle(settlement.Name?.ToString()),
                BuildGuardSettingsInquiryText(settlement.Name?.ToString(), effectiveSettings, activeGuardPartySize, defensiveTroopCount, maximumGarrisonCapacity),
                BuildGuardSettingsInquiryOptions(settlement, effectiveSettings, activeGuardPartySize, defensiveTroopCount, maximumGarrisonCapacity),
                true,
                1,
                1,
                "Apply",
                "Close",
                selectedActions => OnGuardSettingsInquiryConfirmed(settlement, selectedActions),
                _ => { },
                string.Empty,
                false);

            MBInformationManager.ShowMultiSelectionInquiry(inquiry, true, false);
        }

        private static List<InquiryElement> BuildGuardSettingsInquiryOptions(Settlement settlement, GarrisonSettings settings, int activeGuardPartySize, int defensiveTroopCount, int maximumGarrisonCapacity)
        {
            int currentGuardPartyMaxSize = settings.ResolveGuardPartyMaxSize(defensiveTroopCount);
            int automaticGuardPartyMaxSize = GarrisonSettings.CalculateAutomaticGuardPartyMaxSize(defensiveTroopCount);
            int currentReserveThreshold = settings.ResolveRecruitmentThreshold(maximumGarrisonCapacity);
            int automaticReserveThreshold = GarrisonSettings.CalculateAutomaticRecruitmentThreshold(maximumGarrisonCapacity);
            int maximumReserveThreshold = GarrisonSettings.GetMaximumRecruitmentThreshold(maximumGarrisonCapacity);

            return new List<InquiryElement>
            {
                CreateGuardSettingsInquiryElement(
                    GuardSettingsInquiryAction.ToggleGuardParties,
                    BuildToggleGuardPartyOptionText(settings.GuardPartyEnabled),
                    true,
                    settings.GuardPartyEnabled
                        ? $"Disable guard parties for {ResolveSettlementName(settlement.Name?.ToString())} and return any active guard troops to the garrison."
                        : $"Allow {ResolveSettlementName(settlement.Name?.ToString())} to create and maintain a guard party."),
                CreateGuardSettingsInquiryElement(
                    GuardSettingsInquiryAction.ToggleAutoRefill,
                    BuildToggleAutoRefillOptionText(settings.GuardPartyAutoRefill),
                    true,
                    settings.GuardPartyAutoRefill
                        ? $"Stop refilling {ResolveSettlementName(settlement.Name?.ToString())}'s guard party from the garrison."
                        : "Allow the guard party to refill from troops above the protected garrison reserve."),
                CreateGuardSettingsInquiryElement(
                    GuardSettingsInquiryAction.IncreaseGuardPartySize,
                    BuildGuardPartySizeOptionText(currentGuardPartyMaxSize, increase: true, settings.UsesAutomaticGuardPartyMaxSize),
                    currentGuardPartyMaxSize < GarrisonSettings.MaxGuardPartyMaxSize,
                    currentGuardPartyMaxSize < GarrisonSettings.MaxGuardPartyMaxSize
                        ? $"Raise the maximum number of troops assigned to the guard party. Maximum: {GarrisonSettings.MaxGuardPartyMaxSize}."
                        : $"The guard-party max size is already at the maximum of {GarrisonSettings.MaxGuardPartyMaxSize}."),
                CreateGuardSettingsInquiryElement(
                    GuardSettingsInquiryAction.DecreaseGuardPartySize,
                    BuildGuardPartySizeOptionText(currentGuardPartyMaxSize, increase: false, settings.UsesAutomaticGuardPartyMaxSize),
                    currentGuardPartyMaxSize > GarrisonSettings.MinGuardPartyMaxSize,
                    currentGuardPartyMaxSize > GarrisonSettings.MinGuardPartyMaxSize
                        ? $"Lower the maximum number of troops assigned to the guard party. Minimum: {GarrisonSettings.MinGuardPartyMaxSize}."
                        : $"The guard-party max size is already at the minimum of {GarrisonSettings.MinGuardPartyMaxSize}."),
                CreateGuardSettingsInquiryElement(
                    GuardSettingsInquiryAction.ResetGuardPartySize,
                    BuildResetGuardPartySizeOptionText(automaticGuardPartyMaxSize),
                    !settings.UsesAutomaticGuardPartyMaxSize,
                    settings.UsesAutomaticGuardPartyMaxSize
                        ? $"The guard-party max size already uses the automatic value of {automaticGuardPartyMaxSize}."
                        : "Return the guard-party max size to the automatic value based on 25% of the current defensive troops."),
                CreateGuardSettingsInquiryElement(
                    GuardSettingsInquiryAction.IncreaseReserveThreshold,
                    BuildReserveThresholdOptionText(currentReserveThreshold, increase: true, settings.UsesAutomaticRecruitmentThreshold, maximumGarrisonCapacity),
                    currentReserveThreshold < maximumReserveThreshold,
                    currentReserveThreshold < maximumReserveThreshold
                        ? $"Raise the garrison size where auto-recruitment stops. Guard refill only takes troops above half this value. Maximum: {maximumReserveThreshold}."
                        : $"The reserve threshold is already at the maximum of {maximumReserveThreshold}."),
                CreateGuardSettingsInquiryElement(
                    GuardSettingsInquiryAction.DecreaseReserveThreshold,
                    BuildReserveThresholdOptionText(currentReserveThreshold, increase: false, settings.UsesAutomaticRecruitmentThreshold, maximumGarrisonCapacity),
                    currentReserveThreshold > GarrisonSettings.MinRecruitmentThreshold,
                    currentReserveThreshold > GarrisonSettings.MinRecruitmentThreshold
                        ? $"Lower the garrison size where auto-recruitment stops. Guard refill only takes troops above half this value. Minimum: {GarrisonSettings.MinRecruitmentThreshold}."
                        : $"The reserve threshold is already at the minimum of {GarrisonSettings.MinRecruitmentThreshold}."),
                CreateGuardSettingsInquiryElement(
                    GuardSettingsInquiryAction.ResetReserveThreshold,
                    BuildResetReserveThresholdOptionText(automaticReserveThreshold),
                    !settings.UsesAutomaticRecruitmentThreshold,
                    settings.UsesAutomaticRecruitmentThreshold
                        ? $"Auto-recruitment already stops at {automaticReserveThreshold}, and guard refill keeps at least {BuildProtectedGarrisonValueText(automaticReserveThreshold)}."
                        : "Return the auto-recruit target to the settlement's garrison capacity. Guard refill keeps at least half of that value in the garrison."),
                CreateGuardSettingsInquiryElement(
                    GuardSettingsInquiryAction.ApplyNow,
                    BuildApplyGuardSettingsOptionText(settings.GuardPartyEnabled, activeGuardPartySize),
                    settings.GuardPartyEnabled,
                    settings.GuardPartyEnabled
                        ? $"Apply the current guard party settings immediately for {ResolveSettlementName(settlement.Name?.ToString())}."
                        : "Enable guard parties before creating or refreshing one.")
            };
        }

        private static InquiryElement CreateGuardSettingsInquiryElement(GuardSettingsInquiryAction action, string title, bool isEnabled, string hint)
        {
            return new InquiryElement(action, title, null, isEnabled, hint);
        }

        private static void OnGuardSettingsInquiryConfirmed(Settlement settlement, List<InquiryElement> selectedActions)
        {
            if (settlement == null || selectedActions == null || selectedActions.Count == 0)
            {
                return;
            }

            if (!(selectedActions[0].Identifier is GuardSettingsInquiryAction action))
            {
                return;
            }

            ApplyGuardSettingsInquiryAction(settlement, action);
            ShowGuardSettingsInquiry(settlement);
        }

        private static void ApplyGuardSettingsInquiryAction(Settlement settlement, GuardSettingsInquiryAction action)
        {
            ImprovedGarrisonsCampaignBehavior behavior = Campaign.Current?.GetCampaignBehavior<ImprovedGarrisonsCampaignBehavior>();
            if (settlement == null || behavior == null)
            {
                return;
            }

            GarrisonSettings settings = behavior.GetOrCreateSettings(settlement);

            switch (action)
            {
                case GuardSettingsInquiryAction.ToggleGuardParties:
                    ToggleGuardPartiesFromInquiry(settlement, settings);
                    break;

                case GuardSettingsInquiryAction.ToggleAutoRefill:
                    ToggleAutoRefillFromInquiry(settlement, settings);
                    break;

                case GuardSettingsInquiryAction.IncreaseGuardPartySize:
                case GuardSettingsInquiryAction.DecreaseGuardPartySize:
                    AdjustGuardPartySizeFromInquiry(settlement, settings, action == GuardSettingsInquiryAction.IncreaseGuardPartySize);
                    break;

                case GuardSettingsInquiryAction.ResetGuardPartySize:
                    ResetGuardPartySizeFromInquiry(settlement, settings);
                    break;

                case GuardSettingsInquiryAction.IncreaseReserveThreshold:
                case GuardSettingsInquiryAction.DecreaseReserveThreshold:
                    AdjustReserveThresholdFromInquiry(settlement, settings, action == GuardSettingsInquiryAction.IncreaseReserveThreshold);
                    break;

                case GuardSettingsInquiryAction.ResetReserveThreshold:
                    ResetReserveThresholdFromInquiry(settlement, settings);
                    break;

                case GuardSettingsInquiryAction.ApplyNow:
                    ApplyNowFromInquiry(settlement, settings);
                    break;
            }
        }

        private static void ToggleGuardPartiesFromInquiry(Settlement settlement, GarrisonSettings settings)
        {
            settings.GuardPartyEnabled = !settings.GuardPartyEnabled;
            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    settings.GuardPartyEnabled
                        ? $"Improved Garrisons: Guard parties enabled for {settlement.Name}."
                        : $"Improved Garrisons: Guard parties disabled for {settlement.Name}.",
                    settings.GuardPartyEnabled ? Colors.Green : Colors.Yellow));
        }

        private static void ToggleAutoRefillFromInquiry(Settlement settlement, GarrisonSettings settings)
        {
            settings.GuardPartyAutoRefill = !settings.GuardPartyAutoRefill;
            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    settings.GuardPartyAutoRefill
                        ? $"Improved Garrisons: Auto-refill enabled for {settlement.Name}'s guard party."
                        : $"Improved Garrisons: Auto-refill disabled for {settlement.Name}'s guard party.",
                    Colors.Cyan));
        }

        private static void AdjustGuardPartySizeFromInquiry(Settlement settlement, GarrisonSettings settings, bool increase)
        {
            int delta = increase
                ? GarrisonSettings.GuardPartyMaxSizeStep
                : -GarrisonSettings.GuardPartyMaxSizeStep;
            int defensiveTroopCount = GetTotalDefensiveTroopCount(settlement);
            int previousSize = settings.ResolveGuardPartyMaxSize(defensiveTroopCount);
            int newSize = settings.AdjustGuardPartyMaxSize(delta, defensiveTroopCount);
            if (newSize == previousSize)
            {
                return;
            }

            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: {settlement.Name} guard-party max size set to {newSize}.",
                    Colors.Yellow));
        }

        private static void ResetGuardPartySizeFromInquiry(Settlement settlement, GarrisonSettings settings)
        {
            if (settings.UsesAutomaticGuardPartyMaxSize)
            {
                return;
            }

            int defensiveTroopCount = GetTotalDefensiveTroopCount(settlement);
            settings.ResetGuardPartyMaxSize();
            int automaticValue = settings.ResolveGuardPartyMaxSize(defensiveTroopCount);

            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: {settlement.Name} guard-party max size reset to Auto ({automaticValue}).",
                    Colors.Yellow));
        }

        private static void AdjustReserveThresholdFromInquiry(Settlement settlement, GarrisonSettings settings, bool increase)
        {
            int delta = increase
                ? GarrisonSettings.RecruitmentThresholdStep
                : -GarrisonSettings.RecruitmentThresholdStep;
            int maximumGarrisonCapacity = GetMaximumGarrisonCapacity(settlement);
            int previousThreshold = settings.ResolveRecruitmentThreshold(maximumGarrisonCapacity);
            int newThreshold = settings.AdjustRecruitmentThreshold(delta, maximumGarrisonCapacity);
            if (newThreshold == previousThreshold)
            {
                return;
            }

            string protectedGarrisonText = BuildProtectedGarrisonValueText(newThreshold);

            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: {settlement.Name} auto-recruit target set to {newThreshold}. Guard refill now keeps at least {protectedGarrisonText}.",
                    Colors.Yellow));
        }

        private static void ResetReserveThresholdFromInquiry(Settlement settlement, GarrisonSettings settings)
        {
            if (settings.UsesAutomaticRecruitmentThreshold)
            {
                return;
            }

            settings.ResetRecruitmentThreshold();
            int automaticValue = settings.ResolveRecruitmentThreshold(settlement);
            string protectedGarrisonText = BuildProtectedGarrisonValueText(automaticValue);

            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: {settlement.Name} auto-recruit target reset to Auto ({automaticValue}). Guard refill now keeps at least {protectedGarrisonText}.",
                    Colors.Yellow));
        }

        private static void ApplyNowFromInquiry(Settlement settlement, GarrisonSettings settings)
        {
            if (!settings.GuardPartyEnabled)
            {
                return;
            }

            ApplyGuardSettings(settlement, settings);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"Improved Garrisons: Applied guard settings for {settlement.Name}.",
                    Colors.Green));
        }

        private static void RefreshGuardSettingsMenu(MenuCallbackArgs args)
        {
            if (args?.MenuContext != null)
            {
                args.MenuContext.Refresh();
                return;
            }

            GameMenu.SwitchToMenu(GuardSettingsMenuId);
        }

        private static bool TryGetManagedSettlement(MenuCallbackArgs args, out Settlement settlement)
        {
            settlement = ResolveManagedSettlement(args);
            return settlement != null;
        }

        private static int ResolveGuardPartyMaxSize(Settlement settlement, GarrisonSettings settings)
        {
            return (settings ?? new GarrisonSettings()).ResolveGuardPartyMaxSize(GetTotalDefensiveTroopCount(settlement));
        }

        private static int GetMaximumGarrisonCapacity(Settlement settlement)
        {
            return GarrisonSettings.GetMaximumGarrisonCapacity(settlement);
        }

        private static int GetTotalDefensiveTroopCount(Settlement settlement)
        {
            return GetCurrentGarrisonTroopCount(settlement) + GuardPartyManager.GetActiveGuardPartySize(settlement);
        }

        private static int GetCurrentGarrisonTroopCount(Settlement settlement)
        {
            return settlement?.Town?.GarrisonParty?.MemberRoster?.TotalManCount ?? 0;
        }

        private static bool TryGetMenuContext(MenuCallbackArgs args, out Settlement settlement, out GarrisonSettings settings)
        {
            settlement = ResolveManagedSettlement(args);
            settings = null;
            if (settlement == null)
            {
                return false;
            }

            var behavior = Campaign.Current?.GetCampaignBehavior<ImprovedGarrisonsCampaignBehavior>();
            if (behavior == null)
            {
                return false;
            }

            settings = behavior.GetOrCreateSettings(settlement);
            return true;
        }

        private static Settlement ResolveManagedSettlement(MenuCallbackArgs args)
        {
            Settlement settlement = ResolveCurrentSettlement(args);
            if (settlement == null || (!settlement.IsTown && !settlement.IsCastle))
            {
                return null;
            }

            return ImprovedGarrisonsCampaignBehavior.IsPlayerFief(settlement)
                ? settlement
                : null;
        }

        private static Settlement ResolveManagedSettlement(GameMenu gameMenu)
        {
            Settlement settlement = null;
            if (gameMenu?.RelatedObject is Settlement relatedSettlement)
            {
                settlement = relatedSettlement;
            }

            settlement ??= Settlement.CurrentSettlement ?? Hero.MainHero?.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
            if (settlement == null || (!settlement.IsTown && !settlement.IsCastle))
            {
                return null;
            }

            return ImprovedGarrisonsCampaignBehavior.IsPlayerFief(settlement)
                ? settlement
                : null;
        }

        private static Settlement ResolveCurrentSettlement(MenuCallbackArgs args)
        {
            if (args?.MenuContext?.GameMenu?.RelatedObject is Settlement relatedSettlement)
            {
                return relatedSettlement;
            }

            return Settlement.CurrentSettlement ?? Hero.MainHero?.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
        }

        private static string GetParentMenuId(Settlement settlement)
        {
            return settlement != null && settlement.IsCastle
                ? CastleMenuId
                : TownMenuId;
        }

        private static string ResolveSettlementName(string settlementName)
        {
            return string.IsNullOrWhiteSpace(settlementName)
                ? "your settlement"
                : settlementName;
        }
    }
}