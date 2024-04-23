using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(StartMatchLever))]
    internal class StartMatchLever_Patch : MonoBehaviour
    {
        [HarmonyPatch(nameof(StartMatchLever.StartGame))]
        [HarmonyPostfix]
        private static void LoadNewMoon()
        {
            Plugin.waypoints.Clear();

            // Manually destroy instantiated traps as they persist between moons
            foreach (GameObject obj in FindObjectsOfType<GameObject>().Where(obj => obj.name == "Landmine(Clone)" || obj.name == "TurretContainer(Clone)" || obj.name == "SpikeRoofTrapHazard(Clone)"))
            {
                Destroy(obj);
            }
        }
    }
}
