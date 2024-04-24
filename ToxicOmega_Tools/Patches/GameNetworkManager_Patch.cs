using HarmonyLib;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManager_Patch : MonoBehaviour
    {
        [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        private static void Disconnect()    // Clear game-specific variables when disconnecting
        {
            Plugin.enableGod = false;
            Plugin.waypoints.Clear();
            CustomGUI.nearbyVisible = false;
            CustomGUI.fullListVisible = false;
        }
    }
}
