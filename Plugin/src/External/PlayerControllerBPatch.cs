using HarmonyLib;
using LethalAnomalies;
using LethalAnomalies.External;
using UnityEngine.Bindings;
using UnityEngine;
using GameNetcodeStuff;
using BepInEx.Logging;
using System.Collections.Generic;

namespace LethalAnomalies.External
{
    // [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        // [HarmonyPostfix]
        // [HarmonyPatch("SpawnPlayerAnimation")]
        // public static void SpawnPlayerAnimation(ref PlayerControllerB __instance)
        // {
        //     if (StartOfRound.Instance.connectedPlayersAmount == 0)
        //     {
        //         Object.Instantiate(Plugin.damageTypesHandler, __instance.transform);
        //     }
        // }
        // [HarmonyPostfix]
        // [HarmonyPatch("DamagePlayer")]
        // public static void DamagePlayer(ref PlayerControllerB __instance, int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default(Vector3))
        // {
        //     __instance.GetComponentInChildren<DamageTypeHandler>().OnPlayerDamaged(damageNumber, hasDamageSFX, callRPC, causeOfDeath, deathAnimation, fallDamage, force);
        // }
    }
}