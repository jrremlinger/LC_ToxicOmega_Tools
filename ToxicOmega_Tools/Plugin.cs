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

        public static PlayerControllerB FindPlayerFromString(string searchString)
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
        
        public static void RevivePlayer (ulong playerClientID)
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
            //if (localPlayerController.isPlayerDead && localPlayerController.playerClientId == playerTarget.playerClientId)
            //{
            //    if (localPlayerController.spectatedPlayerScript != null)
            //    {
            //        position = localPlayerController.spectatedPlayerScript.transform.position;
            //    }
            //    else return;
            //}
            if (playerTarget.isPlayerDead)
            {
                LogMessage($"Could not spawn enemy at {playerTarget.playerUsername}!\nPlayer is dead!", true);
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
            //if (localPlayerController.isPlayerDead && localPlayerController.playerClientId == playerTarget.playerClientId)
            //{
            //    position = (localPlayerController.spectatedPlayerScript).transform.position;
            //}
            if (playerTarget.isPlayerDead)
            {
                LogMessage($"Could not spawn item at {playerTarget.playerUsername}!\nPlayer is dead!", true);
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