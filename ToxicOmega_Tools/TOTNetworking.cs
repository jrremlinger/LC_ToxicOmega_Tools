using GameNetcodeStuff;
using OdinSerializer;
using StaticNetcodeLib;
using Unity.Netcode;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [StaticNetcode]
    internal class TOTNetworking
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
            Plugin.GetPlayerController(data.playerClientId).DamagePlayer(data.damage);
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
            Plugin.GetGrabbableObject(data.itemId).SetScrapValue(data.scrapValue);
        }

        [ClientRpc]
        public static void SyncSuitClientRpc(TOT_SyncSuitData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"SyncSuitClientRpc\".");
            UnlockableSuit.SwitchSuitForPlayer(Plugin.GetPlayerController(data.playerId), data.suitId);
        }

        [ClientRpc]
        public static void TerminalCodeClientRpc(TOT_TerminalCodeData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TerminalCodeClientRpc\".");
            Plugin.GetTerminalAccessibleObject(data.networkId).SetCodeTo(data.code);
        }

        [ClientRpc]
        public static void TerminalCreditsClientRpc(int val)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TerminalCreditsClientRpc\".");
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();

            if (terminal != null)
                terminal.groupCredits += val;
        }

        [ClientRpc]
        public static void TPGameObjectClientRpc(TOT_TPGameObjectData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TPGameObjectClientRpc\".");
            if (Plugin.GetEnemyAI(data.networkId) != null)
            {
                EnemyAI enemy = Plugin.GetEnemyAI(data.networkId);
                enemy.agent.enabled = false;
                enemy.transform.position = data.position;
                enemy.agent.enabled = true;
                enemy.serverPosition = data.position;
                enemy.SetEnemyOutside(data.position.y > -50);
            }
            else if (Plugin.GetGrabbableObject(data.networkId) != null)
            {
                GrabbableObject foundItem = Plugin.GetGrabbableObject(data.networkId);
                foundItem.transform.position = data.position;
                foundItem.startFallingPosition = data.position;
                if (foundItem.transform.parent != null)
                    foundItem.startFallingPosition = foundItem.transform.parent.InverseTransformPoint(foundItem.startFallingPosition);
                foundItem.FallToGround();
            }
        }

        [ClientRpc]
        public static void TPPlayerClientRpc(TOT_TPPlayerData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TPPlayerClientRpc\".");
            Plugin.mls.LogInfo($"Found: {Plugin.GetPlayerController(data.playerClientId).playerUsername}, Sending Inside: {data.isInside}");
            Plugin.GetPlayerController(data.playerClientId).transform.position = data.position;
            if (data.position.y >= -50)
                data.isInside = false;
            else if (data.position.y <= -100)
                data.isInside = true;
            Plugin.PlayerTeleportEffects(data.playerClientId, data.isInside);
        }

    }

    public struct TOT_DamagePlayerData
    {
        [OdinSerialize]
        public ulong playerClientId { get; set; }
        [OdinSerialize]
        public int damage { get; set; }
    }

    public struct TOT_SyncScrapData
    {
        [OdinSerialize]
        public ulong itemId { get; set; }
        [OdinSerialize]
        public int scrapValue { get; set; }
    }
    
    public struct TOT_SyncSuitData
    {
        [OdinSerialize]
        public ulong playerId { get; set; }
        [OdinSerialize]
        public int suitId { get; set; }
    }

    public struct TOT_TerminalCodeData
    {
        [OdinSerialize]
        public ulong networkId { get; set; }
        [OdinSerialize]
        public int code { get; set; }
    }

    public struct TOT_TPGameObjectData
    {
        [OdinSerialize]
        public ulong networkId { get; set; }
        [OdinSerialize]
        public Vector3 position { get; set; }
    }
    
    public struct TOT_TPPlayerData
    {
        [OdinSerialize]
        public bool isInside { get; set; }
        [OdinSerialize]
        public ulong playerClientId { get; set; }
        [OdinSerialize]
        public Vector3 position { get; set; }
    }

}