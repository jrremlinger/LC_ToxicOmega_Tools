using GameNetcodeStuff;
using LethalNetworkAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.InputSystem.InputRemoting;

namespace ToxicOmega_Tools.Patches
{
    internal class TOTNetworking
    {
        public static LethalServerMessage<ulong> syncAmmoRPC = new LethalServerMessage<ulong>(identifier: "TOT_SYNC_AMMO");

        private static string hostVerifiedMessage = "RPC SENDER VERIFIED AS HOST, PROCEEDING WITH HANDLER METHOD.";
        private static string nonHostSenderMessage = "RPC SENDER IS NOT THE HOST, HANDLER METHOD CANCELLED.";


        public static void TOT_SYNC_AMMO(ulong data, ulong clientId)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_SYNC_AMMO\".");
            //PlayerControllerB playerSender = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender));
            PlayerControllerB playerSender = clientId.GetPlayerController();

            if (Plugin.CheckPlayerIsHost(playerSender))
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                //LC_API.GameInterfaceAPI.Features.Item.List.FirstOrDefault(item => item.NetworkObjectId.Equals(message.networkObjectID)).GetComponentInChildren<ShotgunItem>().shellsLoaded = 2;
                UnityEngine.Object.FindObjectsOfType<NetworkObject>().FirstOrDefault(item => item.NetworkObjectId.Equals(data)).GetComponentInChildren<ShotgunItem>().shellsLoaded = 2;
            }
            else
                Plugin.mls.LogInfo(nonHostSenderMessage);
        }



        //[NetworkMessage("TOT_CHARGE_PLAYER", true)]
        public static void TOT_CHARGE_PLAYER_HANDLER(ulong sender, TOT_PLAYER_Broadcast message)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_CHARGE_PLAYER\".");
            PlayerControllerB playerSender = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender));

            if (Plugin.CheckPlayerIsHost(playerSender))
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                PlayerControllerB playerTarget = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(message.playerClientId));
                GrabbableObject foundItem = playerTarget.ItemSlots[playerTarget.currentItemSlot];

                if (foundItem != null && foundItem.itemProperties.requiresBattery)
                    foundItem.insertedBattery.charge = 1f;
            }
            else
                Plugin.mls.LogInfo(nonHostSenderMessage);
        }

        //[NetworkMessage("TOT_HEAL_PLAYER", true)]
        public static void TOT_HEAL_PLAYER_HANDLER(ulong sender, TOT_PLAYER_Broadcast message)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_HEAL_PLAYER\".");
            PlayerControllerB playerSender = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender));

            if (Plugin.CheckPlayerIsHost(playerSender))
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                PlayerControllerB playerTarget = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(message.playerClientId));

                if (playerTarget != null && playerTarget.isPlayerDead)
                    Plugin.RevivePlayer(playerTarget.playerClientId);

                if (playerTarget != null)
                {
                    Plugin.SavePlayer(playerTarget);
                    playerTarget.isExhausted = false;
                    playerTarget.bleedingHeavily = false;
                }
            }
            else
                Plugin.mls.LogInfo(nonHostSenderMessage);
        }

        //[NetworkMessage("TOT_TERMINAL_CREDITS", true)]
        public static void TOT_TERMINAL_CREDITS_HANDLER(ulong sender, TOT_INT_Broadcast message)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_TERMINAL_CREDITS\".");
            PlayerControllerB playerSender = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender));

            if (Plugin.CheckPlayerIsHost(playerSender))
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();

                if (terminal != null)
                    terminal.groupCredits += message.dataInt;
            }
            else
                Plugin.mls.LogInfo(nonHostSenderMessage);
        }

        //[NetworkMessage("TOT_TP_PLAYER", true)]
        public static void TOT_TP_PLAYER_HANDLER(ulong sender, TOT_TP_PLAYER_Broadcast message)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_TP_PLAYER\".");
            PlayerControllerB playerSender = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender));

            if (Plugin.CheckPlayerIsHost(playerSender))
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                Plugin.mls.LogInfo($"Found: {StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender)).playerUsername}, Sending Inside: {message.isInside}");
                Plugin.PlayerTeleportEffects(message.playerClientId, message.isInside);
            }
            else
                Plugin.mls.LogInfo(nonHostSenderMessage);
        }

        //[NetworkMessage("TOT_SYNC_AMMO", true)]
        public static void TOT_SYNC_AMMO_HANDLER(ulong sender, TOT_ITEM_Broadcast message)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_SYNC_AMMO\".");
            PlayerControllerB playerSender = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(sender));

            if (Plugin.CheckPlayerIsHost(playerSender))
            {
                Plugin.mls.LogInfo(hostVerifiedMessage);
                //LC_API.GameInterfaceAPI.Features.Item.List.FirstOrDefault(item => item.NetworkObjectId.Equals(message.networkObjectID)).GetComponentInChildren<ShotgunItem>().shellsLoaded = 2;
            }
            else
                Plugin.mls.LogInfo(nonHostSenderMessage);
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