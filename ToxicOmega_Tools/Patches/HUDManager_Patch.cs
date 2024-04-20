﻿using GameNetcodeStuff;
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
            bool flag = true;   // Chat will not be sent if flag = true; If no command is recognized it will be set to false
            string chatMessage = __instance.chatTextField.text;

            __instance.tipsPanelCoroutine = null;   // Clears vanilla tip coroutine to prevent Plugin.LogMessage() from being blocked

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
                case string s when "help".StartsWith(s):
                    List<string> commandList = new List<string>
                    {
                        "Item: Lists spawnable items",
                        "Enemy: Lists spawnable enemies",
                        "Trap: Lists spawnable traps",
                        "Spawn: Spawns items/enemies/traps",
                        "List: Lists existing players/items/enemies",
                        "GUI: Toggles a GUI displaying nearby items/enemies",
                        "TP: Teleport players or gameobjects",
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
                case string s when "items".StartsWith(s):
                    if (command.Length > 1)
                        int.TryParse(command[1], out itemListPage);

                    itemListPage = Math.Max(itemListPage, 1);
                    FindPage(allItemsList, itemListPage, 6, "Item");
                    break;
                case string s when "enemies".StartsWith(s):
                    if (command.Length > 1)
                        int.TryParse(command[1], out enemyListPage);

                    enemyListPage = Math.Max(enemyListPage, 1);
                    FindPage(Plugin.allEnemiesList, enemyListPage, 6, "Enemy");
                    break;
                case string s when "spawn".StartsWith(s):
                    string targetString = "";
                    int spawnCountTest = 1;
                    int itemValueTest = -1;

                    if (command.Length < 2)
                        break;

                    if (command.Length > 2)
                        targetString = command[2].ToLower();

                    if (command.Length > 3)
                    {
                        int.TryParse(command[3], out spawnCountTest);
                        spawnCountTest = Math.Max(spawnCountTest, 1);
                    }

                    if (command.Length > 4)
                    {
                        if (command[4] == "$")
                        {
                            itemValueTest = -1;
                        }
                        else
                        {
                            int.TryParse(command[4], out itemValueTest);
                            itemValueTest = Math.Max(itemValueTest, 0);
                        }
                    }

                    SearchableGameObject prefabFromString = Plugin.allSpawnablesList.FirstOrDefault(obj => obj.Name.ToLower().StartsWith(command[1].Replace("_", " ")));

                    if (prefabFromString.Name == null)
                    {
                        Plugin.LogMessage($"Unable to find GameObject with name \"{command[1]}\"", true);
                        break;
                    }

                    if (!prefabFromString.IsEnemy && !prefabFromString.IsTrap)  // Spawn item
                    {
                        Plugin.SpawnItem(prefabFromString, spawnCountTest, itemValueTest, targetString);
                    }
                    else if (prefabFromString.IsEnemy)   // Spawn enemy
                    {
                        Plugin.SpawnEnemy(prefabFromString, spawnCountTest, targetString);
                    }
                    else if (prefabFromString.IsTrap)   // Spawn trap
                    {
                        Plugin.SpawnTrap(prefabFromString, spawnCountTest, targetString);
                    }
                    break;
                case string s when "trap".StartsWith(s):
                    HUDManager.Instance.DisplayTip("Trap List", "Mine, Turret, Spikes");
                    break;
                case string s when "list".StartsWith(s):
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
                case string s when "tp".StartsWith(s) || "teleport".StartsWith(s):
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
                                        new TOT_TPPlayerData
                                        {
                                            IsInside = false,
                                            PlayerClientId = localPlayerController.playerClientId,
                                            Position = destination
                                        });
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
                                Vector3 newPos = Vector3.zero;
                                foundId = ulong.TryParse(command[1], out networkId);

                                if (foundId && Plugin.GetGrabbableObject(networkId) != null)
                                {
                                    newPos = Plugin.GetPositionFromCommand(command[2], 3, Plugin.GetGrabbableObject(networkId).itemProperties.itemName);
                                }
                                else if (foundId && Plugin.GetEnemyAI(networkId) != null)
                                {
                                    newPos = Plugin.GetPositionFromCommand(command[2], 3, Plugin.GetEnemyAI(networkId).enemyType.enemyName);
                                }

                                if (foundId && newPos != Vector3.zero)
                                {
                                    Plugin.mls.LogInfo("RPC SENDING: \"TPGameObjectClientRpc\".");
                                    Networking.TPGameObjectClientRpc(new TOT_TPGameObjectData { NetworkId = networkId, Position = newPos });
                                    break;
                                }
                            }

                            // Player teleport handler
                            playerTarget = command.Length > 2 ? Plugin.GetPlayerFromString(command[1]) : localPlayerController;

                            if (playerTarget != null)
                            {
                                if (Plugin.GetPositionFromCommand(command.Length > 2 ? command[2] : command[1], 3, playerTarget.playerUsername) != Vector3.zero)
                                {
                                    Plugin.mls.LogInfo("RPC SENDING: \"TPPlayerClientRpc\".");
                                    Networking.TPPlayerClientRpc(
                                        new TOT_TPPlayerData
                                        {
                                            IsInside = sendPlayerInside,
                                            PlayerClientId = playerTarget.playerClientId,
                                            Position = Plugin.GetPositionFromCommand(command.Length > 2 ? command[2] : command[1], 3, playerTarget.playerUsername)
                                        });
                                }
                            }
                            break;
                    }
                    break;
                case string s when "wp".StartsWith(s) || "waypoints".StartsWith(s):
                    if (command.Length == 1)
                    {
                        if (Plugin.waypoints.Count > 0)
                        {
                            string pageText = "";

                            for (int i = 0; i < Plugin.waypoints.Count; i++)
                            {
                                pageText += $"@{i}{Plugin.waypoints[i].Position}, ";
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
                            Plugin.waypoints.Add(new Waypoint { IsInside = true, Position = doorPosition });
                            Plugin.LogMessage($"Waypoint @{Plugin.waypoints.Count - 1} created at Main Entrance.");
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

                        Plugin.mls.LogInfo("RPC SENDING: \"HealPlayerClientRpc\".");
                        Networking.HealPlayerClientRpc(playerTarget.playerClientId);
                    }
                    break;
                case string s when "gm".StartsWith(s) || "godmode".StartsWith(s):
                    Plugin.enableGod = !Plugin.enableGod;
                    Plugin.LogMessage($"GodMode toggled {(Plugin.enableGod ? "on!" : "off.")}");
                    break;
                case string s when "codes".StartsWith(s):
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
                                    if (obj.isBigDoor)
                                    {
                                        objectList += $"{obj.objectCode}(Door), ";
                                    }
                                    else if (obj.GetComponentInChildren<Turret>())
                                    {
                                        objectList += $"{obj.objectCode}(Turret), ";
                                    }
                                    else if (obj.GetComponentInChildren<Landmine>())
                                    {
                                        if (obj.GetComponentInChildren<Landmine>().hasExploded)
                                            continue;

                                        objectList += $"{obj.objectCode}(Landmine), ";
                                    }
                                    else if (obj.transform.parent.gameObject.GetComponentInChildren<SpikeRoofTrap>())
                                    {
                                        objectList += $"{obj.objectCode}(Spikes), ";
                                    }
                                    else
                                    {
                                        objectList += $"{obj.objectCode}(Unknown), ";
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
                            Plugin.mls.LogInfo("RPC SENDING: \"TerminalCreditsClientRpc\".");
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
                        playerTarget = Plugin.GetPlayerFromString(string.Join(" ", command.Skip(1)).ToLower());
                    }

                    if (playerTarget != null && !playerTarget.isPlayerDead)
                    {
                        itemTarget = playerTarget.ItemSlots[playerTarget.currentItemSlot];

                        if (itemTarget != null)
                        {
                            if (itemTarget.itemProperties.requiresBattery)
                            {
                                Plugin.mls.LogInfo("RPC SENDING: \"ChargePlayerClientRpc\".");
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
                    else if (playerTarget.isPlayerDead)
                    {
                        Plugin.LogMessage($"Could not charge {playerTarget.playerUsername}'s item!\nPlayer is dead!", true);
                    }
                    break;
                case string s when "kill".StartsWith(s):
                    bool forceDestroy = false;

                    if (command.Length < 2)
                    {
                        Plugin.LogMessage($"Kill command requires a target!", true);
                        break;
                    }

                    if (command[1][command[1].Length - 1] == '*')
                    {
                        forceDestroy = true;
                        command[1] = command[1].Remove(command[1].Length - 1, 1);
                    }

                    foundId = ulong.TryParse(command[1], out networkId);

                    if (foundId && Plugin.GetGrabbableObject(networkId) != null)
                    {
                        itemTarget = Plugin.GetGrabbableObject(networkId);
                        UnityEngine.Object.Destroy(itemTarget.gameObject);
                        Plugin.LogMessage($"Killing {itemTarget.itemProperties.itemName} ({itemTarget.NetworkObjectId})!");
                    }
                    else if (foundId && Plugin.GetEnemyAI(networkId) != null)
                    {
                        enemyTarget = Plugin.GetEnemyAI(networkId);
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
                            forceDestroy || (command.Length > 2 && command[2] == "*"))
                        {
                            UnityEngine.Object.Destroy(enemyTarget.gameObject);
                        }
                        Plugin.LogMessage($"Killing {enemyTarget.enemyType.enemyName} ({enemyTarget.NetworkObjectId})!");
                    }
                    else
                    {
                        playerTarget = Plugin.GetPlayerFromString(command[1]);

                        if (playerTarget != null && !playerTarget.isPlayerDead && playerTarget.isPlayerControlled)
                        {
                            Plugin.mls.LogInfo("RPC SENDING: \"HurtPlayerClientRpc\".");
                            Networking.HurtPlayerClientRpc(new TOT_DamagePlayerData { PlayerClientId = playerTarget.playerClientId, Damage = 999999 });
                            Plugin.LogMessage($"Killing {playerTarget.playerUsername}!");
                        }
                        else if (playerTarget != null && playerTarget.isPlayerDead)
                        {
                            Plugin.LogMessage($"Unable to kill {playerTarget.playerUsername}, player already dead!", true);
                        }
                    }
                    break;
                case string s when "gui".StartsWith(s) || "hud".StartsWith(s):
                    GUI.visible = !GUI.visible;
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

                        if (selectedSuit == -1 || allSuits[selectedSuit].unlockableType != 0)
                        {
                            Plugin.LogMessage($"Unable to find suit \"{command[1]}\"!", true);
                            break;
                        }

                        playerTarget = command.Length > 2 ? Plugin.GetPlayerFromString(command[2]) : localPlayerController;
                        if (playerTarget == null) { break; }
                        Plugin.mls.LogInfo("RPC SENDING: \"SyncScrapClientRpc\".");
                        Networking.SyncSuitClientRpc(new TOT_SyncSuitData { PlayerId = playerTarget.playerClientId, SuitId = selectedSuit });
                        Plugin.LogMessage($"Setting {playerTarget.playerUsername} to {allSuits[selectedSuit].unlockableName}.");
                    }
                    break;
                default:
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
                    else if (listName == "Player" && activePlayersList[i].isPlayerControlled)
                    {
                        pageText += $"{(activePlayersList[i].isPlayerDead ? "Dead: " : "")}{activePlayersList[i].playerUsername} (#{activePlayersList[i].playerClientId}{(Plugin.CheckPlayerIsHost(activePlayersList[i]) ? " - HOST" : "")})\n";
                    }
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