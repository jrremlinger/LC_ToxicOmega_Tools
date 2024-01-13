using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        [HarmonyPatch(nameof(RoundManager.LoadNewLevel))]
        [HarmonyPrefix]
        static void ModifyLevel(ref SelectableLevel newLevel)
        {
            Plugin.Instance.customOutsideList.AddRange(newLevel.DaytimeEnemies);
            Plugin.Instance.customOutsideList.AddRange(newLevel.OutsideEnemies);

            foreach (SpawnableEnemyWithRarity enemy in FixOutsideEnemySpawns())
            {
                if (!ListHasEnemy(Plugin.Instance.customOutsideList, enemy.enemyType.enemyName))
                    Plugin.Instance.customOutsideList.Add(enemy);
            }

            Plugin.Instance.customInsideList.AddRange(newLevel.Enemies);

            foreach (SpawnableEnemyWithRarity enemy in FixInsideEnemySpawns())
            {
                if (!ListHasEnemy(Plugin.Instance.customInsideList, enemy.enemyType.enemyName))
                    Plugin.Instance.customInsideList.Add(enemy);
            }

            // if allow all enemies is true
            //newLevel.Enemies = TutorialModBase.Instance.InsideEnemyList;
            //newLevel.OutsideEnemies = TutorialModBase.Instance.OutsideEnemyList;
        }

        private static bool ListHasEnemy(List<SpawnableEnemyWithRarity> list, string enemyName)
        {
            return list.Any(e => e.enemyType.enemyName == enemyName);
        }

        private static List<SpawnableEnemyWithRarity> FixInsideEnemySpawns()
        {
            List<SpawnableEnemyWithRarity> returnList = new List<SpawnableEnemyWithRarity>();

            BlobAI blobRef = null;
            CentipedeAI centipedeRef = null;
            CrawlerAI crawlerRef = null;
            DressGirlAI dressGirlRef = null;
            FlowermanAI flowermanRef = null;
            HoarderBugAI hoarderRef = null;
            JesterAI jesterRef = null;
            LassoManAI lassoRef = null;
            MaskedPlayerEnemy maskedRef = null;
            NutcrackerEnemyAI nutRef = null;
            PufferAI pufferRef = null;
            SandSpiderAI spiderRef = null;
            SpringManAI springRef = null;

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(BlobAI)))
                blobRef = (BlobAI)obj;
            SpawnableEnemyWithRarity blobSpawnable = new SpawnableEnemyWithRarity();
            if (blobRef != null)
                blobSpawnable.enemyType = blobRef.enemyType;
            returnList.Add(blobSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(CentipedeAI)))
                centipedeRef = (CentipedeAI)obj;
            SpawnableEnemyWithRarity centipedeSpawnable = new SpawnableEnemyWithRarity();
            if (centipedeRef != null)
                centipedeSpawnable.enemyType = centipedeRef.enemyType;
            returnList.Add(centipedeSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(CrawlerAI)))
                crawlerRef = (CrawlerAI)obj;
            SpawnableEnemyWithRarity crawlerSpawnable = new SpawnableEnemyWithRarity();
            if (crawlerRef != null)
                crawlerSpawnable.enemyType = crawlerRef.enemyType;
            returnList.Add(crawlerSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(DressGirlAI)))
                dressGirlRef = (DressGirlAI)obj;
            SpawnableEnemyWithRarity dressGirlSpawnable = new SpawnableEnemyWithRarity();
            if (dressGirlRef != null)
                dressGirlSpawnable.enemyType = dressGirlRef.enemyType;
            returnList.Add(dressGirlSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(FlowermanAI)))
                flowermanRef = (FlowermanAI)obj;
            SpawnableEnemyWithRarity flowermanSpawnable = new SpawnableEnemyWithRarity();
            if (flowermanRef != null)
                flowermanSpawnable.enemyType = flowermanRef.enemyType;
            returnList.Add(flowermanSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(HoarderBugAI)))
                hoarderRef = (HoarderBugAI)obj;
            SpawnableEnemyWithRarity hoarderSpawnable = new SpawnableEnemyWithRarity();
            if (hoarderRef != null)
                hoarderSpawnable.enemyType = hoarderRef.enemyType;
            returnList.Add(hoarderSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(JesterAI)))
                jesterRef = (JesterAI)obj;
            SpawnableEnemyWithRarity jesterSpawnable = new SpawnableEnemyWithRarity();
            if (jesterRef != null)
                jesterSpawnable.enemyType = jesterRef.enemyType;
            returnList.Add(jesterSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(LassoManAI)))
                lassoRef = (LassoManAI)obj;
            SpawnableEnemyWithRarity lassoSpawnable = new SpawnableEnemyWithRarity();
            if (lassoRef != null)
                lassoSpawnable.enemyType = lassoRef.enemyType;
            returnList.Add(lassoSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(MaskedPlayerEnemy)))
                maskedRef = (MaskedPlayerEnemy)obj;
            SpawnableEnemyWithRarity maskedSpawnable = new SpawnableEnemyWithRarity();
            if (maskedRef != null)
                maskedSpawnable.enemyType = maskedRef.enemyType;
            returnList.Add(maskedSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(NutcrackerEnemyAI)))
                nutRef = (NutcrackerEnemyAI)obj;
            SpawnableEnemyWithRarity nutSpawnable = new SpawnableEnemyWithRarity();
            if (nutRef != null)
                nutSpawnable.enemyType = nutRef.enemyType;
            returnList.Add(nutSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(PufferAI)))
                pufferRef = (PufferAI)obj;
            SpawnableEnemyWithRarity pufferSpawnable = new SpawnableEnemyWithRarity();
            if (pufferRef != null)
                pufferSpawnable.enemyType = pufferRef.enemyType;
            returnList.Add(pufferSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(SandSpiderAI)))
                spiderRef = (SandSpiderAI)obj;
            SpawnableEnemyWithRarity spiderSpawnable = new SpawnableEnemyWithRarity();
            if (spiderRef != null)
                spiderSpawnable.enemyType = spiderRef.enemyType;
            returnList.Add(spiderSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(SpringManAI)))
                springRef = (SpringManAI)obj;
            SpawnableEnemyWithRarity springSpawnable = new SpawnableEnemyWithRarity();
            if (springRef != null)
                springSpawnable.enemyType = springRef.enemyType;
            returnList.Add(springSpawnable);

            return returnList;
        }

        private static List<SpawnableEnemyWithRarity> FixOutsideEnemySpawns()
        {
            List<SpawnableEnemyWithRarity> returnList = new List<SpawnableEnemyWithRarity>();

            BaboonBirdAI baboonRef = null;
            DocileLocustBeesAI docileLocustRef = null;
            DoublewingAI doublewingRef = null;
            ForestGiantAI forestGiantRef = null;
            MouthDogAI mouthDogRef = null;
            RedLocustBees redLocustRef = null;
            SandWormAI sandWormRef = null;

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(BaboonBirdAI)))
                baboonRef = (BaboonBirdAI)obj;
            SpawnableEnemyWithRarity baboonSpawnable = new SpawnableEnemyWithRarity();
            if (baboonRef != null)
                baboonSpawnable.enemyType = baboonRef.enemyType;
            returnList.Add(baboonSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(DocileLocustBeesAI)))
                docileLocustRef = (DocileLocustBeesAI)obj;
            SpawnableEnemyWithRarity docileLocustSpawnable = new SpawnableEnemyWithRarity();
            if (docileLocustRef != null)
                docileLocustSpawnable.enemyType = docileLocustRef.enemyType;
            returnList.Add(docileLocustSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(DoublewingAI)))
                doublewingRef = (DoublewingAI)obj;
            SpawnableEnemyWithRarity doublewingSpawnable = new SpawnableEnemyWithRarity();
            if (doublewingRef != null)
                doublewingSpawnable.enemyType = doublewingRef.enemyType;
            returnList.Add(doublewingSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(ForestGiantAI)))
                forestGiantRef = (ForestGiantAI)obj;
            SpawnableEnemyWithRarity forestGiantSpawnable = new SpawnableEnemyWithRarity();
            if (forestGiantRef != null)
                forestGiantSpawnable.enemyType = forestGiantRef.enemyType;
            returnList.Add(forestGiantSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(MouthDogAI)))
                mouthDogRef = (MouthDogAI)obj;
            SpawnableEnemyWithRarity mouthDogSpawnable = new SpawnableEnemyWithRarity();
            if (mouthDogRef != null)
                mouthDogSpawnable.enemyType = mouthDogRef.enemyType;
            returnList.Add(mouthDogSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(RedLocustBees)))
                redLocustRef = (RedLocustBees)obj;
            SpawnableEnemyWithRarity redLocustSpawnable = new SpawnableEnemyWithRarity();
            if (redLocustRef != null)
                redLocustSpawnable.enemyType = redLocustRef.enemyType;
            returnList.Add(redLocustSpawnable);

            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(SandWormAI)))
                sandWormRef = (SandWormAI)obj;
            SpawnableEnemyWithRarity sandWormSpawnable = new SpawnableEnemyWithRarity();
            if (sandWormRef != null)
                sandWormSpawnable.enemyType = sandWormRef.enemyType;
            returnList.Add(sandWormSpawnable);

            return returnList;
        }
    }
}
