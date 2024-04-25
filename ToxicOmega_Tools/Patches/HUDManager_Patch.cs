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
    internal class HUDManager_Patch : MonoBehaviour
    {
        public static bool sendPlayerInside = true;
        private static int itemListPage;
        private static int enemyListPage;
        private static bool foundId;
        private static ulong networkId;
        private static GrabbableObject itemTarget;
        private static EnemyAI enemyTarget;
        private static PlayerControllerB playerTarget;

        [HarmonyPatch(nameof(HUDManager.EnableChat_performed))]
        [HarmonyPrefix]
        private static bool EnableChatAction(HUDManager __instance) // Allow host to open the in-game chat while dead
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
            else
            {
                return true;
            }
        }

        [HarmonyPatch(nameof(HUDManager.SubmitChat_performed))]
        [HarmonyPrefix]
        private static bool RegisterChatCommand(HUDManager __instance)
        {
            RoundManager currentRound = RoundManager.Instance;
            Terminal terminal = FindObjectOfType<Terminal>();
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;
            PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
            bool flag = true;   // Chat will not be sent if flag = true; If no command is recognized it will be set to false
            string chatMessage = __instance.chatTextField.text;
            __instance.tipsPanelCoroutine = null;   // Clears vanilla tip coroutine to prevent Plugin.LogMessage() from being blocked
            if (chatMessage == null || chatMessage == "")
                return true;
            if (!Plugin.CheckPlayerIsHost(localPlayerController) && !(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
                return true;

            // Split chat message up by spaces, trim trailing spaces, convert to lowercase
            string[] command = chatMessage
                .Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.TrimEnd().ToLowerInvariant())
                .ToArray();

            switch (command[0].Replace("/", "").ToLower())
            {
                case "help":
                    List<string> commandList = new List<string>
                    {
                        "Item: Lists spawnable items",
                        "Enemy: Lists spawnable enemies",
                        "Trap: Lists spawnable traps",
                        "Spawn: Spawns items/enemies/traps",
                        "Give: Adds an item to a players inventory",
                        "List: Lists existing players/items/enemies",
                        "GUI: Toggles a GUI displaying nearby items/enemies",
                        "TP: Teleport players or gameobjects",
                        "WP: Creates waypoints",
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
                        _ = int.TryParse(command[1], out helpPage);
                    helpPage = Math.Max(helpPage, 1);
                    FindPage(commandList, helpPage, 4, "Command");
                    break;
                case string s when "items".StartsWith(s):
                    if (command.Length > 1)
                        _ = int.TryParse(command[1], out itemListPage);
                    itemListPage = Math.Max(itemListPage, 1);
                    FindPage(allItemsList, itemListPage, 10, "Item");
                    break;
                case string s when "enemies".StartsWith(s):
                    if (command.Length > 1)
                        _ = int.TryParse(command[1], out enemyListPage);
                    enemyListPage = Math.Max(enemyListPage, 1);
                    FindPage(Plugin.allEnemiesList, enemyListPage, 10, "Enemy");
                    break;
                case string s when "spawn".StartsWith(s):
                    string targetString = "";
                    int amount = 1;
                    int itemValue = -1;

                    if (command.Length < 2)
                        break;
                    if (command.Length > 2)
                        targetString = command[2];
                    if (command.Length > 3)
                        _ = int.TryParse(command[3], out amount);
                    if (command.Length > 4)
                    {
                        if (command[4] == "$")
                        {
                            itemValue = -1;
                        }
                        else
                        {
                            _ = int.TryParse(command[4], out itemValue);
                        }
                    }

                    SearchableGameObject prefabFromString = Plugin.allSpawnablesList.FirstOrDefault(obj => obj.Name.ToLower().StartsWith(command[1].Replace("_", " ")));
                    if (prefabFromString.Name != null)
                    {
                        if (prefabFromString.IsItem)  // Spawn item
                        {
                            Plugin.SpawnItem(prefabFromString, Math.Max(amount, 1), Math.Max(itemValue, 0), targetString);
                        }
                        else if (prefabFromString.IsEnemy)   // Spawn enemy
                        {
                            Plugin.SpawnEnemy(prefabFromString, Math.Max(amount, 1), targetString);
                        }
                        else if (prefabFromString.IsTrap)   // Spawn trap
                        {
                            Plugin.SpawnTrap(prefabFromString, Math.Max(amount, 1), targetString);
                        }
                    }
                    else
                    {
                        Plugin.LogMessage($"Unable to find GameObject with name \"{command[1]}\"", true);
                    }
                    break;
                case string s when "give".StartsWith(s):
                    if (command.Length > 1)
                    {
                        Item itemType = StartOfRound.Instance.allItemsList.itemsList.FirstOrDefault(x => x.itemName.ToLower().StartsWith(command[1].Replace("_", " ")));
                        if (itemType != null)
                        {
                            playerTarget = command.Length > 2 ? Plugin.GetPlayerFromString(string.Join(" ", command.Skip(2))) : localPlayerController;
                            if (playerTarget == null || playerTarget.isPlayerDead)
                                break;
                            GameObject spawnedItem = Instantiate(itemType.spawnPrefab, playerTarget.transform.position, Quaternion.identity);
                            if (spawnedItem == null)
                                break;
                            spawnedItem.GetComponent<GrabbableObject>().fallTime = 0f;
                            spawnedItem.GetComponent<NetworkObject>().Spawn();
                            if (itemType.minValue > itemType.maxValue)
                                (itemType.maxValue, itemType.minValue) = (itemType.minValue, itemType.maxValue);
                            if (itemType.itemName == "Shotgun")
                            {
                                Networking.SyncAmmoClientRpc(spawnedItem.GetComponent<GrabbableObject>().NetworkObject);
                            }
                            Networking.SyncScrapValueClientRpc(spawnedItem.GetComponent<GrabbableObject>().NetworkObject, (int)(double)(UnityEngine.Random.Range(itemType.minValue, itemType.maxValue) * RoundManager.Instance.scrapValueMultiplier));
                            Networking.GiveItemClientRpc(playerTarget.playerClientId, spawnedItem.GetComponent<GrabbableObject>().NetworkObject);
                            Plugin.LogMessage($"Giving {itemType.itemName} to {playerTarget.playerUsername}.");
                        }
                        else
                        {
                            Plugin.LogMessage($"Unable to find GameObject with name \"{command[1]}\"", true);
                        }
                    }
                    break;
                case string s when "trap".StartsWith(s):
                    HUDManager.Instance.DisplayTip("Trap List", "Mine, Turret, Spikes");
                    break;
                case string s when "list".StartsWith(s):
                    if (command.Length < 2)
                    {
                        CustomGUI.fullListVisible = !CustomGUI.fullListVisible;
                        CustomGUI.nearbyVisible = false;

                        if (CustomGUI.fullListVisible)
                        {
                            localPlayerController.StartCoroutine(PlayerControllerB_Patch.UpdateGUI());
                        }
                        else
                        {
                            localPlayerController.StopCoroutine(PlayerControllerB_Patch.UpdateGUI());
                        }
                        Plugin.LogMessage($"{(CustomGUI.fullListVisible ? "Enabling" : "Disabling")} full list GUI.");
                    }
                    else
                    {
                        int listPage = 1;
                        if (command.Length > 2)
                            _ = int.TryParse(command[2], out listPage);
                        listPage = Math.Max(listPage, 1);

                        if ("players".StartsWith(command[1]))
                        {
                            FindPage(StartOfRound.Instance.allPlayerScripts.ToList(), listPage, 4, "Player");
                        }
                        else if ("items".StartsWith(command[1]))
                        {
                            FindPage(FindObjectsOfType<GrabbableObject>().ToList(), listPage, 6, "Active Items");
                        }
                        else if ("enemy".StartsWith(command[1]) || "enemies".StartsWith(command[1]))
                        {
                            FindPage(FindObjectsOfType<EnemyAI>().ToList(), listPage, 6, "Active Enemies");
                        }
                        else if ("codes".StartsWith(command[1]))
                        {
                            FindPage(FindObjectsOfType<TerminalAccessibleObject>().ToList(), listPage, 10, "Terminal Codes");
                        }
                        else if ("waypoints".StartsWith(command[1]))
                        {
                            FindPage(Plugin.waypoints, listPage, 8, "Waypoint");
                        }
                        else
                        {
                            Plugin.LogMessage($"Unable to find list by name {command[1]}!", true);
                        }
                    }
                    break;
                case string s when "tp".StartsWith(s) || "teleport".StartsWith(s):
                    switch (command.Length)
                    {
                        case 1:
                            if (Plugin.GetPositionFromCommand("!", 3, localPlayerController.playerUsername) != Vector3.zero)
                            {
                                if (!localPlayerController.isPlayerDead)
                                {
                                    Vector3 destination = Plugin.GetPositionFromCommand("!", 3, localPlayerController.playerUsername);
                                    Networking.TPPlayerClientRpc(localPlayerController.playerClientId, destination, false);
                                }
                                else
                                {
                                    Plugin.LogMessage($"Could not teleport {localPlayerController.playerUsername}!\nPlayer is dead!", true);
                                }
                            }
                            break;
                        case 2:
                        case 3:
                            // Look for item/enemy by ID and break the switch function if one is found
                            if (command.Length > 2)
                            {
                                string tpTargetString = null;
                                NetworkObjectReference networkObjectRef = new NetworkObjectReference();
                                foundId = ulong.TryParse(command[1], out networkId);
                                enemyTarget = Networking.GetEnemyByNetId(networkId);
                                itemTarget = Networking.GetItemByNetId(networkId);

                                if (foundId && enemyTarget != null)
                                {
                                    networkObjectRef = enemyTarget.NetworkObject;
                                    tpTargetString = enemyTarget.enemyType.enemyName;
                                }
                                else if (foundId && itemTarget != null)
                                {
                                    networkObjectRef = itemTarget.NetworkObject;
                                    tpTargetString = itemTarget.itemProperties.itemName;
                                }

                                if (foundId && tpTargetString != null)
                                {
                                    Networking.TPGameObjectClientRpc(networkObjectRef, Plugin.GetPositionFromCommand(command[2], 3, tpTargetString));
                                    break;
                                }
                            }

                            // Player teleport handler
                            playerTarget = command.Length > 2 ? Plugin.GetPlayerFromString(command[1]) : localPlayerController;
                            if (playerTarget != null)
                            {
                                Vector3 position = Plugin.GetPositionFromCommand(command.Length > 2 ? command[2] : command[1], 3, playerTarget.playerUsername);
                                if (position != Vector3.zero)
                                    Networking.TPPlayerClientRpc(playerTarget.playerClientId, position, sendPlayerInside);
                            }
                            break;
                    }
                    break;
                case string s when "wp".StartsWith(s) || "waypoints".StartsWith(s):
                    if (command.Length == 1)
                    {
                        if (Plugin.waypoints.Count > 0)
                        {
                            FindPage(Plugin.waypoints, 1, 8, "Waypoint");
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
                            bool wpInside = localPlayerController.isInsideFactory;
                            Vector3 wpPosition = localPlayerController.transform.position;
                            Plugin.waypoints.Add(new Waypoint { IsInside = wpInside, Position = wpPosition });
                            Plugin.LogMessage($"Waypoint @{Plugin.waypoints.Count - 1} created at {wpPosition}.");
                        }
                    }
                    else if ("clear".StartsWith(command[1]))
                    {
                        Plugin.waypoints.Clear();
                        Plugin.LogMessage($"Waypoints cleared.");
                    }
                    else if ("door".StartsWith(command[1]))
                    {
                        Vector3 doorPosition = RoundManager.FindMainEntrancePosition(true, true);
                        if (doorPosition != Vector3.zero)
                        {
                            Plugin.waypoints.Add(new Waypoint { IsInside = false, Position = doorPosition });
                            Plugin.LogMessage($"Waypoint @{Plugin.waypoints.Count - 1} created at Front Door.");
                        }
                        else
                        {
                            Plugin.LogMessage("Unable to find Main Entrance!", true);
                        }
                    }
                    else if ("entrance".StartsWith(command[1]))
                    {
                        Vector3 entrancePosition = RoundManager.FindMainEntrancePosition(true);
                        if (entrancePosition != Vector3.zero)
                        {
                            Plugin.waypoints.Add(new Waypoint { IsInside = true, Position = entrancePosition });
                            Plugin.LogMessage($"Waypoint @{Plugin.waypoints.Count - 1} created inside Main Entrance.");
                        }
                        else
                        {
                            Plugin.LogMessage("Unable to find Main Entrance!", true);
                        }
                    }
                    break;
                case string s when "heal".StartsWith(s) || "save".StartsWith(s):
                    if (command.Length < 2)
                    {
                        playerTarget = localPlayerController;
                    }
                    else
                    {
                        playerTarget = Plugin.GetPlayerFromString(string.Join(" ", command.Skip(1)));
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
                        Networking.HealPlayerClientRpc(playerTarget.playerClientId);
                    }
                    break;
                case string s when "gm".StartsWith(s) || "godmode".StartsWith(s):
                    Plugin.enableGod = !Plugin.enableGod;
                    Plugin.LogMessage($"GodMode toggled {(Plugin.enableGod ? "on!" : "off.")}");
                    break;
                case string s when "codes".StartsWith(s):
                    List<TerminalAccessibleObject> terminalObjects = FindObjectsOfType<TerminalAccessibleObject>().ToList();
                    if (terminalObjects.Count > 0)
                    {
                        if (command.Length < 2)
                        {
                            FindPage(terminalObjects, 1, 10, "Terminal Codes");
                        }
                        else
                        {
                            foreach (TerminalAccessibleObject obj in terminalObjects)
                            {
                                if (obj != null && obj.objectCode == command[1])
                                    obj.CallFunctionFromTerminal();
                            }
                            Plugin.LogMessage($"Attempted to toggle all TerminalAccessibleObject of code {command[1]}.");
                        }
                    }
                    else
                    {
                        Plugin.LogMessage($"No TerminalAccessibleObject in this area!", true);
                    }
                    break;
                case string s when "breaker".StartsWith(s):
                    BreakerBox breaker = FindObjectOfType<BreakerBox>();
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
                case string s when "credits".StartsWith(s) || "money".StartsWith(s):
                    if (terminal != null)
                    {
                        if (command.Length < 2)
                        {
                            Plugin.LogMessage($"Group Credits: {terminal.groupCredits}");
                        }
                        else
                        {
                            int.TryParse(command[1], out int creditsChange);
                            Networking.TerminalCreditsClientRpc(creditsChange);
                            Plugin.LogMessage($"Adjusted Credits by {creditsChange}.\nNew Total: {terminal.groupCredits}.");
                        }
                    }
                    else
                    {
                        Plugin.LogMessage("Terminal not found!", true);
                    }
                    break;
                case string s when "charge".StartsWith(s):
                    if (command.Length < 2)
                    {
                        playerTarget = localPlayerController;
                    }
                    else
                    {
                        playerTarget = Plugin.GetPlayerFromString(string.Join(" ", command.Skip(1)));
                    }

                    if (playerTarget != null && !playerTarget.isPlayerDead)
                    {
                        itemTarget = playerTarget.ItemSlots[playerTarget.currentItemSlot];
                        if (itemTarget != null)
                        {
                            if (itemTarget.itemProperties.requiresBattery)
                            {
                                Networking.ChargePlayerClientRpc(playerTarget.playerClientId);
                                Plugin.LogMessage($"Charging {playerTarget.playerUsername}'s item \"{itemTarget.itemProperties.itemName}\".");
                            }
                            else
                            {
                                Plugin.LogMessage($"{playerTarget.playerUsername}'s item \"{itemTarget.itemProperties.itemName}\" does not use a battery!", true);
                            }
                        }
                        else
                        {
                            Plugin.LogMessage($"{playerTarget.playerUsername} is not holding an item!", true);
                        }
                    }
                    else if (playerTarget != null && playerTarget.isPlayerDead)
                    {
                        Plugin.LogMessage($"Could not charge {playerTarget.playerUsername}'s item!\nPlayer is dead!", true);
                    }
                    break;
                case string s when "kill".StartsWith(s):
                    bool forceDestroy = false;
                    int endIndex;
                    int counter = 0;

                    if (command.Length < 2)
                    {
                        Plugin.LogMessage($"Kill command requires a target!", true);
                        break;
                    }

                    string tempString = string.Join("", command.Skip(1));
                    if (tempString[tempString.Length - 1] == '*')
                    {
                        forceDestroy = true;
                        tempString = tempString.Remove(tempString.Length - 1, 1);
                    }
                    string[] processedStrings = tempString.Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                    foundId = ulong.TryParse(processedStrings[0], out networkId);
                    if (foundId)
                    {
                        if (processedStrings.Length < 2 || !int.TryParse(processedStrings[1], out endIndex))
                            endIndex = (int)networkId;
                        endIndex = Math.Max((int)networkId, endIndex);

                        for (int i = (int)networkId; i <= endIndex; i++)
                        {
                            enemyTarget = Networking.GetEnemyByNetId((ulong)i);
                            itemTarget = Networking.GetItemByNetId((ulong)i);

                            if (enemyTarget != null)
                            {
                                counter++;
                                enemyTarget.HitEnemy(999999);
                                // Force destroy invincible enemies
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
                                    forceDestroy)
                                {
                                    Destroy(enemyTarget.gameObject);
                                }
                                if ((int)networkId == endIndex)
                                    Plugin.LogMessage($"Killed {enemyTarget.enemyType.enemyName} ({enemyTarget.NetworkObjectId})!");
                            }
                            else if (itemTarget != null)
                            {
                                counter++;
                                Destroy(itemTarget.gameObject);
                                if ((int)networkId == endIndex)
                                    Plugin.LogMessage($"Killed {itemTarget.itemProperties.itemName} ({itemTarget.NetworkObjectId})!");
                            }
                        }
                        if ((int)networkId != endIndex)
                            Plugin.LogMessage($"Killed {counter} GameObjects!");
                    }
                    else
                    {
                        playerTarget = Plugin.GetPlayerFromString(string.Join(" ", command.Skip(1)));
                        if (playerTarget != null && !playerTarget.isPlayerDead && playerTarget.isPlayerControlled)
                        {
                            Networking.HurtPlayerClientRpc(playerTarget.playerClientId, 999999);
                            Plugin.LogMessage($"Killing {playerTarget.playerUsername}!");
                        }
                        else if (playerTarget != null && playerTarget.isPlayerDead)
                        {
                            Plugin.LogMessage($"Unable to kill {playerTarget.playerUsername}, player already dead!", true);
                        }
                    }
                    break;
                case string s when "gui".StartsWith(s) || "hud".StartsWith(s):
                    CustomGUI.nearbyVisible = !CustomGUI.nearbyVisible;
                    CustomGUI.fullListVisible = false;

                    if (CustomGUI.nearbyVisible)
                    {
                        localPlayerController.StartCoroutine(PlayerControllerB_Patch.UpdateGUI());
                    }
                    else
                    {
                        localPlayerController.StopCoroutine(PlayerControllerB_Patch.UpdateGUI());
                    }
                    Plugin.LogMessage($"{(CustomGUI.nearbyVisible ? "Enabling" : "Disabling")} GUI.");
                    break;
                case string s when "suit".StartsWith(s):
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
                        if (selectedSuit != -1 && allSuits[selectedSuit].unlockableType == 0)
                        {
                            playerTarget = command.Length > 2 ? Plugin.GetPlayerFromString(string.Join(" ", command.Skip(2))) : localPlayerController;
                            if (playerTarget == null)
                                break;
                            Networking.SyncSuitClientRpc(playerTarget.playerClientId, selectedSuit);
                            Plugin.LogMessage($"Setting {playerTarget.playerUsername} to {allSuits[selectedSuit].unlockableName}.");
                        }
                        else
                        {
                            Plugin.LogMessage($"Unable to find suit \"{command[1]}\"!", true);
                        }
                    }
                    break;
                default:
                    flag = false;
                    break;
            }

            if (flag)   // Empty chatTextField, this prevents anything from being sent to the in-game chat
                __instance.chatTextField.text = string.Empty;

            // Perform chat even if player is dead, this overrides the way the game blocks dead players from chatting.
            if (localPlayerController.isPlayerDead)
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
            else
            {
                return true;
            }
        }

        private static void FindPage<T>(List<T> list, int page, int itemsPerPage, string listName)
        {
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;
            List<PlayerControllerB> activePlayersList = StartOfRound.Instance.allPlayerScripts.ToList();
            List<GrabbableObject> activeItems = FindObjectsOfType<GrabbableObject>().ToList();
            List<EnemyAI> activeEnemies = FindObjectsOfType<EnemyAI>().ToList();
            List<TerminalAccessibleObject> terminalObjects = FindObjectsOfType<TerminalAccessibleObject>().ToList();
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
                        pageText += $"{allItemsList[i].itemName}, ";
                    }
                    else if (listName == "Enemy")
                    {
                        pageText += $"{Plugin.allEnemiesList[i].enemyType.enemyName}, ";
                    }
                    else if (listName == "Command")
                    {
                        pageText += $"{list[i]}\n";
                    }
                    else if (listName == "Player" && (activePlayersList[i].isPlayerControlled || activePlayersList[i].isPlayerDead))
                    {
                        pageText += $"{(activePlayersList[i].isPlayerDead ? "Dead: " : "")}{activePlayersList[i].playerUsername} (#{activePlayersList[i].playerClientId}{(Plugin.CheckPlayerIsHost(activePlayersList[i]) ? " - HOST" : "")})\n";
                    }
                    else if (listName == "Active Items")
                    {
                        pageText += $"{activeItems[i].itemProperties.itemName} ({activeItems[i].NetworkObjectId}), ";
                        appendList = false;
                    }
                    else if (listName == "Active Enemies")
                    {
                        pageText += $"{activeEnemies[i].enemyType.enemyName} ({activeEnemies[i].NetworkObjectId}), ";
                        appendList = false;
                    }
                    else if (listName == "Terminal Codes")
                    {
                        if (terminalObjects[i].objectCode != null)
                        {
                            if (terminalObjects[i].isBigDoor)
                            {
                                pageText += $"{terminalObjects[i].objectCode}(Door), ";
                            }
                            else if (terminalObjects[i].GetComponentInChildren<Landmine>())
                            {
                                if (terminalObjects[i].GetComponentInChildren<Landmine>().hasExploded)
                                    continue;

                                pageText += $"{terminalObjects[i].objectCode}(Mine), ";
                            }
                            else if (terminalObjects[i].GetComponentInChildren<Turret>())
                            {
                                pageText += $"{terminalObjects[i].objectCode}(Turret), ";
                            }
                            else if (terminalObjects[i].transform.parent.gameObject.GetComponentInChildren<SpikeRoofTrap>())
                            {
                                pageText += $"{terminalObjects[i].objectCode}(Spikes), ";
                            }
                            else
                            {
                                pageText += $"{terminalObjects[i].objectCode}(Unknown), ";
                            }
                        }
                        appendList = false;
                    }
                    else if (listName == "Waypoint")
                    {
                        if (Plugin.waypoints[i].Position == RoundManager.FindMainEntrancePosition(true, true))
                        {
                            pageText += $"@{i}(Door), ";
                        }
                        else if (Plugin.waypoints[i].Position == RoundManager.FindMainEntrancePosition(true))
                        {
                            pageText += $"@{i}(Entrance), ";
                        }
                        else
                        {
                            pageText += $"@{i}({Math.Floor(Plugin.waypoints[i].Position.x)}, {Math.Floor(Plugin.waypoints[i].Position.z)}), ";
                        }
                    }
                }

                pageText = pageText.TrimEnd(',', ' ', '\n') + (listName == "Player" ? "" : ".");
                string pageHeader = $"{listName}{(appendList ? " List" : "")} ({page} of {maxPages})";
                HUDManager.Instance.DisplayTip(pageHeader, pageText);
            }
        }
    }
}