using GameNetcodeStuff;
using LC_API.GameInterfaceAPI.Features;
using LC_API.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    internal class TOTNetworking
    {
        [NetworkMessage("TOT_CHARGE_PLAYER", true)]
        public static void TOT_CHARGE_PLAYER_HANDLER(ulong sender, TOT_PLAYER_Broadcast message)
        {
            PlayerControllerB playerTarget = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(message.playerClientId));
            GrabbableObject foundItem = playerTarget.ItemSlots[playerTarget.currentItemSlot];
            foundItem.insertedBattery.charge = 1f;
        }

        [NetworkMessage("TOT_HEAL_PLAYER", true)]
        public static void TOT_HEAL_PLAYER_HANDLER(ulong sender, TOT_PLAYER_Broadcast message)
        {
            PlayerControllerB playerTarget = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(message.playerClientId));

            if (playerTarget != null)
            {
                Plugin.SavePlayer(playerTarget);
                playerTarget.isExhausted = false;
                playerTarget.bleedingHeavily = false;
            }
        }

        //[NetworkMessage("TOT_SMITE_PLAYER", true)]
        //public static void TOT_SMITE_PLAYER_HANDLER(ulong sender, TOT_PLAYER_Broadcast message)
        //{
        //    PlayerControllerB playerTarget = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(message.playerClientId));
        //    Landmine.SpawnExplosion(playerTarget.transform.position + Vector3.up * 0.25f, killRange: 2.4f, damageRange: 5f);
        //}

        [NetworkMessage("TOT_TERMINAL_CREDITS", true)]
        public static void TOT_TERMINAL_CREDITS_HANDLER(ulong sender, TOT_INT_Broadcast message)
        {
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            terminal.groupCredits += message.dataInt;
        }

        [NetworkMessage("TOT_TP_PLAYER", true)]
        public static void TOT_TP_PLAYER_HANDLER(ulong sender, TOT_TP_PLAYER_Broadcast message)
        {
            Plugin.PlayerTeleportEffects(StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(message.playerClientId)), message.isInside);
        }

        [NetworkMessage("TOT_SYNC_AMMO", true)]
        public static void TOT_SYNC_AMMO_HANDLER(ulong sender, TOT_ITEM_Broadcast message)
        {
            LC_API.GameInterfaceAPI.Features.Item.List.FirstOrDefault(item => item.NetworkObjectId.Equals(message.networkObjectID)).GetComponentInChildren<ShotgunItem>().shellsLoaded = 2;
        }
    }

    internal class TOT_INT_Broadcast
    {
        public int dataInt { get; set; }
    }

    internal class TOT_ITEM_Broadcast
    {
        public ulong networkObjectID { get; set; }
    }

    internal class TOT_PLAYER_Broadcast
    {
        public ulong playerClientId { get; set; }
    }

    internal class TOT_TP_PLAYER_Broadcast
    {
        public bool isInside { get; set; }
        public ulong playerClientId { get; set; }
    }
}