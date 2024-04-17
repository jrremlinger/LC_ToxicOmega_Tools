using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
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
        private static int itemId;
        private static int itemCount;
        private static int itemValue;
        private static int enemyId;
        private static int spawnCount;
        private static int trapId;
        private static bool foundId;
        private static GrabbableObject itemTarget;
        private static EnemyAI enemyTarget;
        private static ulong networkId;
        private static string playerString = "";
        private static PlayerControllerB playerTarget = null;
        public static List<SpawnableEnemyWithRarity> allEnemiesList;

        [HarmonyPatch(nameof(HUDManager.EnableChat_performed))]
        [HarmonyPrefix]
        private static bool EnableChatAction(HUDManager __instance)
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;

            // Open chat skipping original method (and its dead player check) to allow host to open chat while dead
            if (localPlayer.isPlayerDead && Plugin.CheckPlayerIsHost(localPlayer))
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

        [HarmonyPatch(nameof(HUDManager.SubmitChat_performed))]
        [HarmonyPrefix]
        private static bool RegisterChatCommand(HUDManager __instance)
        {
            RoundManager currentRound = RoundManager.Instance;
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;
            PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;

            __instance.tipsPanelCoroutine = null;   // Clears vanilla tip coroutine to prevent Plugin.LogMessage() from being blocked

            allEnemiesList = new List<SpawnableEnemyWithRarity>();
            allEnemiesList.AddRange(Plugin.Instance.customOutsideList);
            allEnemiesList.AddRange(Plugin.Instance.customInsideList);

            bool flag = true;   // Chat will not be sent if flag = true; If no command prefix is recognized it will be set to false
            string chatMessage = __instance.chatTextField.text;


            // Return if SubmitChat_performed() runs when user is not actually sending a chat
            if (chatMessage == null || chatMessage == "")
                return true;

            // Split chat message up by spaces, trim trailing spaces, convert to lowercase
            string[] command = chatMessage
                .Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.TrimEnd().ToLowerInvariant())
                .ToArray();

            if (!Plugin.CheckPlayerIsHost(localPlayerController) && !(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
                return true;

            switch (command[0].Replace("/", "").ToLower())
            {
                case "help":
                    List<string> commandList = new List<string> 
                    { 
                        "Item: Lists spawnable items",
                        "Give: Spawns items",
                        "Enemy: Lists spawnable enemies",
                        "Spawn: Spawns enemies",
                        "Trap: Spawns traps",
                        "List: Lists existing players/items/enemies",
                        "GUI: Toggles a GUI displaying nearby items/enemies",
                        "TP: Teleport players",
                        "WP: Creates/lists waypoints",
                        "Heal: Heals/revives a player",
                        "Kill: Kills a player/item/enemy",
                        "GodMode: Toggles invincibility",
                        "Code: Toggles blast doors and traps",
                        "Breaker: Toggles breaker box",
                        "Credit: Adjusts spendable credits",
                        "Suit: Changes the suit of a player",
                        "Charge: Charges a player's held item",
                    };

                    int helpPage = 1;
                    if (command.Length > 1) 
                        int.TryParse(command[1], out helpPage);
                    helpPage = Math.Max(helpPage, 1);
                    FindPage(commandList, helpPage, 4, "Command");
                    break;
                case "it":
                case "item":
                case "items":   // Lists all items with their ID numbers
                    if (command.Length > 1) 
                        int.TryParse(command[1], out itemListPage);
                    itemListPage = Math.Max(itemListPage, 1);
                    FindPage(allItemsList, itemListPage, 6, "Item");
                    break;
                case "gi":
                case "give":    // Spawns one or more items at a player, custom item value can be set
                    playerString = "";
                    itemId = 0;
                    itemCount = 1;
                    itemValue = -1;

                    if (command.Length < 2)
                    {
                        FindPage(allItemsList, 1, 10, "Item");
                        break;
                    }

                    itemId = Plugin.GetItemFromString(command[1]);
                    if (itemId == -1)
                        break;

                    if (command.Length > 2)
                        int.TryParse(command[2], out itemCount);
                    itemCount = Math.Max(itemCount, 1);

                    if (command.Length > 3)
                    {
                        if (command[3] == "$")
                            itemValue = -1;
                        else
                        {
                            int.TryParse(command[3], out itemValue);
                            if (itemValue < 0)
                                itemValue = 0;
                        }
                    }

                    if (command.Length > 4)
                        playerString = string.Join(" ", command.Skip(4)).ToLower();

                    Plugin.SpawnItem(itemId, itemCount, itemValue, playerString);
                    break;
                case "en":
                case "enemy":
                case "enemies":   // Lists all enemies with their ID numbers
                    if (command.Length > 1)
                        int.TryParse(command[1], out enemyListPage);
                    enemyListPage = Math.Max(enemyListPage, 1);
                    FindPage(allEnemiesList, enemyListPage, 6, "Enemy");
                    break;
                case "sp":
                case "spawn":
                    enemyId = 0;
                    spawnCount = 1;
                    playerString = "";

                    if (command.Length < 2)
                    {
                        if (allEnemiesList.Count > 0)
                            FindPage(allEnemiesList, 1, 10, "Enemy");
                        else
                            Plugin.LogMessage("No valid Enemies for this location!", true);
                        break;
                    }

                    enemyId = Plugin.GetEnemyFromString(command[1]);
                    if (enemyId == -1)
                        break;

                    if (command.Length > 2)
                    {
                        int.TryParse(command[2], out spawnCount);
                        spawnCount = Math.Max(spawnCount, 1);
                    }

                    if (command.Length > 3)
                        playerString = string.Join(" ", command.Skip(3)).ToLower();

                    Plugin.SpawnEnemy(enemyId, spawnCount, playerString);
                    break;
                case "tr":
                case "trap":
                    spawnCount = 1;
                    playerString = "";

                    if (command.Length < 2)
                    {
                        HUDManager.Instance.DisplayTip("Trap List", "Mine, Turret, Spikes");
                        break;
                    }

                    if ("landmine".StartsWith(command[1]) || "mine".StartsWith(command[1]))
                        trapId = 0;
                    else if ("turret".StartsWith(command[1]))
                        trapId = 1;
                    else if ("spikes".StartsWith(command[1]) || "roofspikes".StartsWith(command[1]) || "ceilingspikes".StartsWith(command[1]))
                    {
                        trapId = 2;
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
                        playerString = string.Join(" ", command.Skip(3)).ToLower();

                    Plugin.SpawnTrap(trapId, spawnCount, playerString);
                    break;
                case "li":
                case "list": // List currently connected player names with their ID numbers
                    string listName = "";
                    int listPage = 1;

                    if (command.Length > 2)
                        int.TryParse(command[2], out listPage);
                    listPage = Math.Max(listPage, 1);

                    if (command.Length < 2 || "players".StartsWith(command[1]))
                    {
                        List<PlayerControllerB> activePlayers = StartOfRound.Instance.allPlayerScripts.ToList();
                        listName = "Player";
                        FindPage(activePlayers, listPage, 6, listName);
                    }
                    else if ("items".StartsWith(command[1]))
                    {
                        List<GrabbableObject> activeItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>().ToList();
                        listName = "Active Item";
                        FindPage(activeItems, listPage, 6, listName);
                    }
                    else if ("enemy".StartsWith(command[1]) || "enemies".StartsWith(command[1]))
                    {
                        List<EnemyAI> activeEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>().ToList();
                        listName = "Active Enemy";
                        FindPage(activeEnemies, listPage, 6, listName);
                    }
                    else
                    {
                        Plugin.LogMessage($"Unable to find list by name {command[1]}!", true);
                        break;
                    }
                    break;
                case "tp":
                case "tele":
                case "teleport":    // Teleports a player to a given destination
                    switch (command.Length)
                    {
                        case 1:
                            if (Plugin.GetPositionFromCommand("!", 3, localPlayerController.playerUsername) != Vector3.zero)
                            {
                                if (!localPlayerController.isPlayerDead)
                                {
                                    Plugin.mls.LogInfo("RPC SENDING: \"TPPlayerClientRpc\".");
                                    Vector3 destination = Plugin.GetPositionFromCommand("!", 3, localPlayerController.playerUsername);
                                    Networking.TPPlayerClientRpc(
                                        new TOT_TPPlayerData {
                                            isInside = false,
                                            playerClientId = localPlayerController.playerClientId,
                                            position = destination });
                                }
                                else
                                    Plugin.LogMessage($"Could not teleport {localPlayerController.playerUsername}!\nPlayer is dead!", true);
                            }
                            break;
                        case 2:
                        case 3:
                            // Look for item/enemy by ID and break the switch function if one is found
                            if (command.Length > 2)
                            {
                                Vector3 newPos = Vector3.zero;
                                foundId = ulong.TryParse(command[1], out networkId);
                                
                                if (foundId && Plugin.GetGrabbableObject(networkId) != null)
                                    newPos = Plugin.GetPositionFromCommand(command[2], 3, Plugin.GetGrabbableObject(networkId).itemProperties.itemName);
                                else if (foundId && Plugin.GetEnemyAI(networkId) != null)
                                    newPos = Plugin.GetPositionFromCommand(command[2], 3, Plugin.GetEnemyAI(networkId).enemyType.enemyName);

                                if (foundId && newPos != Vector3.zero)
                                {
                                    Plugin.mls.LogInfo("RPC SENDING: \"TPGameObjectClientRpc\".");
                                    Networking.TPGameObjectClientRpc(new TOT_TPGameObjectData { networkId = networkId, position = newPos });
                                    break;
                                }
                            }

                            // Player teleport handler
                            playerTarget = command.Length > 2 ? Plugin.GetPlayerFromString(command[1]) : localPlayerController;

                            if (playerTarget != null && !playerTarget.isPlayerDead)
                            {
                                if (Plugin.GetPositionFromCommand(command.Length > 2 ? command[2] : command[1], 3, playerTarget.playerUsername) != Vector3.zero)
                                {
                                    Plugin.mls.LogInfo("RPC SENDING: \"TPPlayerClientRpc\".");
                                    Networking.TPPlayerClientRpc(
                                        new TOT_TPPlayerData { 
                                            isInside = sendPlayerInside, 
                                            playerClientId = playerTarget.playerClientId, 
                                            position = Plugin.GetPositionFromCommand(command.Length > 2 ? command[2] : command[1], 3, playerTarget.playerUsername) });
                                }
                            }
                            else if (playerTarget != null && playerTarget.isPlayerDead)
                                Plugin.LogMessage($"Could not teleport {playerTarget.playerUsername}!\nPlayer is dead!", true);
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
                                pageText += $"@{i}{Plugin.Instance.waypoints[i].position}, ";

                            pageText = pageText.TrimEnd(',', ' ') + ".";
                            HUDManager.Instance.DisplayTip("Waypoint List", pageText);
                        }
                        else
                            Plugin.LogMessage("Waypoint List is empty!", true);
                    }
                    else if ("add".StartsWith(command[1]))
                    {
                        if (localPlayerController != null && !localPlayerController.isPlayerDead)
                        {
                            bool wpInside = localPlayerController.isInsideFactory;
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
                    else if ("door".StartsWith(command[1]))
                    {
                        Vector3 doorPosition = RoundManager.FindMainEntrancePosition(true, true);

                        if (doorPosition != Vector3.zero)
                        {
                            Plugin.Instance.waypoints.Add(new Waypoint { isInside = true, position = doorPosition });
                        Plugin.LogMessage($"Waypoint @{Plugin.Instance.waypoints.Count - 1} created at Main Entrance.");
                    }
                        else
                            Plugin.LogMessage("Unable to find Main Entrance!", true);
                    }
                    else if ("entrance".StartsWith(command[1]))
                    {
                        Vector3 entrancePosition = RoundManager.FindMainEntrancePosition(true);

                        if (entrancePosition != Vector3.zero)
                        {
                            Plugin.Instance.waypoints.Add(new Waypoint { isInside = true, position = entrancePosition });
                            Plugin.LogMessage($"Waypoint @{Plugin.Instance.waypoints.Count - 1} created inside Main Entrance.");
                        }
                        else
                            Plugin.LogMessage("Unable to find Main Entrance!", true);
                    }
                    break;
                case "he":
                case "heal":
                case "save":    // Sets player health and stamina to max, saves player if in death animation with enemy
                    if (command.Length < 2)
                        playerTarget = localPlayerController;
                    else
                    {
                        string targetUsername = string.Join(" ", command.Skip(1)).ToLower();
                        playerTarget = Plugin.GetPlayerFromString(targetUsername);
                    }

                    if (playerTarget != null)
                    {
                        if (playerTarget.isPlayerDead)
                            Plugin.LogMessage($"Attempting to revive {playerTarget.playerUsername}.");
                        else
                            Plugin.LogMessage($"Healing {playerTarget.playerUsername}.");

                        Plugin.mls.LogInfo("RPC SENDING: \"HealPlayerClientRpc\".");
                        Networking.HealPlayerClientRpc(playerTarget.playerClientId);
                    }
                    break;
                case "gm":
                case "god":
                case "godmode":
                    Plugin.Instance.enableGod = !Plugin.Instance.enableGod;
                    Plugin.LogMessage($"GodMode toggled {(Plugin.Instance.enableGod ? "on!" : "off.")}");
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
                                        objectList += "(Door), ";
                                    else if (obj.GetComponentInChildren<Turret>())
                                        objectList += "(Turret), ";
                                    else if (obj.GetComponentInChildren<Landmine>())
                                    {
                                        if (obj.GetComponentInChildren<Landmine>().hasExploded)
                                            continue;
                                        objectList += "(Landmine), ";
                                    }
                                    else if (obj.transform.parent.gameObject.GetComponentInChildren<SpikeRoofTrap>())
                                        objectList += "(Spikes), ";
                                    else
                                        objectList += "(Unknown), ";
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
                                }
                            }

                            Plugin.LogMessage($"Attempted to toggle all TerminalAccessibleObject of code {command[1]}.");
                        }
                    }
                    else
                        Plugin.LogMessage($"No TerminalAccessibleObject in this area!", true);
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
                        Plugin.LogMessage("BreakerBox not found!", true);
                    break;
                case "cr":
                case "credit":
                case "credits":
                case "money":   // View or adjust current amount of groupCredits
                    if (terminal != null)
                    {
                        if (command.Length < 2)
                            Plugin.LogMessage($"Group Credits: {terminal.groupCredits}");
                        else
                        {
                            int.TryParse(command[1], out int creditsChange);
                            Plugin.mls.LogInfo("RPC SENDING: \"TerminalCreditsClientRpc\".");
                            Networking.TerminalCreditsClientRpc(creditsChange);
                            Plugin.LogMessage($"Adjusted Credits by {creditsChange}.\nNew Total: {terminal.groupCredits}.");
                        }
                    }
                    else
                        Plugin.LogMessage("Terminal not found!", true);
                    break;
                case "ch":
                case "charge":  // Charges a players held item if it uses a battery
                    if (command.Length < 2)
                        playerTarget = localPlayerController;
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
                                Plugin.mls.LogInfo("RPC SENDING: \"ChargePlayerClientRpc\".");
                                Networking.ChargePlayerClientRpc(playerTarget.playerClientId);
                                Plugin.LogMessage($"Charging {playerTarget.playerUsername}'s item \"{foundItem.itemProperties.itemName}\".");
                            }
                            else
                                Plugin.LogMessage($"{playerTarget.playerUsername}'s item \"{foundItem.itemProperties.itemName}\" does not use a battery!", true);
                        }
                        else
                            Plugin.LogMessage($"{playerTarget.playerUsername} is not holding an item!", true);
                    }
                    else if (playerTarget.isPlayerDead)
                        Plugin.LogMessage($"Could not charge {playerTarget.playerUsername}'s item!\nPlayer is dead!", true);
                    break;
                case "ki":
                case "kill":
                    if (command.Length < 2)
                    {
                        Plugin.LogMessage($"Kill command requires a target!", true);
                        break;
                    }

                    string targetName = "";
                    bool forceDestroy = false;

                    if (command[1][command[1].Length - 1] == '*')
                    {
                        forceDestroy = true;
                        command[1] = command[1].Remove(command[1].Length - 1, 1);
                    }

                    foundId = ulong.TryParse(command[1], out networkId);

                    if (foundId && Plugin.GetGrabbableObject(networkId) != null)
                    {
                        itemTarget = Plugin.GetGrabbableObject(networkId);
                        targetName = $"{itemTarget.itemProperties.itemName} ({itemTarget.NetworkObjectId})";
                        UnityEngine.Object.Destroy(itemTarget.gameObject);
                        Plugin.LogMessage($"Killing {targetName}!");
                    }
                    else if (foundId && Plugin.GetEnemyAI(networkId) != null)
                    {
                        enemyTarget = Plugin.GetEnemyAI(networkId);
                        targetName = $"{enemyTarget.enemyType.enemyName} ({enemyTarget.NetworkObjectId})";
                        enemyTarget.HitEnemy(999999);

                        // Despawn invincible enemies
                        if (enemyTarget.GetComponentInChildren<BlobAI>() != null || 
                            enemyTarget.GetComponentInChildren<ButlerBeesEnemyAI>() != null ||
                            enemyTarget.GetComponentInChildren<DressGirlAI>() != null ||
                            enemyTarget.GetComponentInChildren<JesterAI>() != null ||
                            enemyTarget.GetComponentInChildren<LassoManAI>() != null ||
                            enemyTarget.GetComponentInChildren<SpringManAI>() != null ||
                            enemyTarget.GetComponentInChildren<DocileLocustBeesAI>() != null ||
                            enemyTarget.GetComponentInChildren<RadMechAI>() != null ||
                            enemyTarget.GetComponentInChildren<RedLocustBees>() != null ||
                            enemyTarget.GetComponentInChildren<SandWormAI>() != null || 
                            forceDestroy || (command.Length > 2 && command[2] == "*"))
                        {
                            UnityEngine.Object.Destroy(enemyTarget.gameObject);
                        }
                        Plugin.LogMessage($"Killing {targetName}!");
                    }
                    else
                    {
                        playerTarget = Plugin.GetPlayerFromString(command[1]);

                        if (playerTarget != null && !playerTarget.isPlayerDead && playerTarget.isPlayerControlled)
                        {
                            targetName = playerTarget.playerUsername;
                            Plugin.mls.LogInfo("RPC SENDING: \"HurtPlayerClientRpc\".");
                            Networking.HurtPlayerClientRpc(new TOT_DamagePlayerData { playerClientId = playerTarget.playerClientId, damage = 999999 });
                            Plugin.LogMessage($"Killing {targetName}!");
                        }
                        else if (playerTarget != null && playerTarget.isPlayerDead)
                            Plugin.LogMessage($"Unable to kill {playerTarget.playerUsername}, player already dead!", true);
                    }
                    break;
                case "gui":
                case "hud":
                    GUI.visible = !GUI.visible;
                    break;
                case "su":
                case "suit":
                    List<UnlockableItem> allSuits = StartOfRound.Instance.unlockablesList.unlockables;
                    UnlockableSuit suitManager = new UnlockableSuit();
                    if (command.Length < 2)
                    {
                        string suitList = "";
                        foreach (UnlockableItem suit in allSuits)
                        {
                            if (suit.unlockableType == 0)
                                suitList += $"{suit.unlockableName}, ";
                        }
                        suitList = suitList.TrimEnd(',', ' ') + ".";
                        HUDManager.Instance.DisplayTip("Suit List", suitList);
                    }
                    else
                    {
                        int selectedSuit = allSuits.IndexOf(allSuits.FirstOrDefault(suit => suit.unlockableType == 0 && suit.unlockableName.ToLower().StartsWith(command[1])));
                        if (selectedSuit == -1 || allSuits[selectedSuit].unlockableType != 0)
                        {
                            Plugin.LogMessage($"Unable to find suit \"{command[1]}\"!", true);
                            break;
                        }
                        playerTarget = command.Length > 2 ? Plugin.GetPlayerFromString(command[2]) : localPlayerController;
                        if (playerTarget == null) { break; }
                        Plugin.mls.LogInfo("RPC SENDING: \"SyncScrapClientRpc\".");
                        Networking.SyncSuitClientRpc(new TOT_SyncSuitData { playerId = playerTarget.playerClientId, suitId = selectedSuit });
                        Plugin.LogMessage($"Setting {playerTarget.playerUsername} to {allSuits[selectedSuit].unlockableName}.");
                    }
                    break;
                default:
                    // No command recognized, send chat normally
                    flag = false;
                    break;
            }

            if (flag)   // Empty chatTextField, this prevents anything from being sent to the in-game chat
                __instance.chatTextField.text = string.Empty;

            // Perform regular chat if player is the host and dead, this overrides the way the game blocks dead players from chatting.
            if (localPlayerController.isPlayerDead && Plugin.CheckPlayerIsHost(localPlayerController))
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
                EventSystem.current.SetSelectedGameObject(null);
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
                        pageText += $"{allItemsList[i].itemName}({i}), ";
                    else if (listName == "Enemy")
                        pageText += $"{allEnemiesList[i].enemyType.enemyName}({i}), ";
                    else if (listName == "Command")
                        pageText += $"{list[i]}\n";
                    else if (listName == "Player" && activePlayersList[i].isPlayerControlled)
                        pageText += $"Player #{activePlayersList[i].playerClientId}: {activePlayersList[i].playerUsername}\n";
                    else if (listName == "Active Item")
                    {
                        pageText += $"{activeItems[i].itemProperties.itemName} ({activeItems[i].NetworkObjectId}), ";
                        appendList = false;
                    }
                    else if (listName == "Active Enemy")
                    {
                        pageText += $"{activeEnemies[i].enemyType.enemyName} ({activeEnemies[i].NetworkObjectId}), ";
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