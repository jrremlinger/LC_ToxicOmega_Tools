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
        private static string hostVerifiedMessage = "RPC SENDER VERIFIED AS HOST, PROCEEDING WITH HANDLER METHOD.";
        private static string nonHostSenderMessage = "RPC SENDER IS NOT THE HOST, HANDLER METHOD CANCELLED.";

        [NetworkMessage("TOT_CHARGE_PLAYER", true)]
        public static void TOT_CHARGE_PLAYER_HANDLER(ulong sender, TOT_PLAYER_Broadcast message)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_CHARGE_PLAYER\".");

            if (StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender)).IsHost)
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                PlayerControllerB playerTarget = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(message.playerClientId));
                GrabbableObject foundItem = playerTarget.ItemSlots[playerTarget.currentItemSlot];

                if (foundItem != null && foundItem.itemProperties.requiresBattery)
                {
                    foundItem.insertedBattery.charge = 1f;
                }
            }
            else
            {
                Plugin.mls.LogInfo(nonHostSenderMessage);
            }
        }

        [NetworkMessage("TOT_HEAL_PLAYER", true)]
        public static void TOT_HEAL_PLAYER_HANDLER(ulong sender, TOT_PLAYER_Broadcast message)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_HEAL_PLAYER\".");

            if (StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender)).IsHost)
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                PlayerControllerB playerTarget = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(message.playerClientId));

                if (playerTarget != null)
                {
                    Plugin.SavePlayer(playerTarget);
                    playerTarget.isExhausted = false;
                    playerTarget.bleedingHeavily = false;
                }
            }
            else
            {
                Plugin.mls.LogInfo(nonHostSenderMessage);
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
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_TERMINAL_CREDITS\".");

            if (StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender)).IsHost)
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();

                if (terminal != null)
                {
                    terminal.groupCredits += message.dataInt;
                }
                else
                {
                    Plugin.LogMessage("Terminal not found!", true);
                }
            }
            else
            {
                Plugin.mls.LogInfo(nonHostSenderMessage);
            }
        }

        [NetworkMessage("TOT_TP_PLAYER", true)]
        public static void TOT_TP_PLAYER_HANDLER(ulong sender, TOT_TP_PLAYER_Broadcast message)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_TP_PLAYER\".");

            if (StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender)).IsHost)
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                Plugin.PlayerTeleportEffects(StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(message.playerClientId)), message.isInside);
            }
            else
            {
                Plugin.mls.LogInfo(nonHostSenderMessage);
            }
        }

        [NetworkMessage("TOT_SYNC_AMMO", true)]
        public static void TOT_SYNC_AMMO_HANDLER(ulong sender, TOT_ITEM_Broadcast message)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_SYNC_AMMO\".");

            if (StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender)).IsHost)
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                LC_API.GameInterfaceAPI.Features.Item.List.FirstOrDefault(item => item.NetworkObjectId.Equals(message.networkObjectID)).GetComponentInChildren<ShotgunItem>().shellsLoaded = 2;
            }
            else
            {
                Plugin.mls.LogInfo(nonHostSenderMessage);
            }
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