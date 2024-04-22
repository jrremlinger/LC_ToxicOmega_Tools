using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using ToxicOmega_Tools.Patches;
using Unity.Netcode;
using UnityEngine;
using static BepInEx.BepInDependency;

namespace ToxicOmega_Tools
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(StaticNetcodeLib.StaticNetcodeLib.Guid, DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "com.toxicomega.toxicomega_tools";
        private const string modName = "ToxicOmega Tools";
        private const string modVersion = "1.2.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        internal static Plugin Instance;
        internal static ManualLogSource mls;
        internal static GUI menu;
        internal static List<SpawnableEnemyWithRarity> customInsideList;
        internal static List<SpawnableEnemyWithRarity> customOutsideList;
        internal static List<SpawnableEnemyWithRarity> allEnemiesList;
        internal static List<SearchableGameObject> allSpawnablesList;
        internal static List<Waypoint> waypoints = new List<Waypoint>();
        internal static System.Random shipTeleporterSeed;
        internal static bool enableGod = false;

        void Awake()
        {
            if (Instance == null)
                Instance = this;

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo("ToxicOmega Tools mod has awoken.");
            harmony.PatchAll();

            // Patch the GUI
            var gameObject = new GameObject("GUI");
            DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.AddComponent<GUI>();
            menu = (GUI)gameObject.GetComponent("GUI");
        }

        public static bool CheckPlayerIsHost(PlayerControllerB player)
        {
            return player.gameObject == player.playersManager.allPlayerObjects[0];
        }

        public static EnemyAI GetEnemyAI(ulong networkObjectId)
        {
            return FindObjectsOfType<EnemyAI>().FirstOrDefault(enemy => enemy.NetworkObjectId.Equals(networkObjectId));
        }

        public static GrabbableObject GetGrabbableObject(ulong networkObjectId)
        {
            return FindObjectsOfType<GrabbableObject>().FirstOrDefault(item => item.NetworkObjectId.Equals(networkObjectId));
        }

        public static TerminalAccessibleObject GetTerminalAccessibleObject(ulong networkObjectId)
        {
            return FindObjectsOfType<TerminalAccessibleObject>().FirstOrDefault(item => item.NetworkObjectId.Equals(networkObjectId));
        }

        public static PlayerControllerB GetPlayerController(ulong clientId)
        {
            StartOfRound round = StartOfRound.Instance;
            if (clientId >= (ulong)round.allPlayerScripts.Length)
                return null;
            return round.allPlayerScripts[clientId];
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

                    if (foundPlayer != null && (foundPlayer.isPlayerControlled || foundPlayer.isPlayerDead))
                    {
                        return foundPlayer;
                    }
                    else
                    {
                        LogMessage($"No Player with ID #{clientId}!", true);
                        return null;
                    }
                }
                else
                {
                    LogMessage($"Player ID #{clientIdString} is invalid!", true);
                    return null;
                }
            }
            else
            {
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

        public static Vector3 GetPositionFromCommand(string input, int positionType, string targetName = null)
        {
            // Position types:
            // 0: Random RandomScrapSpawn[]
            // 1: Random outsideAINodes[]
            // 2: Random allEnemyVents[]
            // 3: Teleport Destination
            // 4: Random insideAINodes[]

            Vector3 position = Vector3.zero;
            bool isPlayerTarget = false;
            Terminal terminal = FindObjectOfType<Terminal>();
            RoundManager currentRound = RoundManager.Instance;
            RandomScrapSpawn[] randomScrapLocations = FindObjectsOfType<RandomScrapSpawn>();
            PlayerControllerB localPlayerController = StartOfRound.Instance.localPlayerController;

            if (input == "" || input == "$")
            {
                switch (positionType)
                {
                    case 0:
                        if (input == "")
                        {
                            if (localPlayerController.isPlayerDead)
                            {
                                if (localPlayerController.spectatedPlayerScript != null)
                                {
                                    position = localPlayerController.spectatedPlayerScript.transform.position;
                                }
                                else
                                {
                                    position = StartOfRound.Instance.allPlayerScripts[localPlayerController.playerClientId].deadBody.transform.position;
                                }
                            }
                            else
                            {
                                position = localPlayerController.transform.position;
                            }
                        }
                        else
                        {
                            if (randomScrapLocations.Length > 0)
                            {
                                position = randomScrapLocations[UnityEngine.Random.Range(0, randomScrapLocations.Length)].transform.position;
                            }
                            else
                            {
                                LogMessage($"No RandomScrapSpawn in this area!", true);
                                return Vector3.zero;
                            }
                        }
                        break;
                    case 1:
                        if (currentRound.outsideAINodes.Length > 0 && currentRound.outsideAINodes[0] != null)
                        {
                            position = currentRound.outsideAINodes[UnityEngine.Random.Range(0, currentRound.outsideAINodes.Length)].transform.position;
                        }
                        else
                        {
                            LogMessage($"No outsideAINodes in this area!", true);
                            return Vector3.zero;
                        }
                        break;
                    case 2:
                        if (currentRound.allEnemyVents.Length > 0 && currentRound.allEnemyVents[0] != null)
                        {
                            position = currentRound.allEnemyVents[UnityEngine.Random.Range(0, currentRound.allEnemyVents.Length)].floorNode.position;
                        }
                        else
                        {
                            LogMessage($"No allEnemyVents in this area!", true);
                            return Vector3.zero;
                        }
                        break;
                    case 3:
                        if (currentRound.insideAINodes.Length > 0 && currentRound.insideAINodes[0] != null)
                        {
                            HUDManager_Patch.sendPlayerInside = true;

                            if (shipTeleporterSeed == null)
                            {
                                mls.LogInfo("Teleport Seed: Random");
                                Vector3 position2 = currentRound.insideAINodes[UnityEngine.Random.Range(0, currentRound.insideAINodes.Length)].transform.position;
                                position = currentRound.GetRandomNavMeshPositionInRadius(position2);
                            }
                            else
                            {
                                mls.LogInfo("Teleport Seed: Inverse-Teleporter");
                                Vector3 position2 = currentRound.insideAINodes[shipTeleporterSeed.Next(0, currentRound.insideAINodes.Length)].transform.position;
                                position = currentRound.GetRandomNavMeshPositionInBoxPredictable(position2, randomSeed: shipTeleporterSeed);
                            }

                            LogMessage($"Teleported {targetName} to random location within factory.");
                        }
                        else
                        {
                            LogMessage($"No insideAINodes in this area!", true);
                            return Vector3.zero;
                        }
                        break;
                    case 4:
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
                        break;
                }
            }
            else if (input == "!")
            {
                if (terminal != null)
                {
                    position = terminal.transform.position;
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
                        HUDManager_Patch.sendPlayerInside = wp.IsInside;
                        position = wp.Position;
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
                bool foundId = ulong.TryParse(input, out ulong networkId);

                if (foundId && GetEnemyAI(networkId) != null)
                {
                    EnemyAI enemy = GetEnemyAI(networkId);
                    HUDManager_Patch.sendPlayerInside = !enemy.isOutside;
                    position = enemy.transform.position;
                }
                else if (foundId && GetGrabbableObject(networkId) != null)
                {
                    GrabbableObject grabbableObject = GetGrabbableObject(networkId);
                    HUDManager_Patch.sendPlayerInside = grabbableObject.isInFactory && !grabbableObject.isInShipRoom;
                    position = grabbableObject.transform.position;
                }
                else
                {
                    isPlayerTarget = true;
                }
            }

            if (isPlayerTarget)
            {
                PlayerControllerB playerTarget = GetPlayerFromString(input);

                if (playerTarget == null)
                    return Vector3.zero;

                position = playerTarget.transform.position;

                if (playerTarget.isPlayerDead)
                {
                    if (localPlayerController.playerClientId == playerTarget.playerClientId && playerTarget.spectatedPlayerScript != null)
                    {
                        position = playerTarget.spectatedPlayerScript.transform.position;
                    }
                    else
                    {
                        position = StartOfRound.Instance.allPlayerScripts[playerTarget.playerClientId].deadBody.transform.position;
                    }
                }

                if (positionType == 3)
                {
                    HUDManager_Patch.sendPlayerInside = playerTarget.isInsideFactory;
                    LogMessage($"Teleported {targetName} to {playerTarget.playerUsername}.");
                }
            }

            return position;
        }

        public static void LogMessage(string message, bool isError = false)
        {
            string headerText;

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

        public static void PlayerTeleportEffects(ulong playerClientId, bool isInside)
        {
            PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(playerClientId));

            // Redirects enemies in animation with player, unsure if actually working
            playerController.redirectToEnemy?.ShipTeleportEnemy();

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

        public static void RevivePlayer(ulong playerClientId)
        {
            PlayerControllerB localPlayerController = StartOfRound.Instance.localPlayerController;
            PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(playerClientId));
            StartOfRound round = StartOfRound.Instance;
            Terminal terminal = FindObjectOfType<Terminal>();

            Debug.Log("Reviving players A");
            playerController.ResetPlayerBloodObjects(playerController.isPlayerDead);
            if (playerController.isPlayerDead || playerController.isPlayerControlled)
            {
                playerController.isClimbingLadder = false;
                playerController.ResetZAndXRotation();
                playerController.health = 100;
                playerController.disableLookInput = false;
                Debug.Log("Reviving players B");
                if (playerController.isPlayerDead)
                {
                    playerController.isPlayerDead = false;
                    playerController.isPlayerControlled = true;
                    playerController.isInElevator = true;
                    playerController.isInHangarShipRoom = true;
                    playerController.isInsideFactory = false;
                    playerController.wasInElevatorLastFrame = false;
                    round.SetPlayerObjectExtrapolate(false);
                    playerController.TeleportPlayer(round.GetPlayerSpawnPosition((int)playerClientId));
                    playerController.TeleportPlayer(terminal.transform.position);
                    playerController.setPositionOfDeadPlayer = false;
                    playerController.DisablePlayerModel(round.allPlayerObjects[playerClientId], true, true);
                    playerController.helmetLight.enabled = false;
                    Debug.Log("Reviving players C");
                    playerController.Crouch(false);
                    playerController.criticallyInjured = false;
                    playerController.playerBodyAnimator?.SetBool("Limp", false);
                    playerController.bleedingHeavily = false;
                    playerController.activatingItem = false;
                    playerController.twoHanded = false;
                    playerController.inSpecialInteractAnimation = false;
                    playerController.disableSyncInAnimation = false;
                    playerController.inAnimationWithEnemy = null;
                    playerController.holdingWalkieTalkie = false;
                    playerController.speakingToWalkieTalkie = false;
                    Debug.Log("Reviving players D");
                    playerController.isSinking = false;
                    playerController.isUnderwater = false;
                    playerController.sinkingValue = 0.0f;
                    playerController.statusEffectAudio.Stop();
                    playerController.DisableJetpackControlsLocally();
                    playerController.health = 100;
                    Debug.Log("Reviving players E");
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
                        Debug.Log("Reviving players E2");
                        playerController.reverbPreset = round.shipReverb;
                    }
                }
                Debug.Log("Reviving players F");
                SoundManager.Instance.earsRingingTimer = 0.0f;
                playerController.voiceMuffledByEnemy = false;
                SoundManager.Instance.playerVoicePitchTargets[playerClientId] = 1f;
                SoundManager.Instance.SetPlayerPitch(1f, (int)playerClientId);

                if (playerController.currentVoiceChatIngameSettings == null)
                    round.RefreshPlayerVoicePlaybackObjects();

                if (playerController.currentVoiceChatIngameSettings != null)
                {
                    if (playerController.currentVoiceChatIngameSettings.voiceAudio == null)
                        playerController.currentVoiceChatIngameSettings.InitializeComponents();

                    if (playerController.currentVoiceChatIngameSettings.voiceAudio == null)
                        return;

                    playerController.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
                }
                Debug.Log("Reviving players G");
            }

            playerController.bleedingHeavily = false;
            playerController.criticallyInjured = false;
            playerController.playerBodyAnimator.SetBool("Limp", false);
            playerController.health = 100;
            HUDManager.Instance.UpdateHealthUI(100, false);
            playerController.spectatedPlayerScript = null;
            HUDManager.Instance.audioListenerLowPass.enabled = false;
            Debug.Log("Reviving players H");
            round.SetSpectateCameraToGameOverMode(false, playerController);
            round.livingPlayers += 1;
            round.allPlayersDead = false;
            round.UpdatePlayerVoiceEffects();
            round.ResetMiscValues();

            if (localPlayerController.playerClientId == playerController.playerClientId)
                HUDManager.Instance.HideHUD(false);
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

            // Makes mechs drop player and stuns them
            RadMechAI[] mechs = FindObjectsByType<RadMechAI>(FindObjectsSortMode.None);
            for (int i = 0; i < mechs.Length; i++)
            {
                if (mechs[i].inSpecialAnimationWithPlayer == player)
                {
                    mechs[i].CancelSpecialAnimations();
                    mechs[i].CancelTorchPlayerAnimation();
                    mechs[i].GetComponentInChildren<EnemyAI>().SetEnemyStunned(true, 7.5f, player);
                }
            }

            // Clears blood off of screen
            if (StartOfRound.Instance.localPlayerController.playerClientId == player.playerClientId)
                HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", false);
        }

        private static void SpawningMessage(SearchableGameObject obj, string targetString, int amount, int value = 0)
        {
            string logLocation;
            string type = "Unknown";
            string logValue = "";

            if (obj.IsItem)
            {
                type = "Item";
                logValue = value != -1 ? string.Concat(value) : "Random";
            }
            else if (obj.IsEnemy)
            {
                type = "Enemy";
            }
            else if (obj.IsTrap)
            {
                type = "Trap";
            }

            if (targetString == "$" || (targetString == "" && !obj.IsItem))
            {
                logLocation = "Random";
            }
            else if (targetString == "!")
            {
                logLocation = "Terminal";
            }
            else if (targetString.StartsWith("@"))
            {
                logLocation = $"WP @{targetString.Substring(1)}";
            }
            else
            {
                bool foundId = ulong.TryParse(targetString, out ulong networkId);

                if (foundId && GetGrabbableObject(networkId) != null)
                {
                    logLocation = GetGrabbableObject(networkId).itemProperties.itemName;
                }
                else if (foundId && GetEnemyAI(networkId) != null)
                {
                    logLocation = GetEnemyAI(networkId).enemyType.enemyName;
                }
                else if (GetPlayerFromString(targetString) != null)
                {
                    logLocation = GetPlayerFromString(targetString).playerUsername;
                }
                else
                {
                    return;
                }
            }

            LogMessage($"Spawned {type}\nName: {obj.Name}, Location: {logLocation}, Amount: {amount}{(obj.IsItem ? $", Value: {logValue}" : ".")}");
        }

        public static void SpawnEnemy(SearchableGameObject enemy, int amount, string targetString)
        {
            if (GetPositionFromCommand(targetString, enemy.IsOutsideEnemy ? 1 : 2) == Vector3.zero)
                return;

            SpawningMessage(enemy, targetString, amount);

            for (int i = 0; i < amount; i++)
            {
                try
                {
                    GameObject prefab = enemy.IsOutsideEnemy ? customOutsideList[enemy.Id].enemyType.enemyPrefab : customInsideList[enemy.Id].enemyType.enemyPrefab;
                    Vector3 position = GetPositionFromCommand(targetString, enemy.IsOutsideEnemy ? 1 : 2);
                    Instantiate(prefab, position, Quaternion.Euler(Vector3.zero)).gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                }
                catch (Exception ex)
                {
                    LogMessage($"Unable to Spawn Enemy: {enemy.Name}", true);
                    mls.LogError(ex);
                }
            }
        }

        public static void SpawnItem(SearchableGameObject item, int amount, int value, string targetString)
        {
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;

            if (GetPositionFromCommand(targetString, 0) == Vector3.zero)
                return;

            SpawningMessage(item, targetString, amount, value);

            for (int i = 0; i < amount; i++)
            {
                try
                {
                    // The Shotgun (and maybe other items I haven't noticed) have their max and min values backwards causing an index error without this
                    if (allItemsList[item.Id].minValue > allItemsList[item.Id].maxValue)
                        (allItemsList[item.Id].maxValue, allItemsList[item.Id].minValue) = (allItemsList[item.Id].minValue, allItemsList[item.Id].maxValue);

                    int setValue = (int)(double)(value == -1 ? UnityEngine.Random.Range(allItemsList[item.Id].minValue, allItemsList[item.Id].maxValue) * RoundManager.Instance.scrapValueMultiplier : value);

                    Vector3 position = GetPositionFromCommand(targetString, 0);
                    GameObject myItem = Instantiate(allItemsList[item.Id].spawnPrefab, position, Quaternion.identity);
                    myItem.GetComponent<GrabbableObject>().transform.rotation = Quaternion.Euler(myItem.GetComponent<GrabbableObject>().itemProperties.restingRotation);
                    myItem.GetComponent<GrabbableObject>().fallTime = 0f;
                    myItem.GetComponent<NetworkObject>().Spawn();
                    Networking.SyncScrapValueClientRpc(myItem.GetComponent<GrabbableObject>().NetworkObjectId, setValue);

                    // RPC to set Shotgun shells loaded to be two for all players
                    if (allItemsList[item.Id].itemName == "Shotgun")
                    {
                        Networking.SyncAmmoClientRpc(myItem.GetComponent<GrabbableObject>().NetworkObjectId);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Unable to Spawn Item: {item.Name}", true);
                    mls.LogError(ex);
                }
            }
        }

        public static void SpawnTrap(SearchableGameObject trap, int amount, string targetString)
        {
            RoundManager currentRound = RoundManager.Instance;

            if (GetPositionFromCommand(targetString, 4) == Vector3.zero)
                return;

            SpawningMessage(trap, targetString, amount);

            switch (trap.Id)
            {
                case 0:
                    foreach (SpawnableMapObject obj in currentRound.currentLevel.spawnableMapObjects)
                    {
                        try
                        {
                            if (obj.prefabToSpawn.GetComponentInChildren<Landmine>() != null)   // Find prefab to copy
                            {
                                for (int i = 0; i < amount; i++)
                                {
                                    Vector3 inBoxPredictable = currentRound.GetRandomNavMeshPositionInRadius(obj.prefabToSpawn.transform.position);
                                    GameObject mine = Instantiate(obj.prefabToSpawn, GetPositionFromCommand(targetString, 4), Quaternion.identity);
                                    mine.GetComponent<NetworkObject>().Spawn(true);

                                    int randomCode = UnityEngine.Random.Range(0, RoundManager.Instance.possibleCodesForBigDoors.Length - 1);
                                    Networking.TerminalCodeClientRpc(mine.GetComponentInChildren<TerminalAccessibleObject>().NetworkObjectId, randomCode);
                                }
                                break;  // Break after finding first matching prefab
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Unable to Spawn Trap: {trap.Name}!", true);
                            mls.LogError(ex);
                        }
                    }
                    break;
                case 1:
                    foreach (SpawnableMapObject obj in currentRound.currentLevel.spawnableMapObjects)
                    {
                        try
                        {
                            if (obj.prefabToSpawn.GetComponentInChildren<Turret>() != null) // Find prefab to copy
                            {
                                for (int i = 0; i < amount; i++)
                                {
                                    Vector3 pos = GetPositionFromCommand(targetString, 4);
                                    GameObject turret = Instantiate(obj.prefabToSpawn, pos, Quaternion.identity);
                                    turret.transform.eulerAngles = new Vector3(0.0f, currentRound.YRotationThatFacesTheFarthestFromPosition(pos + (Vector3.up * 0.2f)), 0.0f);
                                    turret.GetComponent<NetworkObject>().Spawn(true);

                                    int randomCode = UnityEngine.Random.Range(0, RoundManager.Instance.possibleCodesForBigDoors.Length - 1);
                                    Networking.TerminalCodeClientRpc(turret.GetComponentInChildren<TerminalAccessibleObject>().NetworkObjectId, randomCode);
                                }
                                break;  // Break after finding first matching prefab
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Unable to Spawn Trap: {trap.Name}!", true);
                            mls.LogError(ex);
                        }
                    }
                    break;
                case 2:
                    foreach (SpawnableMapObject obj in currentRound.currentLevel.spawnableMapObjects)   // Find prefab to copy
                    {
                        try
                        {
                            if (obj.prefabToSpawn.GetComponentInChildren<SpikeRoofTrap>() != null)
                            {
                                for (int i = 0; i < amount; i++)
                                {
                                    Vector3 pos = GetPositionFromCommand(targetString, 4);
                                    GameObject spikes = Instantiate(obj.prefabToSpawn, pos, Quaternion.identity);

                                    if (targetString == "" || targetString == "$")
                                    {
                                        if (Physics.Raycast(spikes.transform.position, -spikes.transform.forward, out RaycastHit hitInfo, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                                        {
                                            spikes.transform.position = hitInfo.point;
                                            {
                                                spikes.transform.forward = hitInfo.normal;
                                                spikes.transform.eulerAngles = new Vector3(0.0f, spikes.transform.eulerAngles.y, 0.0f);
                                            }
                                        }
                                    }

                                    spikes.GetComponentInChildren<SpikeRoofTrap>().Start();
                                    spikes.GetComponent<NetworkObject>().Spawn(true);

                                    int randomCode = UnityEngine.Random.Range(0, RoundManager.Instance.possibleCodesForBigDoors.Length - 1);
                                    Networking.TerminalCodeClientRpc(spikes.GetComponentInChildren<TerminalAccessibleObject>().NetworkObjectId, randomCode);
                                }
                                break;  // Break after finding first matching prefab
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Unable to Spawn Trap: {trap.Name}!", true);
                            mls.LogError(ex);
                        }
                    }
                    break;
            }
        }
    }

    public struct SearchableGameObject
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public bool IsItem { get; set; }
        public bool IsEnemy { get; set; }
        public bool IsOutsideEnemy { get; set; }
        public bool IsTrap { get; set; }
    }

    public struct Waypoint
    {
        public bool IsInside { get; set; }
        public Vector3 Position { get; set; }
    }
}