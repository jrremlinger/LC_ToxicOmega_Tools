using AsmResolver.DotNet.Signatures.Types;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
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
            Vector3 localPos = (__instance.isPlayerDead && __instance.spectatedPlayerScript != null) ? __instance.spectatedPlayerScript.transform.position : __instance.transform.position;
            TOTGUI.posLabelText = $"X: {Math.Round(localPos.x, 1)}\nY: {Math.Round(localPos.y, 1)}\nZ: {Math.Round(localPos.z, 1)}\n";
            TOTGUI.itemListText = "";
            TOTGUI.terminalObjListText = "";
            TOTGUI.enemyListText = "";

            foreach (GrabbableObject obj in UnityEngine.Object.FindObjectsOfType<GrabbableObject>())
            {
                if (Vector3.Distance(obj.transform.position, localPos) < 25.0f)
                {
                    TOTGUI.itemListText += $"{obj.itemProperties.itemName} ({obj.NetworkObjectId}){(obj.scrapValue > 0 ? $" - ${obj.scrapValue}" : "")}\n";
                }
            }

            foreach (TerminalAccessibleObject terminalObj in UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>())
            {
                string objType = "";
                if (Vector3.Distance(terminalObj.transform.position, localPos) < 10.0f)
                {
                    if (terminalObj.isBigDoor)
                        objType = "Door";
                    else if (terminalObj.GetComponentInChildren<Turret>())
                        objType = "Turret";
                    else if (terminalObj.GetComponentInChildren<Landmine>())
                    {
                        if (terminalObj.GetComponentInChildren<Landmine>().hasExploded)
                            continue;
                        objType = "Landmine";
                    }
                    else if (terminalObj.transform.parent.gameObject.GetComponentInChildren<SpikeRoofTrap>())
                        objType = "Spikes";
                    else
                        objType += "Unknown";

                    TOTGUI.terminalObjListText += $"{(terminalObj.inCooldown || (terminalObj.isBigDoor && terminalObj.isDoorOpen) ? $"<color={(terminalObj.isBigDoor && terminalObj.isDoorOpen ? "lime" : "red")}>" : "")}{terminalObj.objectCode.ToUpper()}{(terminalObj.inCooldown || (terminalObj.isBigDoor && terminalObj.isDoorOpen) ? "</color>" : "")} - {objType}\n";
                }
            }

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (Vector3.Distance(player.transform.position, localPos) < 25.0f && player.isPlayerControlled && !player.isPlayerDead)
                {
                    TOTGUI.enemyListText += $"{player.playerUsername} (#{player.playerClientId}{(Plugin.CheckPlayerIsHost(player) ? " - HOST" : "")})\n";
                }
            }

            foreach (EnemyAI enemy in UnityEngine.Object.FindObjectsOfType<EnemyAI>())
            {
                if (Vector3.Distance(enemy.transform.position, localPos) < 25.0f)
                {
                    TOTGUI.enemyListText += $"{(enemy.isEnemyDead ? "<color=red>" : "")}{enemy.enemyType.enemyName}{(enemy.isEnemyDead ? "</color>" : "")} ({enemy.NetworkObjectId})\n";
                }
            }
        }
    }
}
