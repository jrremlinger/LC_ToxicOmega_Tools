using GameNetcodeStuff;
using HarmonyLib;
using LC_API.GameInterfaceAPI.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(StartMatchLever))]
    internal class StartMatchLever_Patch
    {
        [HarmonyPatch(nameof(StartMatchLever.StartGame))]
        [HarmonyPostfix]
        private static void resetWaypoints()
        {
            Plugin.waypoints.Clear();
        }
    }
}
