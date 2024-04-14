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
                    foundItem.insertedBattery.charge = 1f;
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
        public static void TerminalCreditsClientRpc(int val)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TerminalCreditsClientRpc\".");
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();

            if (terminal != null)
                terminal.groupCredits += val;
        }

        [ClientRpc]
        public static void TPItemClientRpc(TOT_TPItemData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TPItemClientRpc\".");
            GrabbableObject foundItem = Plugin.GetGrabbableObject(data.itemId);
            foundItem.transform.position = data.pos;
            foundItem.startFallingPosition = data.pos;
            if (foundItem.transform.parent != null)
                foundItem.startFallingPosition = foundItem.transform.parent.InverseTransformPoint(foundItem.startFallingPosition);
            foundItem.FallToGround();
        }

        [ClientRpc]
        public static void TPPlayerClientRpc(TOT_TPPlayerData data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TPPlayerClientRpc\".");
            Plugin.mls.LogInfo($"Found: {Plugin.GetPlayerController(data.playerClientId).playerUsername}, Sending Inside: {data.isInside}");
            Plugin.GetPlayerController(data.playerClientId).transform.position = data.pos;
            if (data.pos.y >= -50)
                data.isInside = false;
            else if (data.pos.y <= -100)
                data.isInside = true;
            Plugin.PlayerTeleportEffects(data.playerClientId, data.isInside);
        }

    }

    public struct TOT_TPItemData
    {
        [OdinSerialize]
        public ulong itemId { get; set; }
        [OdinSerialize]
        public Vector3 pos { get; set; }
    }

    public struct TOT_DamagePlayerData
    {
        [OdinSerialize]
        public ulong playerClientId { get; set; }
        [OdinSerialize]
        public int damage { get; set; }
    }

    public struct TOT_TPPlayerData
    {
        [OdinSerialize]
        public bool isInside { get; set; }
        [OdinSerialize]
        public ulong playerClientId { get; set; }
        [OdinSerialize]
        public Vector3 pos { get; set; }
    }

    public struct TOT_SyncScrapData
    {
        [OdinSerialize]
        public ulong itemId { get; set; }
        [OdinSerialize]
        public int scrapValue { get; set; }
    }

}