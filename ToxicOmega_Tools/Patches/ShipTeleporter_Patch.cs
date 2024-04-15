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
        [HarmonyPatch(nameof(ShipTeleporter.SetRandomSeed))]
        [HarmonyPostfix]
        private static void grabTeleporterSeed(ref Random ___shipTeleporterSeed)
        {
            Plugin.Instance.shipTeleporterSeed = ___shipTeleporterSeed;
        }

        [HarmonyPatch(nameof(ShipTeleporter.OnDisable))]
        [HarmonyPostfix]
        private static void resetTeleporterSeed()
        {
            Plugin.Instance.shipTeleporterSeed = null;
        }
    }
}
