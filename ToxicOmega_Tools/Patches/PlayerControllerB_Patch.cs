using GameNetcodeStuff;
using HarmonyLib;
using LC_API.GameInterfaceAPI.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerB_Patch
    {
        [HarmonyPatch("KillPlayer")]
        [HarmonyPostfix]
        private static void DeadPlayerEnableHUD(PlayerControllerB __instance)   // Allows host to see UI while dead so they can still use commands
        {
            if (Player.HostPlayer.ClientId == __instance.playerClientId)
            {
                HUDManager HUD = HUDManager.Instance;
                HUD.HideHUD(false);
                HUD.ToggleHUD(true);
            }
        }
    }
}
