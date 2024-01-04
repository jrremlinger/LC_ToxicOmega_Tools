using DigitalRuby.ThunderAndLightning;
using GameNetcodeStuff;
using HarmonyLib;
using LC_API.GameInterfaceAPI.Features;
using LC_API.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManager_Patch
    {
        private static int itemListPage;
        private static int outsideListPage;
        private static int insideListPage;
        private static int itemID;
        private static int itemCount;
        private static int itemValue;
        private static int enemyID;
        private static int spawnCount;
        private static bool spawnOnPlayer;
        private static PlayerControllerB playerTarget;

        [HarmonyPatch("SubmitChat_performed")]
        [HarmonyPrefix]
        private static void RegisterChatCommand(HUDManager __instance /*, string chatMessage, string nameOfUserWhoTyped*/)
        {
            // Grab instances and refs as soon as chat is submitted
            PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            RoundManager currentRound = RoundManager.Instance;
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;

            bool flag = true;   // Chat will not be sent if flag = true; If no command prefix is recognized it will be set to false
            string chatMessage = __instance.chatTextField.text;

            // Split chat message up by spaces, trim trailing spaces
            string[] vs = chatMessage.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.TrimEnd())
                .ToArray();

            // Only run commands if user is host
            if (!Player.Get(localPlayerController).IsHost)
            {
                return;
            }

            switch (vs[0].Replace("/", "").ToLower())
            {
                case "item":
                case "items":   // Lists all items with their ID numbers
                    if (vs.Length > 1)
                    {
                        int.TryParse(vs[1], out itemListPage);
                    }
                    itemListPage = Math.Max(itemListPage, 1);

                    FindPage(allItemsList, itemListPage, 20, "Item");
                    break;
                case "give":    // Spawns one or more items at a player, custom item value can be set
                    itemID = 0;
                    itemCount = 1;
                    itemValue = -1;
                    playerTarget = localPlayerController;

                    if (vs.Length < 2)
                    {
                        string text = $"\nItem List (ID | Name) Total Items: {allItemsList.Count}";
                        for (int i = 0; i < allItemsList.Count; i++)
                        {
                            text += $"\n{i} | {allItemsList[i].itemName}";
                        }
                        Plugin.mls.LogInfo(text);
                        FindPage(allItemsList, 1, 20, "Item");
                        break;
                    }

                    if (!int.TryParse(vs[1], out itemID) || itemID >= allItemsList.Count)
                    {
                        Plugin.LogMessage($"Item ID \"{vs[1]}\" not found!", true);
                        break;
                    }

                    if (vs.Length > 2)
                    {
                        int.TryParse(vs[2], out itemCount);
                            
                        if (itemCount <= 0)
                        {
                            itemCount = 1;
                        }
                    }

                    if (vs.Length > 3)
                    {
                        if (vs[3] == "$")
                        {
                            itemValue = -1;
                        }
                        else
                        {
                            int.TryParse(vs[3], out itemValue);

                            if (itemValue < 0)
                            {
                                itemValue = 0;
                            }
                        }
                    }

                    if (vs.Length > 4)
                    {
                        string targetUsername = string.Join(" ", vs.Skip(4)).ToLower();
                        playerTarget = Plugin.FindPlayerFromString(targetUsername);
                    }

                    if (playerTarget != null)
                    {
                        Plugin.SpawnItem(itemID, itemCount, itemValue, playerTarget);
                    }
                    break;
                case "eout":
                case "enemyout":
                case "enemiesout":  // Lists all outside enemies with their ID numbers
                    if (vs.Length > 1)
                    {
                        int.TryParse(vs[1], out outsideListPage);
                    }
                    outsideListPage = Math.Max(outsideListPage, 1);

                    FindPage(currentRound.currentLevel.OutsideEnemies, outsideListPage, 20, "Outside Enemy");
                    break;
                case "ein":
                case "enemyin":
                case "enemiesin":   // Lists all inside enemies with their ID numbers
                    if (vs.Length > 1)
                    {
                        int.TryParse(vs[1], out insideListPage);
                    }
                    insideListPage = Math.Max(insideListPage, 1);

                    FindPage(currentRound.currentLevel.Enemies, insideListPage, 20, "Inside Enemy");
                    break;
                case "sout":
                case "spawn0":
                case "spawnout":    // Spawns one or more of an outside creature either at its normal spawnpoint or on a player
                    enemyID = 0;
                    spawnCount = 1;
                    spawnOnPlayer = false;
                    playerTarget = localPlayerController;

                    if (vs.Length < 2)
                    {
                        string text = $"\nEnemy List (ID | Name) Total Enemies: {currentRound.currentLevel.OutsideEnemies.Count}";
                        if (currentRound.currentLevel.OutsideEnemies.Count > 0)
                        {
                            for (int i = 0; i < currentRound.currentLevel.OutsideEnemies.Count; i++)
                            {
                                text += $"\n{i} | {currentRound.currentLevel.OutsideEnemies[i].enemyType.enemyName}";
                            }
                            Plugin.mls.LogInfo(text);
                            FindPage(currentRound.currentLevel.OutsideEnemies, 1, 20, "Outside Enemy");
                        }
                        else
                        {
                            Plugin.LogMessage("No valid Enemies for this location!", true);
                        }
                        break;
                    }

                    if (!int.TryParse(vs[1], out enemyID) || enemyID >= currentRound.currentLevel.OutsideEnemies.Count)
                    {
                        Plugin.LogMessage($"Enemy ID \"{vs[1]}\" not found!", true);
                        break;
                    }

                    if (vs.Length > 2)
                    {
                        int.TryParse(vs[2], out spawnCount);

                        if (spawnCount <= 0)
                        {
                            spawnCount = 1;
                        }
                    }

                    if (vs.Length > 3)
                    {
                        string targetUsername = string.Join(" ", vs.Skip(3)).ToLower();
                        playerTarget = Plugin.FindPlayerFromString(targetUsername);
                        spawnOnPlayer = true;
                    }

                    if (playerTarget != null)
                    {
                        Plugin.SpawnEnemy(enemyID, spawnCount, spawnOnPlayer, playerTarget, false);
                    }
                    break;
                case "sin":
                case "spawn":
                case "spawn1":
                case "spawnin": // Spawns one or more of an outside creature either at its normal spawnpoint or on a player
                    enemyID = 0;
                    spawnCount = 1;
                    spawnOnPlayer = false;
                    playerTarget = localPlayerController;

                    if (vs.Length < 2)
                    {
                        string text = $"\nEnemy List (ID | Name) Total Enemies: {currentRound.currentLevel.Enemies.Count}";
                        if (currentRound.currentLevel.Enemies.Count > 0)
                        {
                            for (int i = 0; i < currentRound.currentLevel.Enemies.Count; i++)
                            {
                                text += $"\n{i} | {currentRound.currentLevel.Enemies[i].enemyType.enemyName}";
                            }
                            Plugin.mls.LogInfo(text);
                            FindPage(currentRound.currentLevel.Enemies, 1, 20, "Inside Enemy");
                        }
                        else
                        {
                            Plugin.LogMessage("No valid Enemies for this location!", true);
                        }
                        break;
                    }

                    if (!int.TryParse(vs[1], out enemyID) || enemyID >= currentRound.currentLevel.Enemies.Count)
                    {
                        Plugin.LogMessage($"Enemy ID \"{vs[1]}\" not found!", true);
                        break;
                    }

                    if (vs.Length > 2)
                    {
                        int.TryParse(vs[2], out spawnCount);
                    }

                    if (vs.Length > 3)
                    {
                        string targetUsername = string.Join(" ", vs.Skip(3)).ToLower();
                        playerTarget = Plugin.FindPlayerFromString(targetUsername);
                        spawnOnPlayer = true;
                    }

                    if (playerTarget != null)
                    {
                        Plugin.SpawnEnemy(enemyID, spawnCount, spawnOnPlayer, playerTarget, true);
                    }
                    break;
                case "tp":
                case "tele":
                case "teleport":    // Teleports a player to a given destination
                    PlayerControllerB playerA = null;
                    PlayerControllerB playerB = null;

                    // Adjust target and destination based on the amount of arguments given
                    switch (vs.Length)
                    {
                        case 1: // Providing no arguments teleports the localPlayer to the ship's terminal
                            if (terminal != null)
                            {
                                if (localPlayerController.redirectToEnemy != null)
                                {
                                    localPlayerController.redirectToEnemy.ShipTeleportEnemy();
                                }
                                Network.Broadcast("TOT_TP_PLAYER", new TOT_TP_PLAYER_Broadcast { isInside = false, playerClientId = localPlayerController.playerClientId });
                                Player.Get(localPlayerController).Position = terminal.transform.position;
                                Plugin.LogMessage($"Teleported {localPlayerController.playerUsername} to terminal.");
                            }
                            else
                            {
                                Plugin.LogMessage("Terminal not found!", true);
                                break;
                            }
                            break;
                        case 2: // Providing one argument can teleport the localPlayer to different player, or to a random location in the factory
                            if (vs[1] == "$")   // "$" character as the destination chooses a random location inside the factory
                            {
                                if (RoundManager.Instance.insideAINodes.Length != 0)
                                {
                                    Vector3 position2 = RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
                                    Debug.DrawRay(position2, Vector3.up * 1f, Color.red);
                                    Vector3 inRadiusSpherical = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(position2);
                                    Debug.DrawRay(inRadiusSpherical + Vector3.right * 0.01f, Vector3.up * 3f, Color.green);

                                    if (UnityEngine.Object.FindObjectOfType<AudioReverbPresets>())
                                    {
                                        UnityEngine.Object.FindObjectOfType<AudioReverbPresets>().audioPresets[2].ChangeAudioReverbForPlayer(localPlayerController);
                                    }
                                    Network.Broadcast("TOT_TP_PLAYER", new TOT_TP_PLAYER_Broadcast { isInside = true, playerClientId = localPlayerController.playerClientId });
                                    Player.Get(localPlayerController).Position = inRadiusSpherical;
                                    Plugin.LogMessage($"Teleported {localPlayerController.playerUsername} to random location within factory.");
                                    break;
                                }
                                else
                                {
                                    Plugin.LogMessage($"No insideAINodes in this area!", true);
                                    break;
                                }
                            }

                            playerA = Plugin.FindPlayerFromString(vs[1]);

                            if (playerA != null)
                            {
                                if (playerA.playerClientId != localPlayerController.playerClientId)
                                {
                                    Network.Broadcast("TOT_TP_PLAYER", new TOT_TP_PLAYER_Broadcast { isInside = playerA.isInsideFactory, playerClientId = localPlayerController.playerClientId });
                                    Player.Get(localPlayerController).Position = playerA.transform.position;
                                    Plugin.LogMessage($"Teleported {localPlayerController.playerUsername} to {playerA.playerUsername}.");
                                }
                                else
                                {
                                    Plugin.LogMessage("Player destination cannot be yourself!", true);
                                    break;
                                }
                            }
                            break;
                        case 3: // Providing two arguments will use the first as a target and the second as a destination
                            playerA = Plugin.FindPlayerFromString(vs[1]);

                            if (vs[2] == "$")   // "$" character as the destination chooses a random location inside the factory
                            {
                                if (RoundManager.Instance.insideAINodes.Length != 0)
                                {
                                    Vector3 position2 = RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
                                    Debug.DrawRay(position2, Vector3.up * 1f, Color.red);
                                    Vector3 inRadiusSpherical = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(position2);
                                    Debug.DrawRay(inRadiusSpherical + Vector3.right * 0.01f, Vector3.up * 3f, Color.green);

                                    if (UnityEngine.Object.FindObjectOfType<AudioReverbPresets>())
                                    {
                                        UnityEngine.Object.FindObjectOfType<AudioReverbPresets>().audioPresets[2].ChangeAudioReverbForPlayer(localPlayerController);
                                    }
                                    if (playerA != null)
                                    {
                                        Network.Broadcast("TOT_TP_PLAYER", new TOT_TP_PLAYER_Broadcast { isInside = true, playerClientId = playerA.playerClientId });
                                        Player.Get(playerA).Position = inRadiusSpherical;
                                        Plugin.LogMessage($"Teleported {playerA.playerUsername} to random location within factory.");
                                    }
                                }
                                else
                                {
                                    Plugin.LogMessage($"No insideAINodes in this area!", true);
                                }
                            }
                            else if(vs[2] == "!")   // "!" character as the destination chooses the ship terminal
                            {
                                if (terminal != null && playerA != null)
                                {
                                    if (playerA.redirectToEnemy != null)
                                    {
                                        playerA.redirectToEnemy.ShipTeleportEnemy();
                                    }
                                    Network.Broadcast("TOT_TP_PLAYER", new TOT_TP_PLAYER_Broadcast { isInside = false, playerClientId = playerA.playerClientId });
                                    Player.Get(playerA).Position = terminal.transform.position;
                                    Plugin.LogMessage($"Teleported {playerA.playerUsername} to terminal.");
                                }
                                else
                                {
                                    Plugin.LogMessage("Terminal not found!", true);
                                }
                            }
                            else
                            {
                                playerB = Plugin.FindPlayerFromString(vs[2]);

                                if (playerA != null && playerB != null)
                                {
                                    if (playerA.playerClientId != playerB.playerClientId)
                                    {
                                        Network.Broadcast("TOT_TP_PLAYER", new TOT_TP_PLAYER_Broadcast { isInside = playerB.isInsideFactory, playerClientId = playerA.playerClientId });
                                        Player.Get(playerA).Position = playerB.transform.position;
                                        Plugin.LogMessage($"Teleported {playerA.playerUsername} to {playerB.playerUsername}.");
                                    }
                                    else
                                    {
                                        Plugin.LogMessage("Player destination cannot be the same player!", true);
                                    }
                                }
                            }
                            break;
                    }
                    break;
                case "ch":
                case "charge":  // Charges a players held item if it uses a battery
                    if (vs.Length < 2)
                    {
                        playerTarget = localPlayerController;
                    }
                    else
                    {
                        string targetUsername = string.Join(" ", vs.Skip(1)).ToLower();
                        playerTarget = Plugin.FindPlayerFromString(targetUsername);
                    }

                    if (playerTarget != null)
                    {
                        GrabbableObject foundItem = playerTarget.ItemSlots[playerTarget.currentItemSlot];
                        if (foundItem != null)
                        {
                            if (foundItem.itemProperties.requiresBattery)
                            {
                                Network.Broadcast("TOT_CHARGE_PLAYER", new TOT_PLAYER_Broadcast { playerClientId = playerTarget.playerClientId });
                                Plugin.LogMessage($"Charging {playerTarget.playerUsername}'s item \"{foundItem.itemProperties.itemName}\".");
                            }
                            else
                            {
                                Plugin.LogMessage($"{playerTarget.playerUsername}'s item \"{foundItem.itemProperties.itemName}\" does not use a battery!", true);
                            }
                        }
                        else
                        {
                            Plugin.LogMessage($"{playerTarget.playerUsername} is not holding an item!", true);
                        }
                    }
                    break;
                case "heal":
                case "save":    // Sets player health and stamina to max, saves player if in death animation with enemy
                    if (vs.Length < 2)
                    {
                        playerTarget = localPlayerController;
                    }
                    else
                    {
                        string targetUsername = string.Join(" ", vs.Skip(1)).ToLower();
                        playerTarget = Plugin.FindPlayerFromString(targetUsername);
                    }

                    if (playerTarget != null)
                    {
                        Network.Broadcast("TOT_HEAL_PLAYER", new TOT_PLAYER_Broadcast { playerClientId = playerTarget.playerClientId });
                        Player.Get(playerTarget).SprintMeter = 100f;
                        Player.Get(playerTarget).Health = 100;
                        Player.Get(playerTarget).Hurt(-1);  // Player health/status likes not not update unless a damage function is called
                        Plugin.LogMessage($"Healing {playerTarget.playerUsername}.");
                    }
                    break;
                case "list":
                case "player":
                case "players": // List currently connected player names with their ID numbers
                    string playerList = "";
                    foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
                    {
                        if (player.isPlayerControlled)
                        {
                            playerList += $"Player #{player.playerClientId}: {player.playerUsername}\n";
                        }
                    }
                    Plugin.LogMessage(playerList);
                    break;
                case "credit":
                case "credits":
                case "money":   // View or adjust current amount of groupCredits
                    if (terminal != null)
                    {
                        if (vs.Length < 2)
                        {
                            Plugin.LogMessage($"Group Credits: {terminal.groupCredits}");
                        }
                        else
                        {
                            int.TryParse(vs[1], out int creditsChange);
                            Network.Broadcast("TOT_TERMINAL_CREDITS", new TOT_INT_Broadcast { dataInt = creditsChange });
                            Plugin.LogMessage($"Adjusted Credits by {creditsChange}.\nNew Total: {terminal.groupCredits}.");
                        }
                    }
                    else
                    {
                        Plugin.LogMessage("Terminal not found!", true);
                    }
                    break;
                //case "smite":
                //case "strike":
                //    if (vs.Length < 2)
                //    {
                //        playerTarget = localPlayerController;
                //    }
                //    else
                //    {
                //        string targetUsername = string.Join(" ", vs.Skip(1)).ToLower();
                //        playerTarget = Plugin.FindPlayerFromString(targetUsername);
                //    }

                //    if (playerTarget != null)
                //    {
                //        //Network.Broadcast("TOT_SMITE_PLAYER", new TOT_PLAYER_Broadcast { playerClientId = playerTarget.playerClientId });
                //        StormyWeather storm = new StormyWeather();
                //        RoundManager.Instance.LightningStrikeServerRpc(playerTarget.transform.position);
                //        UnityEngine.Object.FindObjectOfType<StormyWeather>(includeInactive: true).LightningStrike(strikePosition, useTargetedObject: true);
                //        Plugin.LogMessage($"{playerTarget.playerUsername} has been smitten!");
                //    }
                //    break;
                default:
                    // No command recognized, send chat normally
                    flag = false;
                    break;
            }

            if (flag)
            {
                //Empty chatTextField, this prevents anything from being sent to the in-game chat
                __instance.chatTextField.text = string.Empty;
            }
        }

        private static void FindPage<T>(List<T> list, int page, int itemsPerPage, string listName)
        {
            RoundManager currentRound = RoundManager.Instance;
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;

            int totalItems = list.Count;
            int maxPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);

            int startIndex = (page - 1) * itemsPerPage;
            int endIndex = startIndex + itemsPerPage - 1;

            endIndex = Math.Min(endIndex, totalItems - 1);

            if (startIndex < 0 || startIndex >= totalItems || startIndex > endIndex)
            {
                if (startIndex >= totalItems)
                {
                    Plugin.LogMessage($"{listName} list is empty!", true);
                    return;
                }
                Plugin.LogMessage("Invalid page number! Please enter a valid page number.", true);
            }
            else
            {
                string pageText = "";
                bool flag = false;

                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (listName == "Item")
                    {
                        pageText += $"{allItemsList[i].itemName}-{i} ";
                    }
                    else if (listName == "Outside Enemy")
                    {
                        pageText += $"{currentRound.currentLevel.OutsideEnemies[i].enemyType.enemyName}-{i} ";
                        flag = true;
                    }
                    else if (listName == "Inside Enemy")
                    {
                        pageText += $"{currentRound.currentLevel.Enemies[i].enemyType.enemyName}-{i} ";
                        flag = true;
                    }
                }

                if (flag)
                {
                    listName = "Enemy";
                }

                string pageHeader = $"{listName} List (Page {page} of {maxPages})";
                HUDManager.Instance.DisplayTip(pageHeader, pageText, false, false, "LC_Tip1");
            }
        }
    }
}