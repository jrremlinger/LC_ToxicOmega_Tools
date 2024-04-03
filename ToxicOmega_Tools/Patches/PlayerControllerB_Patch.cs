using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerB_Patch
    {
        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPostfix]
        private static void DeadPlayerEnableHUD(PlayerControllerB __instance)
        {
            // Allows host to see UI while dead so they can still use commands
            //if (Player.HostPlayer.ClientId == __instance.playerClientId)
            if (Plugin.CheckPlayerIsHost(__instance))
            {
                HUDManager HUD = HUDManager.Instance;
                HUD.HideHUD(false);
                HUD.ToggleHUD(true);
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.AllowPlayerDeath))]
        [HarmonyPrefix]
        static bool OverrideDeath(PlayerControllerB __instance)
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (!Plugin.CheckPlayerIsHost(localPlayer))
                return true;
            return !Plugin.Instance.enableGod;
        }

    }
}
