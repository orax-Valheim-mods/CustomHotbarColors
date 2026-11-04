using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CustomHotbarColors
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class CustomHotbarColors : BaseUnityPlugin
    {
        public const string PluginGUID = "orax.CustomHotbarColors";
        public const string PluginName = "CustomHotbarColors";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = base.Logger;
            Log.LogInfo("CustomHotbarColors has landed");

            HotbarConfig.Bind(Config);

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            // A script reload takes the styler away from the bars that are already on screen
            // without switching them off, so they are picked up again right here.
            foreach (var bar in FindObjectsByType<HotkeyBar>(FindObjectsSortMode.None))
            {
                HotbarStyler.Ensure(bar);
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();

            foreach (var styler in FindObjectsByType<HotbarStyler>(FindObjectsSortMode.None))
            {
                Destroy(styler);
            }
        }
    }

    /// <summary>
    /// Starts the restyling timer when the game switches a hotbar on. Nothing else is hooked:
    /// the cells are only ever touched from that timer, four times a second at most.
    /// </summary>
    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.OnEnable))]
    internal static class HotkeyBarOnEnablePatch
    {
        private static void Postfix(HotkeyBar __instance)
        {
            HotbarStyler.Ensure(__instance);
        }
    }
}
