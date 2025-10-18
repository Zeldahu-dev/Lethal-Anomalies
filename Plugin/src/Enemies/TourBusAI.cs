using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using DunGen;
using GameNetcodeStuff;
using HarmonyLib;
using LethalAnomalies.Configuration;
using LethalAnomalies.External;
using LethalLib.Modules;
using Unity.Netcode;
using UnityEngine;

namespace LethalAnomalies {

    class TourBusAI : EnemyAI
    {
        bool hasStartedExploding = false;
        enum State
        {
            Generic,
        }
        public override void Start()
        {
            base.Start();
            return;
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);
            if (IsServer && !hasStartedExploding)
            {
                hasStartedExploding = true;
                ExplosionClientRpc();
            }
        }

        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy = null!)
        {
            base.OnCollideWithEnemy(other, collidedEnemy);
            if (IsServer && !hasStartedExploding && Plugin.BoundConfig.CanMobsTriggerTourBus.Value)
            {
                hasStartedExploding = true;
                ExplosionClientRpc();
            }
        }

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null!, bool playHitSFX = true, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            if (playerWhoHit != null && playerWhoHit.playerClientId == GameNetworkManager.Instance.localPlayerController.playerClientId)
            {
                hasStartedExploding = true;
                ExplosionServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ExplosionServerRpc()
        {
            ExplosionClientRpc();
        }

        [ClientRpc]
        public void ExplosionClientRpc()
        {
            StartCoroutine(ExplosionCoroutine());
        }

        private IEnumerator ExplosionCoroutine()
        {
            UnityEngine.Debug.Log("Exploding tour bus");
            creatureAnimator.Play("ExplosionPriming");
            yield return new WaitForSeconds(5f);
            Landmine.SpawnExplosion(transform.position + new Vector3(0.0f, 3f, 0.0f), false, 30, 35, 50, 200, goThroughCar: true);
            if (IsServer)
            {
                foreach (TouristAI tourist in FindObjectsOfType<TouristAI>())
                {
                    tourist.RemoteExplode();
                }
            }
            yield break;
        }
    }
}