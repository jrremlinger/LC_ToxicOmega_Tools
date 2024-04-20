using GameNetcodeStuff;
using StaticNetcodeLib;
using Unity.Netcode;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [StaticNetcode]
    internal class Networking
    {
        [ClientRpc]
        public static void ChargePlayerClientRpc(ulong playerId)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"ChargePlayerClientRpc\".");
            PlayerControllerB playerTarget = Plugin.GetPlayerController(playerId);

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
        public static void GiveItemClientRpc(ulong playerId, ulong itemId)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"GiveItemClientRpc\".");
            PlayerControllerB player = Plugin.GetPlayerController(playerId);
            GrabbableObject item = Plugin.GetGrabbableObject(itemId);

            player.currentlyGrabbingObject = item;
            if (!GameNetworkManager.Instance.gameHasStarted && !item.itemProperties.canBeGrabbedBeforeGameStart && StartOfRound.Instance.testRoom == null)
                return;
            player.grabInvalidated = false;
            if (item == null || player.inSpecialInteractAnimation || item.isHeld || item.isPocketed)
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
            PlayerControllerB player = Plugin.GetPlayerController(playerId);

            player.sprintMeter = 100f;
            player.health = 100;
            player.DamagePlayer(-1);

            if (player != null && player.isPlayerDead)
                Plugin.RevivePlayer(player.playerClientId);

            if (player != null)
            {
                Plugin.SavePlayer(player);
                player.isExhausted = false;
                player.bleedingHeavily = false;
            }
        }

        [ClientRpc]
        public static void HurtPlayerClientRpc(ulong playerId, int damage)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"HurtPlayerClientRpc\".");
            Plugin.GetPlayerController(playerId).DamagePlayer(damage);
        }

        [ClientRpc]
        public static void SyncAmmoClientRpc(ulong itemId)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"SyncAmmoClientRpc\".");
            Plugin.GetGrabbableObject(itemId).GetComponentInChildren<ShotgunItem>().shellsLoaded = 2;
        }

        [ClientRpc]
        public static void SyncScrapValueClientRpc(ulong itemId, int scrapValue)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"SyncScrapValueClientRpc\".");
            Plugin.GetGrabbableObject(itemId).SetScrapValue(scrapValue);
        }

        [ClientRpc]
        public static void SyncSuitClientRpc(ulong playerId, int suitId)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"SyncSuitClientRpc\".");
            UnlockableSuit.SwitchSuitForPlayer(Plugin.GetPlayerController(playerId), suitId);
        }

        [ClientRpc]
        public static void TerminalCodeClientRpc(ulong networkId, int code)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TerminalCodeClientRpc\".");
            Plugin.GetTerminalAccessibleObject(networkId).SetCodeTo(code);
        }

        [ClientRpc]
        public static void TerminalCreditsClientRpc(int val)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TerminalCreditsClientRpc\".");
            Terminal terminal = Object.FindObjectOfType<Terminal>();

            if (terminal != null)
                terminal.groupCredits += val;
        }

        [ClientRpc]
        public static void TPGameObjectClientRpc(ulong networkId, Vector3 position)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TPGameObjectClientRpc\".");
            if (Plugin.GetEnemyAI(networkId) != null)
            {
                EnemyAI enemy = Plugin.GetEnemyAI(networkId);
                enemy.agent.enabled = false;
                enemy.transform.position = position;
                enemy.agent.enabled = true;
                enemy.serverPosition = position;
                enemy.SetEnemyOutside(position.y > -50);
            }
            else if (Plugin.GetGrabbableObject(networkId) != null)
            {
                GrabbableObject foundItem = Plugin.GetGrabbableObject(networkId);
                foundItem.transform.position = position;
                foundItem.startFallingPosition = position;

                if (foundItem.transform.parent != null)
                    foundItem.startFallingPosition = foundItem.transform.parent.InverseTransformPoint(foundItem.startFallingPosition);

                foundItem.FallToGround();
            }
        }

        [ClientRpc]
        public static void TPPlayerClientRpc(ulong playerId, Vector3 position, bool isInside)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TPPlayerClientRpc\".");
            Plugin.mls.LogInfo($"Found: {Plugin.GetPlayerController(playerId).playerUsername}, Sending Inside: {isInside}");

            PlayerControllerB player = Plugin.GetPlayerController(playerId);

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
                return;
            }

            player.transform.position = position;
            if (position.y >= -50)
            {
                isInside = false;
            }
            else if (position.y <= -100)
            {
                isInside = true;
            }

            Plugin.PlayerTeleportEffects(playerId, isInside);
        }
    }
}