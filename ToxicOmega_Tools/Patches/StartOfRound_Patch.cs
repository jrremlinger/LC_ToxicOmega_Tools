using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRound_Patch
    {
        [HarmonyPatch(nameof(StartOfRound.Start))]
        [HarmonyPostfix]
        private static void RegisterHandlers()
        {
            Plugin.mls.LogInfo("TEST TEST TEST");
            TOTNetworking.syncAmmoRPC.OnReceived += TOTNetworking.TOT_SYNC_AMMO;
        }
    }
}
