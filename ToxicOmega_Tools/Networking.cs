using GameNetcodeStuff;
using OdinSerializer;
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
        public static void HealPlayerClientRpc(ulong playerId)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"HealPlayerClientRpc\".");
            PlayerControllerB playerTarget = Plugin.GetPlayerController(playerId);

            playerTarget.sprintMeter = 100f;
            playerTarget.health = 100;
            playerTarget.DamagePlayer(-1);

            if (playerTarget != null && playerTarget.isPlayerDead)
                Plugin.RevivePlayer(playerTarget.playerClientId);

            if (playerTarget != null)
            {
                Plugin.SavePlayer(playerTarget);
                playerTarget.isExhausted = false;
                playerTarget.bleedingHeavily = false;
            }
        }

        [ClientRpc]
        public static void HurtPlayerClientRpc(TOT_DamagePlayerData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"HurtPlayerClientRpc\".");
            Plugin.GetPlayerController(data.PlayerClientId).DamagePlayer(data.Damage);
        }

        [ClientRpc]
        public static void SyncAmmoClientRpc(ulong itemId)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"SyncAmmoClientRpc\".");
            Plugin.GetGrabbableObject(itemId).GetComponentInChildren<ShotgunItem>().shellsLoaded = 2;
        }

        [ClientRpc]
        public static void SyncScrapClientRpc(TOT_SyncScrapData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"SyncScrapClientRpc\".");
            Plugin.GetGrabbableObject(data.ItemId).SetScrapValue(data.ScrapValue);
        }

        [ClientRpc]
        public static void SyncSuitClientRpc(TOT_SyncSuitData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"SyncSuitClientRpc\".");
            UnlockableSuit.SwitchSuitForPlayer(Plugin.GetPlayerController(data.PlayerId), data.SuitId);
        }

        [ClientRpc]
        public static void TerminalCodeClientRpc(TOT_TerminalCodeData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TerminalCodeClientRpc\".");
            Plugin.GetTerminalAccessibleObject(data.NetworkId).SetCodeTo(data.Code);
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
        public static void TPGameObjectClientRpc(TOT_TPGameObjectData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TPGameObjectClientRpc\".");
            if (Plugin.GetEnemyAI(data.NetworkId) != null)
            {
                EnemyAI enemy = Plugin.GetEnemyAI(data.NetworkId);
                enemy.agent.enabled = false;
                enemy.transform.position = data.Position;
                enemy.agent.enabled = true;
                enemy.serverPosition = data.Position;
                enemy.SetEnemyOutside(data.Position.y > -50);
            }
            else if (Plugin.GetGrabbableObject(data.NetworkId) != null)
            {
                GrabbableObject foundItem = Plugin.GetGrabbableObject(data.NetworkId);
                foundItem.transform.position = data.Position;
                foundItem.startFallingPosition = data.Position;

                if (foundItem.transform.parent != null)
                    foundItem.startFallingPosition = foundItem.transform.parent.InverseTransformPoint(foundItem.startFallingPosition);

                foundItem.FallToGround();
            }
        }

        [ClientRpc]
        public static void TPPlayerClientRpc(TOT_TPPlayerData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TPPlayerClientRpc\".");
            Plugin.mls.LogInfo($"Found: {Plugin.GetPlayerController(data.PlayerClientId).playerUsername}, Sending Inside: {data.IsInside}");

            PlayerControllerB player = Plugin.GetPlayerController(data.PlayerClientId);

            if (player.isPlayerDead)
            {
                DeadBodyInfo deadBody = StartOfRound.Instance.allPlayerScripts[data.PlayerClientId].deadBody;
                if (deadBody != null)
                {
                    deadBody.attachedTo = null;
                    deadBody.attachedLimb = null;
                    deadBody.secondaryAttachedLimb = null;
                    deadBody.secondaryAttachedTo = null;

                    if (deadBody.grabBodyObject != null && deadBody.grabBodyObject.isHeld && deadBody.grabBodyObject.playerHeldBy != null)
                        deadBody.grabBodyObject.playerHeldBy.DropAllHeldItems();

                    deadBody.transform.SetParent(null, true);
                    deadBody.SetRagdollPositionSafely(data.Position, true);
                }
                return;
            }

            player.transform.position = data.Position;
            if (data.Position.y >= -50)
            {
                data.IsInside = false;
            }
            else if (data.Position.y <= -100)
            {
                data.IsInside = true;
            }

            Plugin.PlayerTeleportEffects(data.PlayerClientId, data.IsInside);
        }

    }

    public struct TOT_DamagePlayerData
    {
        [OdinSerialize]
        public ulong PlayerClientId { get; set; }
        [OdinSerialize]
        public int Damage { get; set; }
    }

    public struct TOT_SyncScrapData
    {
        [OdinSerialize]
        public ulong ItemId { get; set; }
        [OdinSerialize]
        public int ScrapValue { get; set; }
    }

    public struct TOT_SyncSuitData
    {
        [OdinSerialize]
        public ulong PlayerId { get; set; }
        [OdinSerialize]
        public int SuitId { get; set; }
    }

    public struct TOT_TerminalCodeData
    {
        [OdinSerialize]
        public ulong NetworkId { get; set; }
        [OdinSerialize]
        public int Code { get; set; }
    }

    public struct TOT_TPGameObjectData
    {
        [OdinSerialize]
        public ulong NetworkId { get; set; }
        [OdinSerialize]
        public Vector3 Position { get; set; }
    }

    public struct TOT_TPPlayerData
    {
        [OdinSerialize]
        public bool IsInside { get; set; }
        [OdinSerialize]
        public ulong PlayerClientId { get; set; }
        [OdinSerialize]
        public Vector3 Position { get; set; }
    }

}