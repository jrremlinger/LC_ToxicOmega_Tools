using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerB_Patch : MonoBehaviour
    {
        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPostfix]
        static void DeadPlayerEnableHUD(PlayerControllerB __instance)
        {
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
            if (!Plugin.CheckPlayerIsHost(__instance))
                return true;

            return !Plugin.enableGod;
        }

        [HarmonyPatch(nameof(PlayerControllerB.Update))]
        [HarmonyPostfix]
        static void Update(PlayerControllerB __instance)
        {
            if (!CustomGUI.nearbyVisible && !CustomGUI.fullListVisible)
                return;

            Vector3 localPosition = (__instance.isPlayerDead && __instance.spectatedPlayerScript != null) ? __instance.spectatedPlayerScript.transform.position : __instance.transform.position;

            CustomGUI.posLabelText = $"Time: {(RoundManager.Instance.timeScript.hour + 6 > 12 ? RoundManager.Instance.timeScript.hour - 6 : RoundManager.Instance.timeScript.hour + 6)}{(RoundManager.Instance.timeScript.hour + 6 < 12 ? "am" : "pm")}\n";
            CustomGUI.posLabelText += $"GodMode: {(Plugin.enableGod ? "Enabled" : "Disabled")}\n";
            CustomGUI.posLabelText += $"X: {Math.Round(localPosition.x, 1)}\nY: {Math.Round(localPosition.y, 1)}\nZ: {Math.Round(localPosition.z, 1)}";
        }

        public static IEnumerator UpdateGUI()
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            for (; ; )
            {
                if (!CustomGUI.nearbyVisible && !CustomGUI.fullListVisible)
                    yield return null;

                CustomGUI.itemListText = "";
                CustomGUI.terminalObjListText = "";
                CustomGUI.enemyListText = "";
                Vector3 position = (localPlayer.isPlayerDead && localPlayer.spectatedPlayerScript != null) ? localPlayer.spectatedPlayerScript.transform.position : localPlayer.transform.position;

                foreach (GrabbableObject obj in FindObjectsOfType<GrabbableObject>())
                {
                    if (Vector3.Distance(obj.transform.position, position) < 25.0f || CustomGUI.fullListVisible)
                        CustomGUI.itemListText += $"{obj.itemProperties.itemName} ({obj.NetworkObjectId}){(obj.scrapValue > 0 ? $" - ${obj.scrapValue}" : "")}\n";
                }

                foreach (TerminalAccessibleObject terminalObj in FindObjectsOfType<TerminalAccessibleObject>())
                {
                    string objType = "";
                    bool isActive = true;
                    if (Vector3.Distance(terminalObj.transform.position, position) < 10.0f || CustomGUI.fullListVisible)
                    {
                        if (terminalObj.isBigDoor)
                        {
                            objType = "Door";
                        }
                        else if (terminalObj.GetComponentInChildren<Landmine>())
                        {
                            if (terminalObj.GetComponentInChildren<Landmine>().hasExploded)
                                continue;

                            objType = $"Landmine";
                            isActive = terminalObj.GetComponent<Landmine>().mineActivated;
                        }
                        else if (terminalObj.GetComponentInChildren<Turret>())
                        {
                            objType = $"Turret";
                            isActive = terminalObj.GetComponent<Turret>().turretActive;
                        }
                        else if (terminalObj.transform.parent.gameObject.GetComponentInChildren<SpikeRoofTrap>())
                        {
                            objType = $"Spikes";
                            isActive = terminalObj.transform.parent.gameObject.GetComponentInChildren<SpikeRoofTrap>().trapActive;
                        }
                        else
                        {
                            objType += "Unknown";
                        }

                        CustomGUI.terminalObjListText += $"{(!isActive || (terminalObj.isBigDoor && terminalObj.isDoorOpen) ? $"<color={(terminalObj.isBigDoor && terminalObj.isDoorOpen ? "lime" : "red")}>" : "")}{terminalObj.objectCode.ToUpper()}{(!isActive || (terminalObj.isBigDoor && terminalObj.isDoorOpen) ? "</color>" : "")} - {objType}\n";
                    }
                }

                foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
                {
                    if ((Vector3.Distance(player.isPlayerDead ? player.deadBody.transform.position : player.transform.position, position) < 25.0f || CustomGUI.fullListVisible) && (player.isPlayerControlled || player.isPlayerDead))
                        CustomGUI.enemyListText += $"{(player.isPlayerDead ? "<color=red>" : "")}{player.playerUsername}{(player.isPlayerDead ? "</color>" : "")} (#{player.playerClientId}{(Plugin.CheckPlayerIsHost(player) ? " - HOST" : "")})\n";
                }

                foreach (EnemyAI enemy in FindObjectsOfType<EnemyAI>())
                {
                    if (Vector3.Distance(enemy.transform.position, position) < 25.0f || CustomGUI.fullListVisible)
                        CustomGUI.enemyListText += $"{(enemy.isEnemyDead ? "<color=red>" : "")}{enemy.enemyType.enemyName}{(enemy.isEnemyDead ? "</color>" : "")} ({enemy.NetworkObjectId})\n";
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
