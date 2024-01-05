using BepInEx.Logging;
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using GameNetcodeStuff;
using Unity.Netcode;
using System;
using ToxicOmega_Tools.Patches;
using LC_API.Networking;

namespace ToxicOmega_Tools
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "com.toxicomega.toxicomega_tools";
        private const string modName = "ToxicOmega Tools";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        private static Plugin Instance;
        internal static ManualLogSource mls;
        public static PlayerControllerB localPlayerController;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo("ToxicOmega Tools mod has awoken.");
            harmony.PatchAll();
            Network.RegisterAll();
        }

        public static PlayerControllerB FindPlayerFromString(string searchString)
        {
            // Use string to find playername
            PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;

            // Search player by ID# if string starts with "#"
            if (searchString == ("#") && searchString.Length > 1)
            {
                string clientIdString = searchString.Substring(1);

                if (ulong.TryParse(clientIdString, out ulong clientId))
                {
                    PlayerControllerB foundPlayer = allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(clientId));
                    if (foundPlayer != null)
                    {
                        return foundPlayer;
                    }
                    else
                    {
                        LogMessage($"No Player with ID {clientId}!", true);
                        return null;
                    }
                }
                else
                {
                    LogMessage($"Player ID # {clientIdString} is invalid!", true);
                    return null;
                }
            }
            else
            {
                // Returns a player who's name starts with the input string
                PlayerControllerB foundPlayer = allPlayerScripts.FirstOrDefault(player => player.playerUsername.ToLower().StartsWith(searchString.ToLower()));
                if (foundPlayer != null)
                {
                    return foundPlayer;
                }
                else
                {
                    LogMessage($"Player {searchString} not found!", true);
                    return null;
                }
            }
        }

        public static void LogMessage(string message, bool isError = false)
        {
            string headerText = string.Empty;
            if (isError)
            {
                headerText = "Error!";
                mls.LogError(message);
            }
            else
            {
                headerText = "Success!";
                mls.LogInfo(message);
            }
            HUDManager.Instance.DisplayTip(headerText, message, isError);
        }

        public static void PlayerTeleportEffects(PlayerControllerB player, bool isInside)
        {
            // Ensures player fog/lighting is consistent after a teleport
            SavePlayer(player);
            player.isInElevator = !isInside;
            player.isInHangarShipRoom = !isInside;
            player.isInsideFactory = isInside;
            player.averageVelocity = 0.0f;
            player.velocityLastFrame = Vector3.zero;
            player.beamUpParticle.Play();
            player.beamOutBuildupParticle.Play();
        }
        
        public static void SavePlayer(PlayerControllerB player)
        {
            // Knocks any Centipedes off players head
            CentipedeAI[] centipedes = FindObjectsByType<CentipedeAI>(FindObjectsSortMode.None);
            for (int i = 0; i < centipedes.Length; i++)
            {
                if (centipedes[i].clingingToPlayer == player)
                {
                    centipedes[i].HitEnemy(1, player, true);
                }
            }

            // Makes forest giant drop player and stuns them
            ForestGiantAI[] giants = FindObjectsByType<ForestGiantAI>(FindObjectsSortMode.None);
            for (int i = 0; i < giants.Length; i++)
            {
                if (giants[i].inSpecialAnimationWithPlayer == player)
                {
                    giants[i].GetComponentInChildren<EnemyAI>().SetEnemyStunned(true, 7.5f, player);
                }
            }
        }   // Eventually make this save you from the masked

        public static void SpawnEnemy(int enemyID, int amount, bool onPlayer, PlayerControllerB playerTarget, bool inside)
        {
            localPlayerController = StartOfRound.Instance.localPlayerController;
            RoundManager currentRound = RoundManager.Instance;

            if (playerTarget == null || !playerTarget.isPlayerControlled || playerTarget.inTerminalMenu || (localPlayerController.IsServer && !localPlayerController.isHostPlayerObject))
            {
                return;
            }

            Vector3 position = playerTarget.transform.position;

            // Targets spectated player if playerTarget is dead and also is the localPlayerController
            if (localPlayerController.isPlayerDead && localPlayerController.playerClientId == playerTarget.playerClientId)
            {
                if (localPlayerController.spectatedPlayerScript != null)
                {
                    position = localPlayerController.spectatedPlayerScript.transform.position;
                }
                else return;
            }
            else if (playerTarget.isPlayerDead)
            {
                return;
            }

            // Uses different enemy list depending on what creature the player is trying to spawn
            if (inside)
            {
                LogMessage($"Spawning Inside - Name: {currentRound.currentLevel.Enemies[enemyID].enemyType.enemyName}, ID: {enemyID}, Amount: {amount}, Location: {(onPlayer ? playerTarget.playerUsername : "Natural")}.");

                try
                {
                    for (int i = 0; i < amount; i++)
                    {
                        // Spawns the enemy at a random vent if no player destination is set
                        if (!onPlayer)
                        {
                            position = currentRound.allEnemyVents[UnityEngine.Random.Range(0, currentRound.allEnemyVents.Length)].floorNode.position;
                        }
                        //currentRound.SpawnEnemyOnServer(position, currentRound.allEnemyVents[i].floorNode.eulerAngles.y, enemyID);    // This doesn't work when spawning more than 5 enemies at a time
                        Instantiate(currentRound.currentLevel.Enemies[enemyID].enemyType.enemyPrefab, position, Quaternion.Euler(Vector3.zero)).gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Unable to Spawn Inside Enemy ID: {enemyID}", true);
                    mls.LogError(ex);
                }
            }
            else
            {
                LogMessage($"Spawning Outside - Name: {currentRound.currentLevel.OutsideEnemies[enemyID].enemyType.enemyName}, ID: {enemyID}, Amount: {amount}, Location: {(onPlayer ? playerTarget.playerUsername : "Natural")}.");

                try
                {
                    for (int i = 0; i < amount; i++)
                    {
                        // Spawns the enemy at a random OutsideAINode if no player destination is set
                        if (!onPlayer)
                        {
                            position = GameObject.FindGameObjectsWithTag("OutsideAINode")[UnityEngine.Random.Range(0, GameObject.FindGameObjectsWithTag("OutsideAINode").Length - 1)].transform.position;
                        }
                        Instantiate(currentRound.currentLevel.OutsideEnemies[enemyID].enemyType.enemyPrefab, position, Quaternion.Euler(Vector3.zero)).gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Unable to Spawn Outside Enemy ID: {enemyID}", true);
                    mls.LogError(ex);
                }
            }
        }
        
        public static void SpawnItem(int itemID, int amount, int value, PlayerControllerB playerTarget)
        {
            localPlayerController = StartOfRound.Instance.localPlayerController;
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;

            if (playerTarget == null || !playerTarget.isPlayerControlled || playerTarget.inTerminalMenu || (localPlayerController.IsServer && !localPlayerController.isHostPlayerObject))
            {
                return;
            }

            string logValue = value >= 0 ? $"{value}" : "Random";
            LogMessage($"Spawning - Name: {allItemsList[itemID].name}, ID: {itemID}, Amount: {amount}, Location: {playerTarget.playerUsername}, Value: {logValue}.");

            Vector3 position = playerTarget.transform.position;

            // Targets spectated player if playerTarget is dead and also is the localPlayerController
            if (localPlayerController.isPlayerDead && localPlayerController.playerClientId == playerTarget.playerClientId)
            {
                position = (localPlayerController.spectatedPlayerScript).transform.position;
            }
            else if (playerTarget.isPlayerDead)
            {
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                try
                {
                    // The Shotgun (and maybe other items I haven't noticed) have their max and min values backwards causing an error unless I flip them... c'mon Zeekers...
                    if (allItemsList[itemID].minValue > allItemsList[itemID].maxValue)
                    {
                        int temp = allItemsList[itemID].minValue;
                        allItemsList[itemID].minValue = allItemsList[itemID].maxValue;
                        allItemsList[itemID].maxValue = temp;
                    }

                    // Spawns item using LC API
                    LC_API.GameInterfaceAPI.Features.Item item = LC_API.GameInterfaceAPI.Features.Item.CreateAndSpawnItem(allItemsList[itemID].itemName, true, position);

                    // RPC to set Shotgun shells loaded to be two for all players
                    if (itemID == 59)
                    {
                        mls.LogInfo("RPC SENDING: \"TOT_SYNC_AMMO\".");
                        Network.Broadcast("TOT_SYNC_AMMO", new TOT_ITEM_Broadcast { networkObjectID = item.NetworkObjectId });
                        mls.LogInfo("RPC END: \"TOT_SYNC_AMMO\".");
                    }

                    // Overrides default scrap value if a new value is passed as an argument
                    if (item != null && value != -1)
                    {
                        item.ScrapValue = value;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Unable to Spawn Item ID: {itemID}", true);
                    mls.LogError(ex);
                }
            }
        }
    }
}