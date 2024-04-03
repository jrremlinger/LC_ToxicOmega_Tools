using GameNetcodeStuff;
using LethalNetworkAPI;
using OdinSerializer;
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
        public static LethalServerMessage<ulong> chargePlayerServerMessage = new LethalServerMessage<ulong>(identifier: "TOT_CHARGE_PLAYER");
        public static LethalClientMessage<ulong> chargePlayerClientMessage = new LethalClientMessage<ulong>(identifier: "TOT_CHARGE_PLAYER", onReceived: TOT_CHARGE_PLAYER);

        public static LethalServerMessage<ulong> healPlayerServerMessage = new LethalServerMessage<ulong>(identifier: "TOT_HEAL_PLAYER");
        public static LethalClientMessage<ulong> healPlayerClientMessage = new LethalClientMessage<ulong>(identifier: "TOT_HEAL_PLAYER", onReceived: TOT_HEAL_PLAYER);

        public static LethalServerMessage<ulong> syncAmmoServerMessage = new LethalServerMessage<ulong>(identifier: "TOT_SYNC_AMMO");
        public static LethalClientMessage<ulong> syncAmmoClientMessage = new LethalClientMessage<ulong>(identifier: "TOT_SYNC_AMMO", onReceived: TOT_SYNC_AMMO);

        public static LethalServerMessage<int> terminalCreditsServerMessage = new LethalServerMessage<int>(identifier: "TOT_TERMINAL_CREDITS");
        public static LethalClientMessage<int> terminalCreditsClientMessage = new LethalClientMessage<int>(identifier: "TOT_TERMINAL_CREDITS", onReceived: TOT_TERMINAL_CREDITS);

        public static LethalServerMessage<TOT_TP_PLAYER_Broadcast> TPPlayerServerMessage = new LethalServerMessage<TOT_TP_PLAYER_Broadcast>(identifier: "TOT_TP_PLAYER");
        public static LethalClientMessage<TOT_TP_PLAYER_Broadcast> TPPlayerClientMessage = new LethalClientMessage<TOT_TP_PLAYER_Broadcast>(identifier: "TOT_TP_PLAYER", onReceived: TOT_TP_PLAYER);


        //public static PlayerControllerB GetPlayerController(this ulong clientId)
        //{
        //    return StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[clientId]];
        //}

        //public static ulong GetClientId(this PlayerControllerB player)
        //{
        //    return player.actualClientId;
        //}



        private static void TOT_CHARGE_PLAYER(ulong data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_CHARGE_PLAYER\".");
            PlayerControllerB playerTarget = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(data));

            if (playerTarget != null)
            {
                GrabbableObject foundItem = playerTarget.ItemSlots[playerTarget.currentItemSlot];

                if (foundItem != null && foundItem.itemProperties.requiresBattery)
                    foundItem.insertedBattery.charge = 1f;
            }
        }

        private static void TOT_HEAL_PLAYER(ulong data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_HEAL_PLAYER\".");
            PlayerControllerB playerTarget = data.GetPlayerController();

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

        private static void TOT_SYNC_AMMO(ulong data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_SYNC_AMMO\".");
            UnityEngine.Object.FindObjectsOfType<GrabbableObject>().FirstOrDefault(item => item.NetworkObjectId.Equals(data)).GetComponentInChildren<ShotgunItem>().shellsLoaded = 2;
        }

        private static void TOT_TERMINAL_CREDITS(int data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_TERMINAL_CREDITS\".");
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();

            if (terminal != null)
                terminal.groupCredits += data;
        }

        private static void TOT_TP_PLAYER(TOT_TP_PLAYER_Broadcast data)
        {
            Plugin.mls.LogInfo("RPC RECEIVED: \"TOT_TP_PLAYER\".");
            Plugin.mls.LogInfo("DEST 2: " + data.pos);
            //Plugin.mls.LogInfo($"Found: {StartOfRound.Instance.allPlayerScripts.FirstOrDefault(player => player.playerClientId.Equals(data.playerClientId)).playerUsername}, Sending Inside: {data.isInside}");
            Plugin.mls.LogInfo($"Found: {data.playerClientId.GetPlayerController().playerUsername}, Sending Inside: {data.isInside}");
            data.playerClientId.GetPlayerController().transform.position = data.pos;
            Plugin.PlayerTeleportEffects(data.playerClientId, data.isInside);
        }

    }

    public struct TOT_TP_PLAYER_Broadcast
    {
        [OdinSerialize]
        public bool isInside { get; set; }
        [OdinSerialize]
        public ulong playerClientId { get; set; }
        [OdinSerialize]
        public Vector3 pos { get; set; }
    }
}