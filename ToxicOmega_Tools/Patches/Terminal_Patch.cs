using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

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
            Plugin.allEnemiesList = new List<SpawnableEnemyWithRarity>();

            foreach (SelectableLevel moon in ___moonsCatalogueList)
            {
                foreach (SpawnableEnemyWithRarity daytimeEnemy in moon.DaytimeEnemies)
                {
                    if (!ListHasEnemy(Plugin.customOutsideList, daytimeEnemy.enemyType.enemyName))
                        Plugin.customOutsideList.Add(daytimeEnemy);
                }

                foreach (SpawnableEnemyWithRarity outsideEnemy in moon.OutsideEnemies)
                {
                    if (!ListHasEnemy(Plugin.customOutsideList, outsideEnemy.enemyType.enemyName))
                        Plugin.customOutsideList.Add(outsideEnemy);
                }

                foreach (SpawnableEnemyWithRarity insideEnemy in moon.Enemies)
                {
                    if (!ListHasEnemy(Plugin.customInsideList, insideEnemy.enemyType.enemyName))
                        Plugin.customInsideList.Add(insideEnemy);
                }
            }

            Plugin.allEnemiesList.AddRange(Plugin.customOutsideList);
            Plugin.allEnemiesList.AddRange(Plugin.customInsideList);
            SetupSpawnablesList();
        }

        private static bool ListHasEnemy(List<SpawnableEnemyWithRarity> list, string enemyName)
        {
            return list.Any(e => e.enemyType.enemyName == enemyName);
        }

        private static void SetupSpawnablesList()
        {
            List<Item> itemList = StartOfRound.Instance.allItemsList.itemsList;
            Plugin.allSpawnablesList = new List<SearchableGameObject>();

            foreach (Item obj in itemList)
            {
                Plugin.allSpawnablesList.Add(new SearchableGameObject { Name = obj.itemName, Id = itemList.IndexOf(obj), IsItem = true });
            }

            foreach (SpawnableEnemyWithRarity obj in Plugin.customInsideList)
            {
                Plugin.allSpawnablesList.Add(new SearchableGameObject { Name = obj.enemyType.enemyName, Id = Plugin.customInsideList.IndexOf(obj), IsEnemy = true });
            }

            foreach (SpawnableEnemyWithRarity obj in Plugin.customOutsideList)
            {
                Plugin.allSpawnablesList.Add(new SearchableGameObject { Name = obj.enemyType.enemyName, Id = Plugin.customOutsideList.IndexOf(obj), IsEnemy = true, IsOutsideEnemy = true });
            }

            Plugin.allSpawnablesList.Add(new SearchableGameObject { Name = "Mine", Id = 0, IsTrap = true });
            Plugin.allSpawnablesList.Add(new SearchableGameObject { Name = "LandMine", Id = 0, IsTrap = true });
            Plugin.allSpawnablesList.Add(new SearchableGameObject { Name = "Turret", Id = 1, IsTrap = true });
            Plugin.allSpawnablesList.Add(new SearchableGameObject { Name = "Spikes", Id = 2, IsTrap = true });
            Plugin.allSpawnablesList.Add(new SearchableGameObject { Name = "RoofSpikes", Id = 2, IsTrap = true });
            Plugin.allSpawnablesList.Add(new SearchableGameObject { Name = "CeilingSpikes", Id = 2, IsTrap = true });
        }
    }
}
