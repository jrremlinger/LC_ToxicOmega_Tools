using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class Terminal_Patch
    {
        [HarmonyPatch(nameof(Terminal.Start))]
        [HarmonyPostfix]
        static void GetAllEnemies(ref SelectableLevel[] ___moonsCatalogueList)
        {
            Plugin.customOutsideList = new List<SpawnableEnemyWithRarity>();
            Plugin.customInsideList = new List<SpawnableEnemyWithRarity>();

            foreach (SelectableLevel moon in ___moonsCatalogueList)
            {
                foreach (SpawnableEnemyWithRarity daytimeEnemy in moon.DaytimeEnemies)
                {
                    if (!ListHasEnemy(Plugin.customOutsideList, daytimeEnemy.enemyType.enemyName))
                    {
                        Plugin.customOutsideList.Add(daytimeEnemy);
                    }
                }

                foreach (SpawnableEnemyWithRarity outsideEnemy in moon.OutsideEnemies)
                {
                    if (!ListHasEnemy(Plugin.customOutsideList, outsideEnemy.enemyType.enemyName))
                    {
                        Plugin.customOutsideList.Add(outsideEnemy);
                    }
                }

                foreach (SpawnableEnemyWithRarity insideEnemy in moon.Enemies)
                {
                    if (!ListHasEnemy(Plugin.customInsideList, insideEnemy.enemyType.enemyName))
                    {
                        Plugin.customInsideList.Add(insideEnemy);
                    }
                }
            }
        }

        private static bool ListHasEnemy(List<SpawnableEnemyWithRarity> list, string enemyName)
        {
            return list.Any(e => e.enemyType.enemyName == enemyName);
        }
    }
}
