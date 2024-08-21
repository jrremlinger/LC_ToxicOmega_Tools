using GameNetcodeStuff;
using System.Collections;
using UnityEngine;

namespace ToxicOmega_Tools
{
    internal class CustomGUI : MonoBehaviour
    {
        internal static bool nearbyVisible;
        internal static bool fullListVisible;
        internal static string posLabelText;
        internal static string itemListText;
        internal static string terminalObjListText;
        internal static string enemyListText;

        void OnGUI()
        {
            if (!nearbyVisible && !fullListVisible)
                return;

            GUI.Label(new Rect(Screen.width / 4, Screen.height / 8, Screen.width / 2, Screen.height * 0.75f), itemListText);
            GUI.Label(new Rect(Screen.width / 2, Screen.height / 8, Screen.width / 2, Screen.height * 0.75f), terminalObjListText);
            GUI.Label(new Rect(Screen.width * 0.75f, Screen.height / 8, Screen.width / 2, Screen.height * 0.75f), enemyListText);
            GUI.Label(new Rect((Screen.width / 2) - 75, Screen.height * 0.75f, 150, 150f), posLabelText);
        }

        public static IEnumerator UpdateGUI()
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            for (; ; )
            {
                if (!nearbyVisible && !fullListVisible)
                    yield return null;
                itemListText = "";
                terminalObjListText = "";
                enemyListText = "";
                Vector3 position = (localPlayer.isPlayerDead && localPlayer.spectatedPlayerScript != null) ? localPlayer.spectatedPlayerScript.transform.position : localPlayer.transform.position;
                foreach (GrabbableObject obj in FindObjectsOfType<GrabbableObject>())
                {
                    if (Vector3.Distance(obj.transform.position, position) < 25.0f || fullListVisible)
                        itemListText += $"{obj.itemProperties.itemName} ({obj.NetworkObjectId}){(obj.scrapValue > 0 ? $" - ${obj.scrapValue}" : "")}\n";
                }
                foreach (TerminalAccessibleObject terminalObj in FindObjectsOfType<TerminalAccessibleObject>())
                {
                    string objType = "";
                    bool isActive = true;
                    if (Vector3.Distance(terminalObj.transform.position, position) < 10.0f || fullListVisible)
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

                        terminalObjListText += $"{(!isActive || (terminalObj.isBigDoor && terminalObj.isDoorOpen) ? $"<color={(terminalObj.isBigDoor && terminalObj.isDoorOpen ? "lime" : "red")}>" : "")}{terminalObj.objectCode.ToUpper()}{(!isActive || (terminalObj.isBigDoor && terminalObj.isDoorOpen) ? "</color>" : "")} - {objType}\n";
                    }
                }
                foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
                {
                    if ((Vector3.Distance((player.isPlayerDead && player.deadBody != null) ? player.deadBody.transform.position : player.transform.position, position) < 25.0f || fullListVisible) && (player.isPlayerControlled || player.isPlayerDead))
                        enemyListText += $"{(player.isPlayerDead ? "<color=red>" : "")}{player.playerUsername}{(player.isPlayerDead ? "</color>" : "")} (#{player.playerClientId}{(Plugin.CheckPlayerIsHost(player) ? " - HOST" : "")})\n";
                }
                foreach (EnemyAI enemy in FindObjectsOfType<EnemyAI>())
                {
                    if (Vector3.Distance(enemy.transform.position, position) < 25.0f || fullListVisible)
                        enemyListText += $"{(enemy.isEnemyDead ? "<color=red>" : "")}{enemy.enemyType.enemyName}{(enemy.isEnemyDead ? "</color>" : "")} ({enemy.NetworkObjectId})\n";
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
