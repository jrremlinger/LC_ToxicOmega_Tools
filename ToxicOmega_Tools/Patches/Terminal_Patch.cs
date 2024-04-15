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
            Plugin.Instance.customOutsideList = new List<SpawnableEnemyWithRarity>();
            Plugin.Instance.customInsideList = new List<SpawnableEnemyWithRarity>();

            foreach (SelectableLevel moon in ___moonsCatalogueList)
            {
                foreach (SpawnableEnemyWithRarity daytimeEnemy in moon.DaytimeEnemies)
                {
                    if (!ListHasEnemy(Plugin.Instance.customOutsideList, daytimeEnemy.enemyType.enemyName))
                    {
                        Plugin.Instance.customOutsideList.Add(daytimeEnemy);
                    }
                }

                foreach (SpawnableEnemyWithRarity outsideEnemy in moon.OutsideEnemies)
                {
                    if (!ListHasEnemy(Plugin.Instance.customOutsideList, outsideEnemy.enemyType.enemyName))
                    {
                        Plugin.Instance.customOutsideList.Add(outsideEnemy);
                    }
                }

                foreach (SpawnableEnemyWithRarity insideEnemy in moon.Enemies)
                {
                    if (!ListHasEnemy(Plugin.Instance.customInsideList, insideEnemy.enemyType.enemyName))
                    {
                        Plugin.Instance.customInsideList.Add(insideEnemy);
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
