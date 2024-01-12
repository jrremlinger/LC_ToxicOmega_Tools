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
using UnityEngine.EventSystems;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManager_Patch
    {
        public static bool sendPlayerInside = true;
        private static int itemListPage;
        private static int enemyListPage;
        private static int itemID;
        private static int itemCount;
        private static int itemValue;
        private static int enemyID;
        private static int spawnCount;
        private static int trapID;
        private static string playerString = "";
        private static PlayerControllerB playerTarget = null;
        public static List<SpawnableEnemyWithRarity> allEnemiesList;

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

            allEnemiesList = new List<SpawnableEnemyWithRarity>();
            allEnemiesList.AddRange(Plugin.Instance.customOutsideList);
            allEnemiesList.AddRange(Plugin.Instance.customInsideList);

            bool flag = true;   // Chat will not be sent if flag = true; If no command prefix is recognized it will be set to false
            string chatMessage = __instance.chatTextField.text;

            // SubmitChat_performed runs anytime "Enter" is pressed, even if chat is closed. This check prevents "Index out of range" when pressing enter in other situations like the terminal
            if (chatMessage == null || chatMessage == "")
            {
                return true;
            }

            // Split chat message up by spaces, trim trailing spaces
            string[] command = chatMessage
                .Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.TrimEnd().ToLowerInvariant())
                .ToArray();

            // Only run commands if user is host
            if (!Plugin.CheckPlayerIsHost(localPlayerController))
            {
                return true;
            }

            switch (command[0].Replace("/", "").ToLower())
            {
                case "help":
                    List<string> commandList = new List<string> 
                    { 
                        "Item: Lists spawnable items",
                        "Give: Spawns items",
                        "Enemy: Lists spawnable enemies",
                        "Spawn: Spawns enemies",
                        "TP: Teleport players",
                        "WP: Creates/lists waypoints",
                        "Charge: Charges a player's held item",
                        "Heal: Heals/revives a player",
                        "List: List current Players/Items/Enemies",
                        "Credit: Lists/adjusts spendable credits",
                        "Code: Lists/toggles blast doors and traps",
                        "Breaker: Toggles breaker box"
                    };

                    int helpPage = 1;

                    if (command.Length > 1)
                    {
                        int.TryParse(command[1], out helpPage);
                    }
                    helpPage = Math.Max(helpPage, 1);

                    FindPage(commandList, helpPage, 4, "Command");
                    break;
                case "it":
                case "item":
                case "items":   // Lists all items with their ID numbers
                    if (command.Length > 1)
                    {
                        int.TryParse(command[1], out itemListPage);
                    }
                    itemListPage = Math.Max(itemListPage, 1);

                    FindPage(allItemsList, itemListPage, 10, "Item");
                    break;
                case "gi":
                case "give":    // Spawns one or more items at a player, custom item value can be set
                    playerString = "";
                    itemID = 0;
                    itemCount = 1;
                    itemValue = -1;

                    if (command.Length < 2)
                    {
                        FindPage(allItemsList, 1, 10, "Item");
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
                        itemCount = Math.Max(itemCount, 1);
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
                case "en":
                case "enemy":
                case "enemies":   // Lists all enemies with their ID numbers
                    if (command.Length > 1)
                    {
                        int.TryParse(command[1], out enemyListPage);
                    }
                    enemyListPage = Math.Max(enemyListPage, 1);

                    FindPage(allEnemiesList, enemyListPage, 10, "Enemy");
                    break;
                case "sp":
                case "spawn":
                    enemyID = 0;
                    spawnCount = 1;
                    playerString = "";

                    if (command.Length < 2)
                    {
                        if (allEnemiesList.Count > 0)
                        {
                            FindPage(allEnemiesList, 1, 10, "Enemy");
                        }
                        else
                        {
                            Plugin.LogMessage("No valid Enemies for this location!", true);
                        }
                        break;
                    }

                    if (!int.TryParse(command[1], out enemyID) || enemyID >= allEnemiesList.Count || enemyID < 0)
                    {
                        Plugin.LogMessage($"Enemy ID \"{command[1]}\" not found!", true);
                        break;
                    }

                    if (command.Length > 2)
                    {
                        int.TryParse(command[2], out spawnCount);
                        spawnCount = Math.Max(spawnCount, 1);
                    }

                    if (command.Length > 3)
                    {
                        playerString = string.Join(" ", command.Skip(3)).ToLower();
                    }

                    Plugin.SpawnEnemy(enemyID, spawnCount, playerString);
                    break;
                case "tr":
                case "trap":
                    spawnCount = 1;
                    playerString = "";

                    if (command.Length < 2)
                    {
                        HUDManager.Instance.DisplayTip("Trap List", $"ID #0: Mine\nID #1: Turret");
                        break;
                    }

                    if ("mine".StartsWith(command[1]) || command[1] == "0")
                    {
                        trapID = 0;
                    }
                    else if ("turret".StartsWith(command[1]) || command[1] == "1")
                    {
                        trapID = 1;
                    }
                    else
                    {
                        Plugin.LogMessage($"Unable to find a trap with name {command[1]}!", true);
                        break;
                    }

                    if (command.Length > 2)
                    {
                        int.TryParse(command[2], out spawnCount);
                        spawnCount = Math.Max(spawnCount, 1);
                    }

                    if (command.Length > 3)
                    {
                        playerString = string.Join(" ", command.Skip(3)).ToLower();
                    }

                    Plugin.SpawnTrap(trapID, spawnCount, playerString);
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
                        if (Plugin.Instance.waypoints.Count > 0)
                        {
                            string pageText = "";

                            for (int i = 0; i < Plugin.Instance.waypoints.Count; i++)
                            {
                                pageText += $"@{i}{Plugin.Instance.waypoints[i].position}, ";
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
                            Plugin.Instance.waypoints.Add(new Waypoint { isInside = wpInside, position = wpPosition });
                            Plugin.LogMessage($"Waypoint @{Plugin.Instance.waypoints.Count - 1} created at {wpPosition}.");
                        }
                    }
                    else if ("clear".StartsWith(command[1]))
                    {
                        Plugin.Instance.waypoints.Clear();
                        Plugin.LogMessage($"Waypoints cleared.");
                    }
                    else if ("door".StartsWith(command[1]) || "entrance".StartsWith(command[1]))
                    {
                        Plugin.Instance.waypoints.Add(new Waypoint { isInside = true, position = RoundManager.FindMainEntrancePosition(true) });
                        Plugin.LogMessage($"Waypoint @{Plugin.Instance.waypoints.Count - 1} created at Main Entrance.");
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
                case "he":
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
                case "li":
                case "list": // List currently connected player names with their ID numbers
                    string listName = "";
                    int listPage = 1;

                    if (command.Length > 2)
                    {
                        int.TryParse(command[2], out listPage);
                    }
                    listPage = Math.Max(listPage, 1);

                    if (command.Length < 2 || "players".StartsWith(command[1]))
                    {
                        List<PlayerControllerB> activePlayers = StartOfRound.Instance.allPlayerScripts.ToList();
                        listName = "Player";
                        FindPage(activePlayers, listPage, 10, listName);
                    }
                    else if ("items".StartsWith(command[1]))
                    {
                        List<GrabbableObject> activeItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>().ToList();
                        listName = "Active Item";
                        FindPage(activeItems, listPage, 10, listName);
                    }
                    else if ("enemy".StartsWith(command[1]) || "enemies".StartsWith(command[1]))
                    {
                        List<EnemyAI> activeEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>().ToList();
                        listName = "Active Enemy";
                        FindPage(activeEnemies, listPage, 10, listName);
                    }
                    else
                    {
                        Plugin.LogMessage($"Unable to find list by name {command[1]}!", true);
                        break;
                    }

                    break;
                case "cr":
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
                case "co":
                case "code":
                case "codes":
                    TerminalAccessibleObject[] terminalObjects = UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>();
                    
                    if (terminalObjects.Length > 0)
                    {
                        if (command.Length < 2)
                        {
                            string objectList = "";
                            foreach (TerminalAccessibleObject obj in terminalObjects)
                            {
                                if (obj.objectCode != null)
                                {
                                    objectList += $"{obj.objectCode}";
                                    if (obj.isBigDoor)
                                    {
                                        objectList += "(Door), ";
                                    }
                                    else if (obj.GetComponentInChildren<Turret>())
                                    {
                                        objectList += "(Turret), ";
                                    }
                                    else if (obj.GetComponentInChildren<Landmine>())
                                    {
                                        objectList += "(Landmine), ";
                                    }
                                }
                            }
                            objectList = objectList.TrimEnd(',', ' ') + ".";
                            HUDManager.Instance.DisplayTip("Code List", objectList);
                        }
                        else
                        {
                            foreach (TerminalAccessibleObject obj in terminalObjects)
                            {
                                if (obj != null && obj.objectCode == command[1])
                                {
                                    obj.CallFunctionFromTerminal();

                                    if (obj.GetComponentInChildren<Turret>())
                                    {
                                        obj.GetComponentInChildren<Turret>().ToggleTurretEnabled(false);
                                    }
                                    else if (obj.GetComponentInChildren<Landmine>())
                                    {
                                        obj.GetComponentInChildren<Landmine>().ToggleMine(false);
                                    }
                                }
                            }

                            Plugin.LogMessage($"Attempted to toggle all TerminalAccessibleObject of code {command[1]}.");
                        }
                    }
                    else
                    {
                        Plugin.LogMessage($"No TerminalAccessibleObject in this area!", true);
                    }
                    break;
                case "br":
                case "breaker":
                    BreakerBox breaker = UnityEngine.Object.FindObjectOfType<BreakerBox>();
                    if (breaker != null)
                    {
                        breaker.SwitchBreaker(!breaker.isPowerOn);
                        Plugin.LogMessage($"Turned breaker {(breaker.isPowerOn ? "on" : "off")}.");
                    }
                    else
                    {
                        Plugin.LogMessage("BreakerBox not found!", true);
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
        
        private static void FindPage<T>(List<T> list, int page, int itemsPerPage, string listName)
        {
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;
            List<PlayerControllerB> activePlayersList = StartOfRound.Instance.allPlayerScripts.ToList();
            List<GrabbableObject> activeItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>().ToList();
            List<EnemyAI> activeEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>().ToList();

            bool appendList = true;

            int totalItems = list.Count;
            int maxPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);

            page = Math.Min(page, maxPages);

            int startIndex = (page - 1) * itemsPerPage;
            int endIndex = startIndex + itemsPerPage - 1;

            endIndex = Math.Min(endIndex, totalItems - 1);

            if (startIndex < 0 || startIndex >= totalItems || startIndex > endIndex)
            {
                if (startIndex >= totalItems || list.Count == 0)
                {
                    Plugin.LogMessage($"{listName} list is empty!", true);
                    return;
                }
                Plugin.LogMessage("Invalid page number! Please enter a valid page number.", true);
            }
            else
            {
                string pageText = "";

                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (listName == "Item")
                    {
                        pageText += $"{allItemsList[i].itemName}({i}), ";
                    }
                    else if (listName == "Enemy")
                    {
                        pageText += $"{allEnemiesList[i].enemyType.enemyName}({i}), ";
                    }
                    else if (listName == "Command")
                    {
                        pageText += $"{list[i]}\n";
                    }
                    else if (listName == "Player")
                    {
                        if (activePlayersList[i].isPlayerControlled)
                        {
                            pageText += $"Player #{activePlayersList[i].playerClientId}: {activePlayersList[i].playerUsername}\n";
                        }
                    }
                    else if (listName == "Active Item")
                    {
                        pageText += $"{activeItems[i].itemProperties.itemName}, ";
                        appendList = false;
                    }
                    else if (listName == "Active Enemy")
                    {
                        pageText += $"{activeEnemies[i].enemyType.enemyName}, ";
                        appendList = false;
                    }
                }

                pageText = pageText.TrimEnd(',', ' ', '\n') + (listName == "Player" ? "" : ".");
                string pageHeader = $"{listName}{(appendList ? " List" : "")} (Page {page} of {maxPages})";
                HUDManager.Instance.DisplayTip(pageHeader, pageText);
            }
        }
    }
}