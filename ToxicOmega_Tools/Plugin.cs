using BepInEx.Logging;
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameNetcodeStuff;
using Unity.Netcode;
using System;
using ToxicOmega_Tools.Patches;
using static BepInEx.BepInDependency;

namespace ToxicOmega_Tools
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(StaticNetcodeLib.StaticNetcodeLib.Guid, DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "com.toxicomega.toxicomega_tools";
        private const string modName = "ToxicOmega Tools";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        internal static Plugin Instance;
        internal static ManualLogSource mls;

        internal List<SpawnableEnemyWithRarity> customInsideList = new List<SpawnableEnemyWithRarity>();
        internal List<SpawnableEnemyWithRarity> customOutsideList = new List<SpawnableEnemyWithRarity>();
        internal List<Waypoint> waypoints = new List<Waypoint>();
        internal System.Random shipTeleporterSeed;
        internal bool enableGod = false;

        void Awake()
        {
            if (Instance == null)
                Instance = this;

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo("ToxicOmega Tools mod has awoken.");
            harmony.PatchAll();
            //Network.RegisterAll();    // LC API Networking
        }

        public static bool CheckPlayerIsHost(PlayerControllerB player)
        {
            //return Player.HostPlayer.ClientId == player.playerClientId;
            return player.isHostPlayerObject;   // IDK IF THIS WORKS
        }

        public static GrabbableObject GetGrabbableObject(ulong networkObjectID)
        {
            return FindObjectsOfType<GrabbableObject>().FirstOrDefault(item => item.NetworkObjectId.Equals(networkObjectID));
        }

        public static PlayerControllerB GetPlayerController(ulong clientId)
        {
            return StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[clientId]];
        }

        public static PlayerControllerB GetPlayerFromString(string searchString)
        {
            PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;

            // Search player by ID# if string starts with "#"
            if (searchString.StartsWith("#") && searchString.Length > 1)
            {
                string clientIdString = searchString.Substring(1);

                if (ulong.TryParse(clientIdString, out ulong clientId))
                {
                    PlayerControllerB foundPlayer = GetPlayerController(clientId);
                    
                    if (foundPlayer != null)
                        return foundPlayer;
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
                PlayerControllerB foundPlayer = allPlayerScripts.FirstOrDefault(player => player.playerUsername.ToLower().StartsWith(searchString.ToLower()));
                
                if (foundPlayer != null)
                    return foundPlayer;
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
            // 4: Random insideAINodes[]

            bool isPlayerTarget = false;
            bool isTP = false;
            Vector3 position = Vector3.zero;
            Terminal terminal = FindObjectOfType<Terminal>();
            RoundManager currentRound = RoundManager.Instance;
            RandomScrapSpawn[] randomScrapLocations = FindObjectsOfType<RandomScrapSpawn>();
            PlayerControllerB localPlayerController = StartOfRound.Instance.localPlayerController;

            switch (positionType)
            {
                case 0:
                    if (input == "$")
                    {
                        if (randomScrapLocations.Length > 0)
                            position = randomScrapLocations[UnityEngine.Random.Range(0, randomScrapLocations.Length)].transform.position;
                        else
                        {
                            LogMessage($"No RandomScrapSpawn in this area!", true);
                            return Vector3.zero;
                        }
                    }
                    else if (input.StartsWith("@") && input.Length > 1)
                    {
                        if (int.TryParse(input.Substring(1), out int wpIndex))
                        {
                            if (wpIndex < Instance.waypoints.Count)
                            {
                                Waypoint wp = Instance.waypoints[wpIndex];
                                HUDManager_Patch.sendPlayerInside = wp.isInside;
                                position = wp.position;
                            }
                            else
                            {
                                LogMessage($"Waypoint @{input.Substring(1)} does not exist!", true);
                                return Vector3.zero;
                            }
                        }
                        else
                        {
                            LogMessage($"Waypoint @{input.Substring(1)} is invalid!", true);
                            return Vector3.zero;
                        }
                    }
                    else if (input == "")
                        position = localPlayerController.transform.position;
                    else
                        isPlayerTarget = true;
                    break;
                case 1:
                    if (input == "" || input == "$")
                    {
                        if (currentRound.outsideAINodes.Length > 0 && currentRound.outsideAINodes[0] != null)
                            position = currentRound.outsideAINodes[UnityEngine.Random.Range(0, currentRound.outsideAINodes.Length)].transform.position;
                        else
                        {
                            LogMessage($"No outsideAINodes in this area!", true);
                            return Vector3.zero;
                        }
                    }
                    else if (input.StartsWith("@") && input.Length > 1)
                    {
                        if (int.TryParse(input.Substring(1), out int wpIndex))
                        {
                            if (wpIndex < Instance.waypoints.Count)
                            {
                                Waypoint wp = Instance.waypoints[wpIndex];
                                position = wp.position;
                            }
                            else
                            {
                                LogMessage($"Waypoint @{input.Substring(1)} does not exist!", true);
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
                        isPlayerTarget = true;
                    break;
                case 2:
                    if (input == "" || input == "$")
                    {
                        if (currentRound.allEnemyVents.Length > 0 && currentRound.allEnemyVents[0] != null)
                            position = currentRound.allEnemyVents[UnityEngine.Random.Range(0, currentRound.allEnemyVents.Length)].floorNode.position;
                        else
                        {
                            LogMessage($"No allEnemyVents in this area!", true);
                            return Vector3.zero;
                        }
                    }
                    else if (input.StartsWith("@") && input.Length > 1)
                    {
                        if (int.TryParse(input.Substring(1), out int wpIndex))
                        {
                            if (wpIndex < Instance.waypoints.Count)
                            {
                                Waypoint wp = Instance.waypoints[wpIndex];
                                position = wp.position;
                            }
                            else
                            {
                                LogMessage($"Waypoint @{input.Substring(1)} does not exist!", true);
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
                        isPlayerTarget = true;
                    break;
                case 3:
                    if (input == "$")
                    {
                        if (currentRound.insideAINodes.Length > 0 && currentRound.insideAINodes[0] != null)
                        {
                            HUDManager_Patch.sendPlayerInside = true;

                            if (Instance.shipTeleporterSeed == null)
                            {
                                mls.LogInfo("Teleport Seed: Random");
                                Vector3 position2 = currentRound.insideAINodes[UnityEngine.Random.Range(0, currentRound.insideAINodes.Length)].transform.position;
                                position = currentRound.GetRandomNavMeshPositionInRadius(position2); 
                            }
                            else
                            {
                                mls.LogInfo("Teleport Seed: Inverse-Teleporter");
                                Vector3 position2 = currentRound.insideAINodes[Instance.shipTeleporterSeed.Next(0, currentRound.insideAINodes.Length)].transform.position;
                                position = currentRound.GetRandomNavMeshPositionInBoxPredictable(position2, randomSeed: Instance.shipTeleporterSeed);
                            }

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
                            if (wpIndex < Instance.waypoints.Count)
                            {
                                Waypoint wp = Instance.waypoints[wpIndex];
                                HUDManager_Patch.sendPlayerInside = wp.isInside;
                                position = wp.position;
                                LogMessage($"Teleported {playerToTP.playerUsername} to Waypoint @{wpIndex}.");
                            }
                            else
                            {
                                LogMessage($"Waypoint @{input.Substring(1)} does not exist!", true);
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
                case 4:
                    if (input == "" || input == "$")
                    {
                        if (currentRound.insideAINodes.Length > 0 && currentRound.insideAINodes[0] != null)
                        {
                            Vector3 position2 = currentRound.insideAINodes[UnityEngine.Random.Range(0, currentRound.insideAINodes.Length)].transform.position;
                            position = currentRound.GetRandomNavMeshPositionInRadius(position2);
                        }
                        else
                        {
                            LogMessage($"No insideAINodes in this area!", true);
                            return Vector3.zero;
                        }
                    }
                    else if (input.StartsWith("@") && input.Length > 1)
                    {
                        if (int.TryParse(input.Substring(1), out int wpIndex))
                        {
                            if (wpIndex < Instance.waypoints.Count)
                            {
                                Waypoint wp = Instance.waypoints[wpIndex];
                                position = wp.position;
                            }
                            else
                            {
                                LogMessage($"Waypoint @{input.Substring(1)} does not exist!", true);
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
                        isPlayerTarget = true;
                    break;
            }

            if (isPlayerTarget)
            {
                PlayerControllerB playerTarget = GetPlayerFromString(input);

                if (playerTarget == null || !playerTarget.isPlayerControlled)
                    return Vector3.zero;
                else if (playerTarget.isPlayerDead)
                {
                    LogMessage($"Could not target {playerTarget.playerUsername}!\nPlayer is dead!", true);
                    return Vector3.zero;
                }

                position = playerTarget.transform.position;

                if (isTP)
                {
                    HUDManager_Patch.sendPlayerInside = playerTarget.isInsideFactory;
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

            // Redirects enemies in animation with player, unsure if actually working
            if (playerController.redirectToEnemy != null)
                playerController.redirectToEnemy.ShipTeleportEnemy();

            SavePlayer(playerController);

            // Update reverb to inside or outside
            if ((bool)FindObjectOfType<AudioReverbPresets>())
                FindObjectOfType<AudioReverbPresets>().audioPresets[isInside ? 2 : 3].ChangeAudioReverbForPlayer(playerController);

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
            PlayerControllerB localPlayerController = StartOfRound.Instance.localPlayerController;
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
                    centipedes[i].HitEnemy(0, player, true);
            }
            
            // Makes forest giant drop player and stuns them
            ForestGiantAI[] giants = FindObjectsByType<ForestGiantAI>(FindObjectsSortMode.None);
            for (int i = 0; i < giants.Length; i++)
            {
                if (giants[i].inSpecialAnimationWithPlayer == player)
                    giants[i].GetComponentInChildren<EnemyAI>().SetEnemyStunned(true, 7.5f, player);
            }

            // Save player from the Masked
            MaskedPlayerEnemy[] masked = FindObjectsByType<MaskedPlayerEnemy>(FindObjectsSortMode.None);
            for (int i = 0; i < masked.Length; i++)
            {
                if (masked[i].inSpecialAnimationWithPlayer == player)
                {
                    masked[i].CancelSpecialAnimationWithPlayer();
                    masked[i].HitEnemy(0, player, true);
                    masked[i].GetComponentInChildren<EnemyAI>().SetEnemyStunned(true, 7.5f, player);
                }
            }

            // Clears blood off of screen
            if (StartOfRound.Instance.localPlayerController.playerClientId == player.playerClientId)
                HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", false);
        }

        public static void SpawnEnemy(int enemyID, int amount, string targetString)
        {
            bool inside = false;
            List<SpawnableEnemyWithRarity> allEnemiesList = new List<SpawnableEnemyWithRarity>();
            allEnemiesList.AddRange(Instance.customOutsideList);
            allEnemiesList.AddRange(Instance.customInsideList);

            if (enemyID > Instance.customOutsideList.Count)
                inside = true;

            if (GetPositionFromCommand(targetString, inside ? 2 : 1) == Vector3.zero)
                return;

            string logLocation;
            string logName = allEnemiesList[enemyID].enemyType.enemyName;

            if (targetString == "" || targetString == "$")
                logLocation = "Random";
            else if (targetString.StartsWith("@"))
                logLocation = $"WP @{targetString.Substring(1)}";
            else
                logLocation = GetPlayerFromString(targetString).playerUsername;

            LogMessage($"Spawned Enemy\nName: {logName}, ID: {enemyID}, Amount: {amount}, Location: {logLocation}.");

            for (int i = 0; i < amount; i++)
            {
                try
                {
                    Instantiate(allEnemiesList[enemyID].enemyType.enemyPrefab, GetPositionFromCommand(targetString, inside ? 2 : 1), Quaternion.Euler(Vector3.zero)).gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                }
                catch (Exception ex)
                {
                    LogMessage($"Unable to Spawn Enemy ID: {enemyID}", true);
                    mls.LogError(ex);
                }
            }
        }

        public static void SpawnItem(int itemID, int amount, int value, string targetString)
        {
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;

            if (GetPositionFromCommand(targetString, 0) == Vector3.zero)
                return;

            string logValue = value >= 0 ? $"{value}" : "Random";
            string logLocation;

            if (targetString == "$")
                logLocation = "Random";
            else if (targetString.StartsWith("@"))
                logLocation = $"WP @{ targetString.Substring(1) }";
            else
                logLocation = GetPlayerFromString(targetString).playerUsername;

            LogMessage($"Spawned Item\nName: {allItemsList[itemID].name}, ID: {itemID}, Amount: {amount}, Value: {logValue}, Location: {logLocation}.");

            for (int i = 0; i < amount; i++)
            {
                try
                {
                    // The Shotgun (and maybe other items I haven't noticed) have their max and min values backwards causing an index error without this
                    if (allItemsList[itemID].minValue > allItemsList[itemID].maxValue)
                    {
                        int temp = allItemsList[itemID].minValue;
                        allItemsList[itemID].minValue = allItemsList[itemID].maxValue;
                        allItemsList[itemID].maxValue = temp;
                    }

                    int setValue = (int)((double)(value == -1 ? UnityEngine.Random.Range(allItemsList[itemID].minValue, allItemsList[itemID].maxValue) : value) * RoundManager.Instance.scrapValueMultiplier);

                    GameObject item = Instantiate(allItemsList[itemID].spawnPrefab, GetPositionFromCommand(targetString, 0), Quaternion.identity);
                    item.GetComponent<GrabbableObject>().transform.rotation = Quaternion.Euler(item.GetComponent<GrabbableObject>().itemProperties.restingRotation);
                    item.GetComponent<GrabbableObject>().fallTime = 0f;
                    item.GetComponent<NetworkObject>().Spawn();

                    mls.LogInfo("RPC SENDING: \"SyncScrapClientRpc\".");
                    TOTNetworking.SyncScrapClientRpc(new TOT_SyncScrapData { itemID = item.GetComponent<GrabbableObject>().NetworkObjectId, scrapValue = setValue });

                    // RPC to set Shotgun shells loaded to be two for all players
                    if (itemID == 59)
                    {
                        mls.LogInfo("RPC SENDING: \"SyncAmmoClientRpc\".");
                        TOTNetworking.SyncAmmoClientRpc(item.GetComponent<GrabbableObject>().NetworkObjectId);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Unable to Spawn Item ID: {itemID}", true);
                    mls.LogError(ex);
                }
            }
        }

        public static void SpawnTrap(int trapID, int amount, string targetString)
        {
            RoundManager currentRound = RoundManager.Instance;

            if (GetPositionFromCommand(targetString, 4) == Vector3.zero)
                return;

            string logLocation;

            if (targetString == "" || targetString == "$")
                logLocation = "Random";
            else if (targetString.StartsWith("@"))
                logLocation = $"WP @{targetString.Substring(1)}";
            else
                logLocation = GetPlayerFromString(targetString).playerUsername;

            LogMessage($"Spawned Trap\nName: {(trapID == 1 ? "Turret" : "Mine")}, Amount: {amount}, Location: {logLocation}.");

            switch (trapID)
            {
                case 0:
                    foreach (SpawnableMapObject obj in currentRound.currentLevel.spawnableMapObjects)
                    {
                        try
                        {
                            if (obj.prefabToSpawn.GetComponentInChildren<Landmine>() != null)
                            {
                                for (int i = 0; i < amount; i++)
                                {
                                    Vector3 inBoxPredictable = currentRound.GetRandomNavMeshPositionInRadius(obj.prefabToSpawn.transform.position);
                                    GameObject mine = Instantiate(obj.prefabToSpawn, GetPositionFromCommand(targetString, 4), Quaternion.identity);
                                    mine.GetComponent<NetworkObject>().Spawn(true);
                                }
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Unable to Spawn Trap: {(trapID == 1 ? "Turret" : "Mine")}!", true);
                            mls.LogError(ex);
                        }
                    }
                    break;
                case 1:
                    foreach (SpawnableMapObject obj in currentRound.currentLevel.spawnableMapObjects)
                    {
                        try
                        {
                            if (obj.prefabToSpawn.GetComponentInChildren<Turret>() != null)
                            {
                                for (int i = 0; i < amount; i++)
                                {
                                    Vector3 pos = GetPositionFromCommand(targetString, 4);
                                    GameObject turret = Instantiate(obj.prefabToSpawn, pos, Quaternion.identity);
                                    turret.transform.eulerAngles = new Vector3(0.0f, currentRound.YRotationThatFacesTheFarthestFromPosition(pos + Vector3.up * 0.2f), 0.0f);
                                    turret.GetComponent<NetworkObject>().Spawn(true);
                                }
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Unable to Spawn Trap: {(trapID == 1 ? "Turret" : "Mine")}!", true);
                            mls.LogError(ex);
                        }
                    }
                    break;
            }
        }
    }

    public class Waypoint
    {
        public bool isInside { get; set; }
        public Vector3 position { get; set; }
    }
}