using DigitalRuby.ThunderAndLightning;
using GameNetcodeStuff;
using HarmonyLib;
using LC_API.GameInterfaceAPI.Features;
using LC_API.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManager_Patch
    {
        public static bool sendPlayerInside = true;

        private static int itemListPage;
        private static int outsideListPage;
        private static int insideListPage;
        private static int itemID;
        private static int itemCount;
        private static int itemValue;
        private static int enemyID;
        private static int spawnCount;
        private static string playerString = "";
        private static PlayerControllerB playerTarget = null;

        [HarmonyPatch("EnableChat_performed")]
        [HarmonyPrefix]
        private static bool EnableChatAction(HUDManager __instance) // Allows host to open chat while dead
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (localPlayer.isPlayerDead && Player.HostPlayer.ClientId == localPlayer.playerClientId)
            {
                ShipBuildModeManager.Instance.CancelBuildMode();
                __instance.localPlayer.isTypingChat = true;
                __instance.chatTextField.Select();
                __instance.PingHUDElement(__instance.Chat, 0.1f, endAlpha: 1f);
                __instance.typingIndicator.enabled = true;
                Plugin.mls.LogError("HUD 3");
                return false;
            }
            return true;
        }

        [HarmonyPatch("SubmitChat_performed")]
        [HarmonyPrefix]
        private static bool RegisterChatCommand(HUDManager __instance)
        {
            // Grab instances and refs as soon as chat is submitted
            RoundManager currentRound = RoundManager.Instance;
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;
            PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;

            bool flag = true;   // Chat will not be sent if flag = true; If no command prefix is recognized it will be set to false
            string chatMessage = __instance.chatTextField.text;

            // SubmitChat_performed runs anytime "Enter" is pressed, even if chat is closed. This check prevents "Index out of range" when pressing enter in other situations like the terminal
            if (chatMessage == null || chatMessage == "")
            {
                return true;
            }

            // Split chat message up by spaces, trim trailing spaces
            string[] command = chatMessage.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.TrimEnd())
                .ToArray();

            // Only run commands if user is host
            if (!Plugin.CheckPlayerIsHost(localPlayerController))
            {
                return true;
            }

            switch (command[0].Replace("/", "").ToLower())
            {
                case "item":
                case "items":   // Lists all items with their ID numbers
                    if (command.Length > 1)
                    {
                        int.TryParse(command[1], out itemListPage);
                    }
                    itemListPage = Math.Max(itemListPage, 1);

                    FindPage(allItemsList, itemListPage, "Item");
                    break;
                case "give":    // Spawns one or more items at a player, custom item value can be set
                    playerString = "";
                    itemID = 0;
                    itemCount = 1;
                    itemValue = -1;

                    if (command.Length < 2)
                    {
                        string text = $"\nItem List (ID | Name) Total Items: {allItemsList.Count}";
                        for (int i = 0; i < allItemsList.Count; i++)
                        {
                            text += $"\n{i} | {allItemsList[i].itemName}";
                        }
                        Plugin.mls.LogInfo(text);
                        FindPage(allItemsList, 1, "Item");
                        break;
                    }

                    if (!int.TryParse(command[1], out itemID) || itemID >= allItemsList.Count)
                    {
                        Plugin.LogMessage($"Item ID \"{command[1]}\" not found!", true);
                        break;
                    }

                    if (command.Length > 2)
                    {
                        int.TryParse(command[2], out itemCount);
                            
                        if (itemCount <= 0)
                        {
                            itemCount = 1;
                        }
                    }

                    if (command.Length > 3)
                    {
                        if (command[3] == "$")
                        {
                            itemValue = -1;
                        }
                        else
                        {
                            int.TryParse(command[3], out itemValue);

                            if (itemValue < 0)
                            {
                                itemValue = 0;
                            }
                        }
                    }

                    if (command.Length > 4)
                    {
                        playerString = string.Join(" ", command.Skip(4)).ToLower();
                    }

                    Plugin.SpawnItem(itemID, itemCount, itemValue, playerString);
                    break;
                case "eout":
                case "enemyout":
                case "enemiesout":  // Lists all outside enemies with their ID numbers
                    if (command.Length > 1)
                    {
                        int.TryParse(command[1], out outsideListPage);
                    }
                    outsideListPage = Math.Max(outsideListPage, 1);

                    FindPage(currentRound.currentLevel.OutsideEnemies, outsideListPage, "Outside Enemy");
                    break;
                case "ein":
                case "enemyin":
                case "enemiesin":   // Lists all inside enemies with their ID numbers
                    if (command.Length > 1)
                    {
                        int.TryParse(command[1], out insideListPage);
                    }
                    insideListPage = Math.Max(insideListPage, 1);

                    FindPage(currentRound.currentLevel.Enemies, insideListPage, "Inside Enemy");
                    break;
                case "sout":
                case "spawnout":    // Spawns one or more of an outside creature either at its normal spawnpoint or on a player
                    enemyID = 0;
                    spawnCount = 1;
                    playerString = "";

                    if (command.Length < 2)
                    {
                        string text = $"\nEnemy List (ID | Name) Total Enemies: {currentRound.currentLevel.OutsideEnemies.Count}";
                        if (currentRound.currentLevel.OutsideEnemies.Count > 0)
                        {
                            for (int i = 0; i < currentRound.currentLevel.OutsideEnemies.Count; i++)
                            {
                                text += $"\n{i} | {currentRound.currentLevel.OutsideEnemies[i].enemyType.enemyName}";
                            }
                            Plugin.mls.LogInfo(text);
                            FindPage(currentRound.currentLevel.OutsideEnemies, 1, "Outside Enemy");
                        }
                        else
                        {
                            Plugin.LogMessage("No valid Enemies for this location!", true);
                        }
                        break;
                    }

                    if (!int.TryParse(command[1], out enemyID) || enemyID >= currentRound.currentLevel.OutsideEnemies.Count)
                    {
                        Plugin.LogMessage($"Enemy ID \"{command[1]}\" not found!", true);
                        break;
                    }

                    if (command.Length > 2)
                    {
                        int.TryParse(command[2], out spawnCount);

                        if (spawnCount <= 0)
                        {
                            spawnCount = 1;
                        }
                    }

                    if (command.Length > 3)
                    {
                        playerString = string.Join(" ", command.Skip(3)).ToLower();
                    }

                    Plugin.SpawnEnemy(enemyID, spawnCount, playerString, false);
                    break;
                case "sin":
                case "spawnin": // Spawns one or more of an outside creature either at its normal spawnpoint or on a player
                    enemyID = 0;
                    spawnCount = 1;
                    playerString = "";

                    if (command.Length < 2)
                    {
                        string text = $"\nEnemy List (ID | Name) Total Enemies: {currentRound.currentLevel.Enemies.Count}";
                        if (currentRound.currentLevel.Enemies.Count > 0)
                        {
                            for (int i = 0; i < currentRound.currentLevel.Enemies.Count; i++)
                            {
                                text += $"\n{i} | {currentRound.currentLevel.Enemies[i].enemyType.enemyName}";
                            }
                            Plugin.mls.LogInfo(text);
                            FindPage(currentRound.currentLevel.Enemies, 1, "Inside Enemy");
                        }
                        else
                        {
                            Plugin.LogMessage("No valid Enemies for this location!", true);
                        }
                        break;
                    }

                    if (!int.TryParse(command[1], out enemyID) || enemyID >= currentRound.currentLevel.Enemies.Count)
                    {
                        Plugin.LogMessage($"Enemy ID \"{command[1]}\" not found!", true);
                        break;
                    }

                    if (command.Length > 2)
                    {
                        int.TryParse(command[2], out spawnCount);
                    }

                    if (command.Length > 3)
                    {
                        playerString = string.Join(" ", command.Skip(3)).ToLower();
                    }

                    Plugin.SpawnEnemy(enemyID, spawnCount, playerString, true);
                    break;
                case "tp":
                case "tele":
                case "teleport":    // Teleports a player to a given destination
                    switch (command.Length)
                    {
                        case 1:
                            if (Plugin.GetPositionFromCommand("!", 3, localPlayerController) != Vector3.zero)
                            {
                                if (!localPlayerController.isPlayerDead)
                                {
                                    Plugin.mls.LogInfo("RPC SENDING: \"TOT_TP_PLAYER\".");
                                    Network.Broadcast("TOT_TP_PLAYER", new TOT_TP_PLAYER_Broadcast { isInside = false, playerClientId = localPlayerController.playerClientId });
                                    Plugin.mls.LogInfo("RPC END: \"TOT_TP_PLAYER\".");
                                    Player.Get(localPlayerController).Position = Plugin.GetPositionFromCommand("!", 3, localPlayerController);
                                }
                                else
                                {
                                    Plugin.LogMessage($"Could not teleport {localPlayerController.playerUsername}!\nPlayer is dead!", true);
                                }
                            }
                            break;
                        case 2:
                        case 3:
                            PlayerControllerB playerA = command.Length > 2 ? Plugin.GetPlayerFromString(command[1]) : localPlayerController;

                            if (playerA != null && !playerA.isPlayerDead)
                            {
                                if (Plugin.GetPositionFromCommand(command.Length > 2 ? command[2] : command[1], 3, playerA) != Vector3.zero)
                                {
                                    Plugin.mls.LogInfo("RPC SENDING: \"TOT_TP_PLAYER\".");
                                    Network.Broadcast("TOT_TP_PLAYER", new TOT_TP_PLAYER_Broadcast { isInside = sendPlayerInside, playerClientId = playerA.playerClientId });
                                    Plugin.mls.LogInfo("RPC END: \"TOT_TP_PLAYER\".");

                                    Player.Get(playerA).Position = Plugin.GetPositionFromCommand(command.Length > 2 ? command[2] : command[1], 3, playerA);
                                }
                            }
                            else if (playerA != null && playerA.isPlayerDead)
                            {
                                Plugin.LogMessage($"Could not teleport {playerA.playerUsername}!\nPlayer is dead!", true);
                            }
                            break;
                    }
                    break;
                case "wp":
                case "waypoint":
                case "waypoints":
                    if (command.Length == 1)
                    {
                        if (Plugin.waypoints.Count > 0)
                        {
                            string pageText = "";

                            for (int i = 0; i < Plugin.waypoints.Count; i++)
                            {
                                pageText += $"@{i}{Plugin.waypoints[i].position}, ";
                            }

                            pageText = pageText.TrimEnd(',', ' ') + ".";
                            HUDManager.Instance.DisplayTip("Waypoint List", pageText);
                        }
                        else
                        {
                            Plugin.LogMessage("Waypoint List is empty!", true);
                        }
                    }
                    else if ("add".StartsWith(command[1]))
                    {
                        if (localPlayerController != null && !localPlayerController.isPlayerDead)
                        {
                            bool wpInside = Player.Get(localPlayerController).IsInFactory;
                            Vector3 wpPosition = localPlayerController.transform.position;
                            Plugin.waypoints.Add(new Waypoint { isInside = wpInside, position = wpPosition });
                            Plugin.LogMessage($"Waypoint @{ Plugin.waypoints.Count - 1 } created at {wpPosition}.");
                        }
                    }
                    else if ("clear".StartsWith(command[1]))
                    {
                        Plugin.waypoints.Clear();
                        Plugin.LogMessage($"Waypoints cleared.");
                    }
                    else if ("door".StartsWith(command[1]) || "entrance".StartsWith(command[1]))
                    {
                        Plugin.waypoints.Add(new Waypoint { isInside = true, position = RoundManager.FindMainEntrancePosition(true) });
                        Plugin.LogMessage($"Waypoint @{Plugin.waypoints.Count - 1} created at Main Entrance.");
                    }
                    break;
                case "ch":
                case "charge":  // Charges a players held item if it uses a battery
                    if (command.Length < 2)
                    {
                        playerTarget = localPlayerController;
                    }
                    else
                    {
                        string targetUsername = string.Join(" ", command.Skip(1)).ToLower();
                        playerTarget = Plugin.GetPlayerFromString(targetUsername);
                    }

                    if (playerTarget != null && !playerTarget.isPlayerDead)
                    {
                        GrabbableObject foundItem = playerTarget.ItemSlots[playerTarget.currentItemSlot];
                        if (foundItem != null)
                        {
                            if (foundItem.itemProperties.requiresBattery)
                            {
                                Plugin.mls.LogInfo("RPC SENDING: \"TOT_CHARGE_PLAYER\".");
                                Network.Broadcast("TOT_CHARGE_PLAYER", new TOT_PLAYER_Broadcast { playerClientId = playerTarget.playerClientId });
                                Plugin.mls.LogInfo("RPC END: \"TOT_CHARGE_PLAYER\".");
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
                    else if (playerTarget.isPlayerDead)
                    {
                        Plugin.LogMessage($"Could not charge {playerTarget.playerUsername}'s item!\nPlayer is dead!", true);
                    }
                    break;
                case "heal":
                case "save":    // Sets player health and stamina to max, saves player if in death animation with enemy
                    if (command.Length < 2)
                    {
                        playerTarget = localPlayerController;
                    }
                    else
                    {
                        string targetUsername = string.Join(" ", command.Skip(1)).ToLower();
                        playerTarget = Plugin.GetPlayerFromString(targetUsername);
                    }

                    if (playerTarget != null)
                    {
                        if (playerTarget.isPlayerDead)
                        {
                            Plugin.LogMessage($"Attempting to revive {playerTarget.playerUsername}.");
                        }
                        else
                        {
                            Plugin.LogMessage($"Healing {playerTarget.playerUsername}.");
                        }

                        Plugin.mls.LogInfo("RPC SENDING: \"TOT_HEAL_PLAYER\".");
                        Network.Broadcast("TOT_HEAL_PLAYER", new TOT_PLAYER_Broadcast { playerClientId = playerTarget.playerClientId });
                        Plugin.mls.LogInfo("RPC END: \"TOT_HEAL_PLAYER\".");
                        Player.Get(playerTarget).SprintMeter = 100f;
                        Player.Get(playerTarget).Health = 100;
                        Player.Get(playerTarget).Hurt(-1);  // Player health/status likes not not update unless a damage function is called
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
                        if (command.Length < 2)
                        {
                            Plugin.LogMessage($"Group Credits: {terminal.groupCredits}");
                        }
                        else
                        {
                            int.TryParse(command[1], out int creditsChange);
                            Plugin.mls.LogInfo("RPC SENDING: \"TOT_TERMINAL_CREDITS\".");
                            Network.Broadcast("TOT_TERMINAL_CREDITS", new TOT_INT_Broadcast { dataInt = creditsChange });
                            Plugin.mls.LogInfo("RPC END: \"TOT_TERMINAL_CREDITS\".");
                            Plugin.LogMessage($"Adjusted Credits by {creditsChange}.\nNew Total: {terminal.groupCredits}.");
                        }
                    }
                    else
                    {
                        Plugin.LogMessage("Terminal not found!", true);
                    }
                    break;
                default:
                    // No command recognized, send chat normally
                    flag = false;
                    break;
            }

            if (flag)
            {
                // Empty chatTextField, this prevents anything from being sent to the in-game chat
                __instance.chatTextField.text = string.Empty;
            }

            // Perform regular chat if player is the host and dead, this overrides the way the game blocks dead players from chatting.
            if (localPlayerController.isPlayerDead && Player.HostPlayer.ClientId == localPlayerController.playerClientId)
            {
                if (!string.IsNullOrEmpty(__instance.chatTextField.text) && __instance.chatTextField.text.Length < 50)
                    __instance.AddTextToChatOnServer(__instance.chatTextField.text, (int)__instance.localPlayer.playerClientId);
                for (int index = 0; index < StartOfRound.Instance.allPlayerScripts.Length; ++index)
                {
                    if (StartOfRound.Instance.allPlayerScripts[index].isPlayerControlled && (double)Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, StartOfRound.Instance.allPlayerScripts[index].transform.position) > 24.399999618530273 && (!GameNetworkManager.Instance.localPlayerController.holdingWalkieTalkie || !StartOfRound.Instance.allPlayerScripts[index].holdingWalkieTalkie))
                    {
                        __instance.playerCouldRecieveTextChatAnimator.SetTrigger("ping");
                        break;
                    }
                }
                localPlayerController.isTypingChat = false;
                __instance.chatTextField.text = "";
                EventSystem.current.SetSelectedGameObject((GameObject)null);
                __instance.PingHUDElement(__instance.Chat);
                __instance.typingIndicator.enabled = false;
                return false;
            }

            return true;
        }

        private static void FindPage<T>(List<T> list, int page, string listName)
        {
            RoundManager currentRound = RoundManager.Instance;
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;

            int itemsPerPage = 10;
            int totalItems = list.Count;
            int maxPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);

            int startIndex = (page - 1) * itemsPerPage;
            int endIndex = startIndex + itemsPerPage - 1;

            page = Math.Min(page, maxPages);
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
                        pageText += $"{allItemsList[i].itemName}({i}), ";
                    }
                    else if (listName == "Outside Enemy")
                    {
                        pageText += $"{currentRound.currentLevel.OutsideEnemies[i].enemyType.enemyName}({i}), ";
                        flag = true;
                    }
                    else if (listName == "Inside Enemy")
                    {
                        pageText += $"{currentRound.currentLevel.Enemies[i].enemyType.enemyName}({i}), ";
                        flag = true;
                    }
                }

                if (flag)
                {
                    listName = "Enemy";
                }

                pageText = pageText.TrimEnd(',', ' ') + ".";
                string pageHeader = $"{listName} List (Page {page} of {maxPages})";
                HUDManager.Instance.DisplayTip(pageHeader, pageText);
            }
        }
    }
}