﻿using HarmonyLib;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManager_Patch
    {
        [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        private static void Disconnect()    // Clear game-specific variables when disconnecting
        {
            Plugin.enableGod = false;
            Plugin.waypoints.Clear();
            GUI.visible = false;
            GUI.isFullList = false;
        }
    }
}
