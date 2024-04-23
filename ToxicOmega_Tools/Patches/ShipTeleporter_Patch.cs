using HarmonyLib;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(ShipTeleporter))]
    internal class ShipTeleporter_Patch : MonoBehaviour
    {
        [HarmonyPatch(nameof(ShipTeleporter.SetRandomSeed))]
        [HarmonyPostfix]
        private static void GrabTeleporterSeed(ref System.Random ___shipTeleporterSeed)
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
