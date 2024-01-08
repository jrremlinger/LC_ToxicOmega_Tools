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
using static UnityEngine.InputSystem.InputRemoting;
using LC_API.GameInterfaceAPI.Features;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        public static List<Waypoint> waypoints = new List<Waypoint>();

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
        
        public static bool CheckPlayerIsHost(PlayerControllerB player)
        {
            return Player.HostPlayer.ClientId == player.playerClientId;
        }

        public static PlayerControllerB GetPlayerFromString(string searchString)
        {
            // Use string to find playername
            PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;

            // Search player by ID# if string starts with "#"
            if (searchString.StartsWith("#") && searchString.Length > 1)
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
        
        public static Vector3 GetPositionFromCommand(string input, int positionType, PlayerControllerB playerToTP = null)
        {
            // Position types if player name is not given:
            // 0: Random RandomScrapSpawn[]
            // 1: Random outsideAINodes[]
            // 2: Random allEnemyVents[]
            // 3: Teleport Destination

            bool isPlayerTarget = false;
            bool isTP = false;
            Vector3 position = Vector3.zero;
            Terminal terminal = FindObjectOfType<Terminal>();
            RoundManager currentRound = RoundManager.Instance;
            RandomScrapSpawn[] randomScrapLocations = FindObjectsOfType<RandomScrapSpawn>();
            PlayerControllerB localPlayerController = StartOfRound.Instance.localPlayerController;

            // Targets spectated player if playerTarget is dead
            //    position = (localPlayerController.spectatedPlayerScript).transform.position;

            switch (positionType)
            {
                case 0:
                    if (input == "$")
                    {
                        if (randomScrapLocations.Length != 0 && randomScrapLocations[0] != null)
                        {
                            position = randomScrapLocations[UnityEngine.Random.Range(0, randomScrapLocations.Length)].transform.position;
                        }
                        else
                        {
                            LogMessage($"No RandomScrapSpawn in this area!", true);
                            return Vector3.zero;
                        }
                    }
                    else if (input == "")
                    {
                        position = localPlayerController.transform.position;
                    }
                    else
                    {
                        isPlayerTarget = true;
                    }
                    break;
                case 1:
                    if (input == "" || input == "$")
                    {
                        if (currentRound.outsideAINodes.Length != 0 && currentRound.outsideAINodes[0] != null)
                        {
                            position = currentRound.outsideAINodes[UnityEngine.Random.Range(0, currentRound.insideAINodes.Length)].transform.position;
                            //positionOutput = GameObject.FindGameObjectsWithTag("OutsideAINode")[UnityEngine.Random.Range(0, GameObject.FindGameObjectsWithTag("OutsideAINode").Length - 1)].transform.position;
                        }
                        else
                        {
                            LogMessage($"No outsideAINodes in this area!", true);
                            return Vector3.zero;
                        }
                    }
                    else
                    {
                        isPlayerTarget = true;
                    }
                    break;
                case 2:
                    if (input == "" || input == "$")
                    {
                        if (currentRound.allEnemyVents.Length != 0 && currentRound.allEnemyVents[0] != null)
                        {
                            position = currentRound.allEnemyVents[UnityEngine.Random.Range(0, currentRound.allEnemyVents.Length)].floorNode.position;
                        }
                        else
                        {
                            LogMessage($"No allEnemyVents in this area!", true);
                            return Vector3.zero;
                        }
                    }
                    else
                    {
                        isPlayerTarget = true;
                    }
                    break;
                case 3:
                    if (input == "$")
                    {
                        if (currentRound.insideAINodes.Length != 0 && currentRound.insideAINodes[0] != null)
                        {
                            HUDManager_Patch.sendPlayerInside = true;
                            Vector3 position2 = currentRound.insideAINodes[UnityEngine.Random.Range(0, currentRound.insideAINodes.Length)].transform.position;
                            position = currentRound.GetRandomNavMeshPositionInRadiusSpherical(position2);
                            LogMessage($"Teleported {playerToTP.playerUsername} to random location within factory.");
                        }
                        else
                        {
                            LogMessage($"No insideAINodes in this area!", true);
                            return Vector3.zero;
                        }
                    }
                    else if (input == "!")
                    {
                        if (terminal != null)
                        {
                            HUDManager_Patch.sendPlayerInside = false;
                            position = terminal.transform.position;
                            LogMessage($"Teleported {playerToTP.playerUsername} to terminal.");
                        }
                        else
                        {
                            LogMessage("Terminal not found!", true);
                            return Vector3.zero;
                        }
                    }
                    else if (input.StartsWith("@") && input.Length > 1)
                    {
                        if (int.TryParse(input.Substring(1), out int wpIndex))
                        {
                            if (wpIndex < waypoints.Count)
                            {
                                Waypoint wp = waypoints[wpIndex];
                                HUDManager_Patch.sendPlayerInside = wp.isInside;
                                position = wp.position;
                                LogMessage($"Teleported {playerToTP.playerUsername} to Waypoint @{wpIndex}.");
                            }
                            else
                            {
                                LogMessage($"Waypoint @{input.Substring(1)} is out of bounds!", true);
                                return Vector3.zero;
                            }
                        }
                        else
                        {
                            LogMessage($"Waypoint @{input.Substring(1)} is invalid!", true);
                            return Vector3.zero;
                        }
                    }
                    else
                    {
                        isTP = true;
                        isPlayerTarget = true;
                    }
                    break;
            }

            if (isPlayerTarget)
            {
                PlayerControllerB playerTarget = GetPlayerFromString(input);

                if (playerTarget == null || !playerTarget.isPlayerControlled)
                {
                    return Vector3.zero;
                }
                else if (playerTarget.isPlayerDead)
                {
                    LogMessage($"Could not target {playerTarget.playerUsername}!\nPlayer is dead!", true);
                    return Vector3.zero;
                }

                position = playerTarget.transform.position;

                if (isTP)
                {
                    HUDManager_Patch.sendPlayerInside = Player.Get(playerTarget).IsInFactory;
                    LogMessage($"Teleported {playerToTP.playerUsername} to {playerTarget.playerUsername}.");
                }
            }

            return position;
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

        public static void PlayerTeleportEffects(ulong playerClientID, bool isInside)
        {
            PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(playerClientID));

            // Redirects centipedes, unsure if actually working :p
            if (playerController.redirectToEnemy != null)
            {
                playerController.redirectToEnemy.ShipTeleportEnemy();
            }

            // Saves player from any creatures they are in an animation with
            SavePlayer(playerController);

            // Sets correct AudioReverbPresets
            if ((bool)FindObjectOfType<AudioReverbPresets>())
            {
                mls.LogInfo("Audio preset " + (isInside ? 2 : 3));
                FindObjectOfType<AudioReverbPresets>().audioPresets[isInside ? 2 : 3].ChangeAudioReverbForPlayer(playerController);
            }

            // Ensures player fog/lighting is consistent after a teleport
            playerController.isInElevator = !isInside;
            playerController.isInHangarShipRoom = !isInside;
            playerController.isInsideFactory = isInside;

            playerController.averageVelocity = 0.0f;
            playerController.velocityLastFrame = Vector3.zero;
            playerController.beamUpParticle.Play();
            playerController.beamOutBuildupParticle.Play();
        }
        
        public static void RevivePlayer (ulong playerClientID)  // This function is REALLY long, could probably be shortened
        {
            localPlayerController = StartOfRound.Instance.localPlayerController;
            PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(playerClientID));
            StartOfRound round = StartOfRound.Instance;
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();

            Debug.Log((object)"Reviving players A");
            playerController.ResetPlayerBloodObjects(playerController.isPlayerDead);
            if (playerController.isPlayerDead || playerController.isPlayerControlled)
            {
                playerController.isClimbingLadder = false;
                playerController.ResetZAndXRotation();
                //playerController.roundController.enabled = true;
                playerController.health = 100;
                playerController.disableLookInput = false;
                Debug.Log((object)"Reviving players B");
                if (playerController.isPlayerDead)
                {
                    playerController.isPlayerDead = false;
                    playerController.isPlayerControlled = true;
                    playerController.isInElevator = true;
                    playerController.isInHangarShipRoom = true;
                    playerController.isInsideFactory = false;
                    playerController.wasInElevatorLastFrame = false;
                    round.SetPlayerObjectExtrapolate(false);






                    //playerController.TeleportPlayer(round.GetPlayerSpawnPosition((int)playerClientID));
                    playerController.TeleportPlayer(terminal.transform.position);
                    playerController.setPositionOfDeadPlayer = false;
                    playerController.DisablePlayerModel(round.allPlayerObjects[playerClientID], true, true);
                    playerController.helmetLight.enabled = false;
                    Debug.Log((object)"Reviving players C");
                    playerController.Crouch(false);
                    playerController.criticallyInjured = false;
                    if ((UnityEngine.Object)playerController.playerBodyAnimator != (UnityEngine.Object)null)
                        playerController.playerBodyAnimator.SetBool("Limp", false);
                    playerController.bleedingHeavily = false;
                    playerController.activatingItem = false;
                    playerController.twoHanded = false;
                    playerController.inSpecialInteractAnimation = false;
                    playerController.disableSyncInAnimation = false;
                    playerController.inAnimationWithEnemy = (EnemyAI)null;
                    playerController.holdingWalkieTalkie = false;
                    playerController.speakingToWalkieTalkie = false;
                    Debug.Log((object)"Reviving players D");
                    playerController.isSinking = false;
                    playerController.isUnderwater = false;
                    playerController.sinkingValue = 0.0f;
                    playerController.statusEffectAudio.Stop();
                    playerController.DisableJetpackControlsLocally();
                    playerController.health = 100;
                    Debug.Log((object)"Reviving players E");
                    playerController.mapRadarDotAnimator.SetBool("dead", false);
                    if (playerController.IsOwner)
                    {
                        HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
                        playerController.hasBegunSpectating = false;
                        HUDManager.Instance.RemoveSpectateUI();
                        HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                        playerController.hinderedMultiplier = 1f;
                        playerController.isMovementHindered = 0;
                        playerController.sourcesCausingSinking = 0;
                        Debug.Log((object)"Reviving players E2");
                        playerController.reverbPreset = round.shipReverb;
                    }
                }
                Debug.Log((object)"Reviving players F");
                SoundManager.Instance.earsRingingTimer = 0.0f;
                playerController.voiceMuffledByEnemy = false;
                SoundManager.Instance.playerVoicePitchTargets[playerClientID] = 1f;
                SoundManager.Instance.SetPlayerPitch(1f, (int)playerClientID);
                if ((UnityEngine.Object)playerController.currentVoiceChatIngameSettings == (UnityEngine.Object)null)
                    round.RefreshPlayerVoicePlaybackObjects();
                if ((UnityEngine.Object)playerController.currentVoiceChatIngameSettings != (UnityEngine.Object)null)
                {
                    if ((UnityEngine.Object)playerController.currentVoiceChatIngameSettings.voiceAudio == (UnityEngine.Object)null)
                        playerController.currentVoiceChatIngameSettings.InitializeComponents();
                    if ((UnityEngine.Object)playerController.currentVoiceChatIngameSettings.voiceAudio == (UnityEngine.Object)null)
                        return;
                    playerController.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
                }
                Debug.Log((object)"Reviving players G");
            }

            //PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController;
            playerController.bleedingHeavily = false;
            playerController.criticallyInjured = false;
            playerController.playerBodyAnimator.SetBool("Limp", false);
            playerController.health = 100;
            HUDManager.Instance.UpdateHealthUI(100, false);
            playerController.spectatedPlayerScript = (PlayerControllerB) null;
            HUDManager.Instance.audioListenerLowPass.enabled = false;
            Debug.Log((object) "Reviving players H");
            round.SetSpectateCameraToGameOverMode(false, playerController);
            round.livingPlayers += 1;
            round.allPlayersDead = false;
            round.UpdatePlayerVoiceEffects();
            //round.ResetMiscValues();

            if (localPlayerController.playerClientId == playerController.playerClientId)
            {
                HUDManager.Instance.HideHUD(false);
            }
        }
        
        public static void SavePlayer(PlayerControllerB player) // Eventually make this save you from the masked
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
        }

        public static void SpawnEnemy(int enemyID, int amount, string targetString, bool inside)
        {
            RoundManager currentRound = RoundManager.Instance;
            localPlayerController = StartOfRound.Instance.localPlayerController;

            if ((inside && GetPositionFromCommand(targetString, 2) == Vector3.zero) || (!inside && GetPositionFromCommand(targetString, 1) == Vector3.zero))
            {
                return;
            }

            string logName = inside ? currentRound.currentLevel.Enemies[enemyID].enemyType.enemyName : currentRound.currentLevel.OutsideEnemies[enemyID].enemyType.enemyName;
            LogMessage($"Spawning Enemy - Name: {logName}, ID: {enemyID}, Amount: {amount}, Location: {((targetString == "") || (targetString == "$") ? "Natural" : GetPlayerFromString(targetString).playerUsername)}.");

            // Uses different enemy list depending on what creature the player is trying to spawn
            if (inside)
            {
                try
                {
                    for (int i = 0; i < amount; i++)
                    {
                        //currentRound.SpawnEnemyOnServer(position, currentRound.allEnemyVents[i].floorNode.eulerAngles.y, enemyID);    // This doesn't work when spawning more than 5 enemies at a time
                        Instantiate(currentRound.currentLevel.Enemies[enemyID].enemyType.enemyPrefab, GetPositionFromCommand(targetString, 2), Quaternion.Euler(Vector3.zero)).gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
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
                try
                {
                    for (int i = 0; i < amount; i++)
                    {
                        Instantiate(currentRound.currentLevel.OutsideEnemies[enemyID].enemyType.enemyPrefab, GetPositionFromCommand(targetString, 1), Quaternion.Euler(Vector3.zero)).gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Unable to Spawn Outside Enemy ID: {enemyID}", true);
                    mls.LogError(ex);
                }
            }
        }
        
        public static void SpawnItem(int itemID, int amount, int value, string targetString)
        {
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;
            localPlayerController = StartOfRound.Instance.localPlayerController;

            if (GetPositionFromCommand(targetString, 0) == Vector3.zero)
            {
                return;
            }

            string logValue = value >= 0 ? $"{value}" : "Random";
            string logLocation = targetString != "$" ? $"{GetPlayerFromString(targetString).playerUsername}" : "Random";
            LogMessage($"Spawning - Name: {allItemsList[itemID].name}, ID: {itemID}, Amount: {amount}, Value: {logValue}, Location: {logLocation}.");

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
                    LC_API.GameInterfaceAPI.Features.Item item = LC_API.GameInterfaceAPI.Features.Item.CreateAndSpawnItem(allItemsList[itemID].itemName, true, GetPositionFromCommand(targetString, 0));

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

    public class Waypoint
    {
        public bool isInside { get; set; }
        public Vector3 position { get; set; }
    }
}