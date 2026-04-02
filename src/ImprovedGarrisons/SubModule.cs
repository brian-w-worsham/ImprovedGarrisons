using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ImprovedGarrisons
{
    /// <summary>
    /// Entry point for the ImprovedGarrisons mod. Registers Harmony patches
    /// and adds the garrison management campaign behavior.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                _harmony = new Harmony("com.improvedgarrisons.bannerlord");
                _harmony.PatchAll();
                InformationManager.DisplayMessage(
                    new InformationMessage("Improved Garrisons: Loaded successfully.", Colors.Green));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"Improved Garrisons load error: {ex.Message}", Colors.Red));
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (game.GameType is Campaign)
            {
                var campaignStarter = (CampaignGameStarter)gameStarterObject;
                campaignStarter.AddBehavior(new ImprovedGarrisonsCampaignBehavior());
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            _harmony?.UnpatchAll("com.improvedgarrisons.bannerlord");
        }
    }
}
