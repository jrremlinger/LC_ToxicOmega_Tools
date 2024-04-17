using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(StartMatchLever))]
    internal class StartMatchLever_Patch
    {
        [HarmonyPatch(nameof(StartMatchLever.StartGame))]
        [HarmonyPostfix]
        private static void LoadNewMoon()
        {
            Plugin.Instance.waypoints.Clear();

            foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType<GameObject>().Where(obj => obj.name == "Landmine(Clone)" || obj.name == "TurretContainer(Clone)" || obj.name == "SpikeRoofTrapHazard(Clone)"))
            {
                UnityEngine.Object.Destroy(obj);
            }
        }
    }
}
