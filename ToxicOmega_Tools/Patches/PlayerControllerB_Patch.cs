using AsmResolver.DotNet.Signatures.Types;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

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

        [HarmonyPatch(nameof(PlayerControllerB.Update))]
        [HarmonyPostfix]
        static void Update(PlayerControllerB __instance)
        {
            GrabbableObject[] grabbableObjectList = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            EnemyAI[] enemyList = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            Vector3 localPos = (__instance.isPlayerDead && __instance.spectatedPlayerScript != null) ? __instance.spectatedPlayerScript.transform.position : __instance.transform.position;
            TOTGUI.posLabelText = $"X: {Math.Round(localPos.x, 1)}\nY: {Math.Round(localPos.y, 1)}\nZ: {Math.Round(localPos.z, 1)}\n";
            TOTGUI.itemListText = "";
            TOTGUI.enemyListText = "";

            foreach (GrabbableObject obj in grabbableObjectList)
            {
                if (Vector3.Distance(obj.transform.position, localPos) < 25.0f)
                {
                    TOTGUI.itemListText += $"{obj.itemProperties.itemName} ({obj.NetworkObjectId})\n";
                }
            }

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (Vector3.Distance(player.transform.position, localPos) < 25.0f && player.isPlayerControlled && !player.isPlayerDead)
                {
                    TOTGUI.enemyListText += $"{player.playerUsername} (#{player.playerClientId}{(Plugin.CheckPlayerIsHost(player) ? " - HOST" : "")})\n";
                }
            }

            foreach (EnemyAI enemy in enemyList)
            {
                if (Vector3.Distance(enemy.transform.position, localPos) < 25.0f)
                {
                    TOTGUI.enemyListText += $"{enemy.enemyType.enemyName} ({enemy.NetworkObjectId})\n";
                }
            }
        }
    }
}
