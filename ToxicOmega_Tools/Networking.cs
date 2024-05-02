using GameNetcodeStuff;
using StaticNetcodeLib;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [StaticNetcode]
    internal class Networking : MonoBehaviour
    {
        [ClientRpc]
        public static void ChargePlayerClientRpc(ulong playerId)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"ChargePlayerClientRpc\".");
            PlayerControllerB playerTarget = Plugin.GetPlayerFromClientId(playerId);
            if (playerTarget != null)
            {
                GrabbableObject foundItem = playerTarget.ItemSlots[playerTarget.currentItemSlot];
                if (foundItem != null && foundItem.itemProperties.requiresBattery)
                {
                    foundItem.insertedBattery.empty = false;
                    foundItem.insertedBattery.charge = 1f;
                }
            }
        }

        [ClientRpc]
        public static void DoorLockClientRpc(NetworkObjectReference door, bool isUnlock = false)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"DoorLockClientRpc\".");
            DoorLock foundDoor = GetDoorByNetRef(door);
            if (isUnlock)
            {
                foundDoor?.UnlockDoor();
            }
            else
            {
                foundDoor?.LockDoor();
            }
        }

        [ClientRpc]
        public static void GiveItemClientRpc(ulong playerId, NetworkObjectReference itemRef)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"GiveItemClientRpc\".");
            PlayerControllerB player = Plugin.GetPlayerFromClientId(playerId);
            GrabbableObject item = GetItemByNetRef(itemRef);
            if (item == null)
                return;
            player.currentlyGrabbingObject = item;
            if (!GameNetworkManager.Instance.gameHasStarted && !item.itemProperties.canBeGrabbedBeforeGameStart && StartOfRound.Instance.testRoom == null)
                return;
            player.grabInvalidated = false;
            if (player.inSpecialInteractAnimation || item.isHeld || item.isPocketed)
                return;
            NetworkObject networkObject = item.NetworkObject;
            if (networkObject == null || !networkObject.IsSpawned)
                return;
            item.InteractItem();
            if (!item.grabbable || player.FirstEmptyItemSlot() == -1)
                return;
            player.playerBodyAnimator.SetBool("GrabInvalidated", false);
            player.playerBodyAnimator.SetBool("GrabValidated", false);
            player.playerBodyAnimator.SetBool("cancelHolding", false);
            player.playerBodyAnimator.ResetTrigger("Throw");
            player.SetSpecialGrabAnimationBool(true);
            player.isGrabbingObjectAnimation = true;
            player.cursorIcon.enabled = false;
            player.cursorTip.text = "";
            player.twoHanded = item.itemProperties.twoHanded;
            player.carryWeight += Mathf.Clamp(item.itemProperties.weight - 1f, 0.0f, 10f);
            player.grabObjectAnimationTime = (double)item.itemProperties.grabAnimationTime <= 0.0 ? 0.4f : item.itemProperties.grabAnimationTime;
            if (!player.isTestingPlayer)
                player.GrabObjectServerRpc((NetworkObjectReference)networkObject);
            if (player.grabObjectCoroutine != null)
                player.StopCoroutine(player.grabObjectCoroutine);
            player.grabObjectCoroutine = player.StartCoroutine(player.GrabObject());
        }

        [ClientRpc]
        public static void HealPlayerClientRpc(ulong playerId)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"HealPlayerClientRpc\".");
            PlayerControllerB player = Plugin.GetPlayerFromClientId(playerId);
            if (player == null)
                return;
            player.sprintMeter = 100f;
            player.health = 100;
            player.DamagePlayer(-1);
            Plugin.SavePlayer(player);
            player.isExhausted = false;
            player.bleedingHeavily = false;
            if (player.isPlayerDead)
                Plugin.RevivePlayer(player.playerClientId);
        }

        [ClientRpc]
        public static void HurtPlayerClientRpc(ulong playerId, int damage)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"HurtPlayerClientRpc\".");
            Plugin.GetPlayerFromClientId(playerId)?.DamagePlayer(damage, causeOfDeath: CauseOfDeath.Mauling, fallDamage: true, force: new Vector3 { y = 5 });
        }

        [ClientRpc]
        public static void SyncAmmoClientRpc(NetworkObjectReference itemRef)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"SyncAmmoClientRpc\".");
            ShotgunItem shotgun = GetItemByNetRef(itemRef).GetComponentInChildren<ShotgunItem>();
            if (shotgun != null)
                shotgun.shellsLoaded = 2;
        }

        [ClientRpc]
        public static void SyncScrapValueClientRpc(NetworkObjectReference itemRef, int scrapValue)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"SyncScrapValueClientRpc\".");
            GetItemByNetRef(itemRef)?.SetScrapValue(scrapValue);
        }

        [ClientRpc]
        public static void SyncSuitClientRpc(ulong playerId, int suitId)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"SyncSuitClientRpc\".");
            UnlockableSuit.SwitchSuitForPlayer(Plugin.GetPlayerFromClientId(playerId), suitId);
        }

        [ClientRpc]
        public static void TerminalCodeClientRpc(NetworkObjectReference networkRef, int code)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TerminalCodeClientRpc\".");
            GetTerminalObjectByNetRef(networkRef)?.SetCodeTo(code);
        }

        [ClientRpc]
        public static void TerminalCreditsClientRpc(int val)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TerminalCreditsClientRpc\".");
            Terminal terminal = FindObjectOfType<Terminal>();
            if (terminal != null)
                terminal.groupCredits += val;
        }

        [ClientRpc]
        public static void TPGameObjectClientRpc(NetworkObjectReference networkRef, Vector3 position)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TPGameObjectClientRpc\".");
            EnemyAI foundEnemy = GetEnemyByNetRef(networkRef);
            GrabbableObject foundItem = GetItemByNetRef(networkRef);

            if (foundEnemy != null)
            {
                foundEnemy.agent.enabled = false;
                foundEnemy.transform.position = position;
                foundEnemy.agent.enabled = true;
                foundEnemy.serverPosition = position;
                foundEnemy.SetEnemyOutside(position.y > -100);  // Determine inside/outside based on position.y
            }
            else if (foundItem != null)
            {
                foundItem.transform.position = position;
                foundItem.startFallingPosition = position;
                if (foundItem.transform.parent != null)
                    foundItem.startFallingPosition = foundItem.transform.parent.InverseTransformPoint(foundItem.startFallingPosition);
                foundItem.FallToGround();
            }
        }

        [ClientRpc]
        public static void TPPlayerClientRpc(ulong playerId, Vector3 position)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TPPlayerClientRpc\".");
            PlayerControllerB player = Plugin.GetPlayerFromClientId(playerId);
            if (player == null)
                return;
            if (player.isPlayerDead)
            {
                DeadBodyInfo deadBody = StartOfRound.Instance.allPlayerScripts[playerId].deadBody;
                if (deadBody != null)
                {
                    deadBody.attachedTo = null;
                    deadBody.attachedLimb = null;
                    deadBody.secondaryAttachedLimb = null;
                    deadBody.secondaryAttachedTo = null;
                    if (deadBody.grabBodyObject != null && deadBody.grabBodyObject.isHeld && deadBody.grabBodyObject.playerHeldBy != null)
                        deadBody.grabBodyObject.playerHeldBy.DropAllHeldItems();
                    deadBody.transform.SetParent(null, true);
                    deadBody.SetRagdollPositionSafely(position, true);
                }
            }
            else
            {
                player.transform.position = position;
                Plugin.PlayerTeleportEffects(playerId, position.y < -100f);   // Determine inside/outside based on position.y
            }
        }

        public static DoorLock GetDoorByNetRef(NetworkObjectReference doorRef)
        {
            if (doorRef.TryGet(out NetworkObject netObj))
            {
                return netObj.GetComponentInChildren<DoorLock>();
            }
            else
            {
                return null;
            }
        }

        public static EnemyAI GetEnemyByNetId(ulong enemyNetId)
        {
            return FindObjectsOfType<EnemyAI>().FirstOrDefault(enemy => enemy.NetworkObjectId.Equals(enemyNetId));
        }

        public static EnemyAI GetEnemyByNetRef(NetworkObjectReference enemyNetRef)
        {
            if (enemyNetRef.TryGet(out NetworkObject netObj))
            {
                return netObj.GetComponent<EnemyAI>();
            }
            else
            {
                return null;
            }
        }

        public static GrabbableObject GetItemByNetId(ulong itemNetId)
        {
            return FindObjectsOfType<GrabbableObject>().FirstOrDefault(item => item.NetworkObjectId.Equals(itemNetId));
        }

        public static GrabbableObject GetItemByNetRef(NetworkObjectReference itemNetRef)
        {
            if (itemNetRef.TryGet(out NetworkObject netObj))
            {
                return netObj.GetComponent<GrabbableObject>();
            }
            else
            {
                return null;
            }
        }

        public static TerminalAccessibleObject GetTerminalObjectByNetRef(NetworkObjectReference terminalObjectNetRef)
        {
            if (terminalObjectNetRef.TryGet(out NetworkObject netObj))
            {
                return netObj.GetComponent<TerminalAccessibleObject>();
            }
            else
            {
                return null;
            }
        }
    }
}