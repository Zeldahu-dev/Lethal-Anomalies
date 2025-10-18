using HarmonyLib;
using LethalAnomalies;
using LethalAnomalies.External;
using UnityEngine.Bindings;
using UnityEngine;
using GameNetcodeStuff;
using BepInEx.Logging;

namespace LethalAnomalies.External
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnShipLandedMiscEvents")]
        public static void OnShipLandedMiscEventsPatch(ref StartOfRound __instance)
        {
            if (!__instance.IsServer || GetEnemies.SparkTower == null)
            {
                return;
            }
            EnemyAINestSpawnObject[] array = Object.FindObjectsByType<EnemyAINestSpawnObject>(FindObjectsSortMode.None);
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].enemyType == GetEnemies.SparkTower.enemyType)
                {
                    RoundManager.Instance.SpawnEnemyGameObject(RoundManager.Instance.outsideAINodes[0].transform.position, 0f, -1, GetEnemies.SparkTower.enemyType);
                }
            }
        }

        // [HarmonyPostfix]
        // [HarmonyPatch("OnPlayerConnectedClientRpc")]
        // public static void OnPlayerConnectedClientRpc(ref StartOfRound __instance)
        // {
        //     PlayerControllerB[] array = Object.FindObjectsByType<PlayerControllerB>(FindObjectsSortMode.None);
        //     for (int i = 0; i < array.Length; i++)
        //     {
        //         if (array[i].transform.Find("DamageTypesHandler") == null)
        //         {
        //             Object.Instantiate(Plugin.damageTypesHandler, array[i].transform);
        //         }
        //     }
        // }
    }
}