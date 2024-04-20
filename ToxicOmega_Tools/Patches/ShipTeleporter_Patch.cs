using HarmonyLib;
using System;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(ShipTeleporter))]
    internal class ShipTeleporter_Patch
    {
        [HarmonyPatch(nameof(ShipTeleporter.SetRandomSeed))]
        [HarmonyPostfix]
        private static void GrabTeleporterSeed(ref Random ___shipTeleporterSeed)
        {
            Plugin.shipTeleporterSeed = ___shipTeleporterSeed;
        }

        [HarmonyPatch(nameof(ShipTeleporter.OnDisable))]
        [HarmonyPostfix]
        private static void ResetTeleporterSeed()
        {
            Plugin.shipTeleporterSeed = null;
        }
    }
}
