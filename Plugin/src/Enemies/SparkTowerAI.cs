using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using DunGen;
using GameNetcodeStuff;
using LethalAnomalies.Configuration;
using LethalAnomalies.External;
using Unity.Netcode;
using UnityEngine;

namespace LethalAnomalies {

    class SparkTowerAI : EnemyAI
    {
        public Transform turnCompass = null!;
        public Transform attackArea = null!;
        float detectionValue = 0f;
        int detectedPlayersAmount = 0;
        int isWarningSoundReady = 0;
        bool isAlreadyAttacking = false;
        Vector3 staticPosition = Vector3.zero;
        Quaternion staticRotation = Quaternion.Euler(0f,0f,0f);
        public AudioSource attackAudio = null!;
        public AudioSource attackAudioFar = null!;
        public AudioSource warningAudio = null!;
        public AudioClip threeNotesOddity = null!;
        public float timeSinceLastBroadcastAnomaly = 0f;
        public ParticleSystem warningParticles = null!;
        public Transform lightningPosition = null!;
        enum State {
            DetectingPlayers,
            AttackAndCooldown,
        }
        // Config related values
        float effectiveRange = Plugin.BoundConfig.SparkTowerEffectiveRange.Value;
        float detectionThreshold = Plugin.BoundConfig.SparkTowerDetectionThreshold.Value;
        float cooldownMutiplier = Plugin.BoundConfig.SparkTowerCooldownMultiplier.Value;
        public GameObject colliders = null!;

        [Conditional("DEBUG")]
        public void LogIfDebugBuild(string text) {
            Plugin.Logger.LogInfo(text);
        }
        public override void Start() {
            base.Start();
            staticPosition = transform.position;
            staticRotation = transform.rotation;
            warningAudio.maxDistance = effectiveRange + 10;
            SwitchToBehaviourClientRpc((int)State.DetectingPlayers);
        }

        public override void Update()
        {
            base.Update();
            transform.position = staticPosition;
            transform.rotation = staticRotation;
            timeSinceLastBroadcastAnomaly += Time.deltaTime;
            if (timeSinceLastBroadcastAnomaly > 15f)
            {
                for (int i = 0; i < WalkieTalkie.allWalkieTalkies.Count; i++)
                {
                    if (Vector3.Distance(WalkieTalkie.allWalkieTalkies[i].transform.position, lightningPosition.position) < 30 && WalkieTalkie.allWalkieTalkies[i].isBeingUsed)
                    {
                        WalkieTalkie.allWalkieTalkies[i].target.PlayOneShot(threeNotesOddity);
                    }
                }
                timeSinceLastBroadcastAnomaly = 0f;
            }
            if (StartOfRound.Instance.shipIsLeaving && colliders.activeInHierarchy)
            {
                colliders.SetActive(false);
            }
            
        }
        public override void DoAIInterval() {
            
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
                return;
            };

            switch(currentBehaviourStateIndex) {
                case (int)State.DetectingPlayers:
                    float tempdistance = 0f;
                    detectedPlayersAmount = 0;
                    isAlreadyAttacking = false;
                    for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
                    {
                        tempdistance = Vector3.Distance(eye.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
                        if (!PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i], true) || tempdistance >= effectiveRange)
                        {
                            continue;
                        }
                        detectedPlayersAmount += 1;
                        detectionValue += 0.05f;
                        if (TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Stormy)
                        {
                            detectionValue += 0.05f;
                        }
                        if (tempdistance >= effectiveRange / 2)
                        {
                            continue;
                        }
                        detectionValue += 0.05f;
                    }

                    if (detectedPlayersAmount > 0 && isWarningSoundReady == 0)
                    {
                        PlayWarningSequenceClientRPC();
                        isWarningSoundReady = 4;
                    }

                    if (detectedPlayersAmount == 0)
                    {
                        detectionValue = Mathf.Max(detectionValue - 0.1f, 0f);
                        isWarningSoundReady = Mathf.Max(isWarningSoundReady - 1, 0);
                    }

                    if (detectionValue > detectionThreshold)
                    {
                        SwitchToBehaviourClientRpc((int)State.AttackAndCooldown);  
                    }

                    break;

                case (int)State.AttackAndCooldown:
                    if (!isAlreadyAttacking)
                    {
                        isAlreadyAttacking = true;
                        float attackCooldown = Random.Range(3f, 10f) * cooldownMutiplier;
                        LogIfDebugBuild("Attack cooldown is set " + attackCooldown);
                        AttackClientRpc(attackCooldown);
                    }
                    break;
                    
                default:
                    LogIfDebugBuild("This Behavior State doesn't exist!");
                    break;
            }
        }

        [ClientRpc]
        private void PlayWarningSequenceClientRPC()
        {
            float randompitch = Random.Range(0.85f, 1.15f);
            warningAudio.pitch = randompitch;
            warningAudio.Play();
            warningParticles.Play();
        }

        [ClientRpc]
        private void AttackClientRpc(float attackCooldown)
        {
            StartCoroutine(AttackCoroutine(attackCooldown));
        }

        private IEnumerator AttackCoroutine(float attackCooldown)
        {
            creatureAnimator.Play("Attack");
            attackAudio.Play();
            attackAudioFar.Play();
            yield return new WaitForSeconds(5f);

            List<Vector3> lightningpositions = new List<Vector3>();
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i], true) && Vector3.Distance(eye.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) <= effectiveRange && !Physics.Linecast(lightningPosition.position, StartOfRound.Instance.allPlayerScripts[i].transform.position + new Vector3(0.0f, 0.5f, 0.0f), StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
                {
                    lightningpositions.Add(StartOfRound.Instance.allPlayerScripts[i].transform.position);
                }
            }

            yield return new WaitForSeconds(0.63f);

            MaskedPlayerEnemy[] array = FindObjectsOfType<MaskedPlayerEnemy>();
            for (int i = 0; i < array.Length; i++)
            {
                if (Vector3.Distance(eye.transform.position, array[i].transform.position) <= effectiveRange)
                {
                    lightningpositions.Add(array[i].transform.position);
                }
            }


            if (StartOfRound.Instance.inShipPhase)
            {
                yield break;
            }

            foreach(Vector3 position in lightningpositions)
            {
                ExternalScripts.SpawnLightningBolt(position, true, lightningPosition.position);
            }

            yield return new WaitForSeconds(attackCooldown);
            detectionValue = 0f;
            isWarningSoundReady = 0;
            SwitchToBehaviourClientRpc((int)State.DetectingPlayers);
        }
    }
}