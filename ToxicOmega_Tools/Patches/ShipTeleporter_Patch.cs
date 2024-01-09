using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(ShipTeleporter))]
    internal class ShipTeleporter_Patch
    {
        [HarmonyPatch("SetRandomSeed")]
        [HarmonyPostfix]
        private static void grabTeleporterSeed(ref System.Random ___shipTeleporterSeed)
        {
            Plugin.shipTeleporterSeed = ___shipTeleporterSeed;
        }

        [HarmonyPatch("OnDisable")]
        [HarmonyPostfix]
        private static void resetTeleporterSeed()
        {
            Plugin.shipTeleporterSeed = null;
        }
    }
}
