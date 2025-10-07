using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using DunGen;
using GameNetcodeStuff;
using LethalAnomalies.Configuration;
using LethalAnomalies.External;
using LethalLib.Modules;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using DigitalRuby.ThunderAndLightning;

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
        Quaternion staticRotation = Quaternion.Euler(0f, 0f, 0f);
        public AudioSource attackAudio = null!;
        public AudioSource attackAudioFar = null!;
        public AudioSource warningAudio = null!;
        public Dictionary<AudioClip, int> talkieAnomalies = new Dictionary<AudioClip, int>();
        public List<AudioClip> talkieAnomaliesClips = new List<AudioClip>();
        public List<int> talkieAnomaliesWeights = new List<int>();
        public AudioClip threeNotesOddity = null!;
        public float timeSinceLastBroadcastAnomaly = 0f;
        public ParticleSystem warningParticles = null!;
        public ParticleSystem continuousWarningParticles = null!;
        public Transform lightningPosition = null!;
        public List<GameObject> spawnedLightningPrefabs = new List<GameObject>();
        enum State
        {
            DetectingPlayers,
            AttackAndCooldown,
        }
        // Config related values
        float effectiveRange = Plugin.BoundConfig.SparkTowerEffectiveRange.Value;
        float detectionThreshold = Plugin.BoundConfig.SparkTowerDetectionThreshold.Value;
        float cooldownMutiplier = Plugin.BoundConfig.SparkTowerCooldownMultiplier.Value;
        public GameObject colliders = null!;
        public List<PlayerControllerB> warnedPlayers = new List<PlayerControllerB>();
        bool altBehaviour = Plugin.BoundConfig.SparkTowerAltBehaviorEnabled.Value;
        int altAttackThreshold = Plugin.BoundConfig.SparkTowerAltAttackThreshold.Value;
        public List<PlayerControllerB> detectedPlayersList = new List<PlayerControllerB>();
        public Dictionary<PlayerControllerB, int> playerTargets = new Dictionary<PlayerControllerB, int>();
        public GameObject smallLightningLineRenderer = null!;
        public ParticleSystem altAttackBigExplosion = null!;
        public AudioSource altAttackAudio = null!;
        public AudioSource altAttackAudioFar = null!;
        public TemporaryAudioSource abibabouAudioSource = null!;
        public VisualEffectAsset smallLightningVFX = null!;

        [Conditional("DEBUG")]
        public void LogIfDebugBuild(string text)
        {
            Plugin.Logger.LogInfo(text);
        }
        public override void Start()
        {
            base.Start();
            staticPosition = transform.position;
            staticRotation = transform.rotation;
            warningAudio.maxDistance = effectiveRange + 10;
            for (int i = 0; i < talkieAnomaliesClips.Count; i++)
            {
                talkieAnomalies.Add(talkieAnomaliesClips[i], talkieAnomaliesWeights[i]);
            }
            LogIfDebugBuild("count : " + talkieAnomalies.Count);
            if (altBehaviour)
            {
                AIIntervalTime = 0.5f;
            }
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
                AudioClip chosenSound = null!;
                int maxweight = 0;
                foreach (AudioClip clip in talkieAnomalies.Keys)
                {
                    maxweight += talkieAnomalies[clip];
                }
                int randomPick = Random.RandomRangeInt(1, maxweight + 1);
                foreach (AudioClip clip in talkieAnomalies.Keys)
                {
                    randomPick -= talkieAnomalies[clip];
                    if (randomPick <= 0)
                    {
                        chosenSound = clip;
                        break;
                    }
                }
                if (chosenSound != null)
                {
                    for (int i = 0; i < WalkieTalkie.allWalkieTalkies.Count; i++)
                    {
                        if (Vector3.Distance(WalkieTalkie.allWalkieTalkies[i].transform.position, lightningPosition.position) < 30 && WalkieTalkie.allWalkieTalkies[i].isBeingUsed)
                        {
                            WalkieTalkie.allWalkieTalkies[i].target.PlayOneShot(chosenSound);
                        }
                    }
                    timeSinceLastBroadcastAnomaly = 0f;
                }
            }
            if (StartOfRound.Instance.shipIsLeaving && colliders.activeInHierarchy)
            {
                colliders.SetActive(false);
            }

        }
        public override void DoAIInterval()
        {

            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }
            ;

            switch (currentBehaviourStateIndex)
            {
                case (int)State.DetectingPlayers:
                    if (!altBehaviour)
                    {
                        float tempdistance = 0f;
                        detectedPlayersAmount = 0;
                        isAlreadyAttacking = false;
                        for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
                        {
                            PlayerControllerB tempPlayer = StartOfRound.Instance.allPlayerScripts[i];
                            tempdistance = Vector3.Distance(eye.transform.position, tempPlayer.transform.position);
                            if (!PlayerIsTargetable(tempPlayer, true) || tempdistance >= effectiveRange)
                            {
                                if (warnedPlayers.Contains(tempPlayer))
                                {
                                    warnedPlayers.Remove(tempPlayer);
                                }
                                continue;
                            }
                            detectedPlayersAmount += 1;
                            detectionValue += 0.05f;
                            if (TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Stormy)
                            {
                                detectionValue += 0.05f;
                            }
                            if (!warnedPlayers.Contains(tempPlayer))
                            {
                                SmallLightningClientRpc(tempPlayer.transform.position);
                                warnedPlayers.Add(tempPlayer);
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
                    }
                    else
                    {
                        AltAttackClientRpc();

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

            foreach (Vector3 position in lightningpositions)
            {
                ExternalScripts.SpawnLightningBolt(position, true, lightningPosition.position);
            }

            yield return new WaitForSeconds(attackCooldown);
            detectionValue = 0f;
            isWarningSoundReady = 0;
            SwitchToBehaviourClientRpc((int)State.DetectingPlayers);
        }

        [ClientRpc]
        private void AltAttackClientRpc()
        {
            detectedPlayersList.Clear();
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                PlayerControllerB tempPlayer = StartOfRound.Instance.allPlayerScripts[i];
                if (PlayerIsTargetable(tempPlayer, true) && Vector3.Distance(tempPlayer.transform.position + new Vector3(0.0f, 0.5f, 0.0f), eye.position) <= effectiveRange && !Physics.Linecast(eye.position, tempPlayer.transform.position + new Vector3(0f, 0.5f, 0f), StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
                {
                    detectedPlayersList.Add(tempPlayer);
                    if (!playerTargets.ContainsKey(tempPlayer))
                    {
                        playerTargets.Add(tempPlayer, 0);
                    }
                }
                else
                {
                    if (playerTargets.ContainsKey(tempPlayer))
                    {
                        playerTargets.Remove(tempPlayer);
                    }
                }

            }
            if (detectedPlayersList.Count == 0)
            {
                isWarningSoundReady = 1;
                continuousWarningParticles.Stop();
            }
            else
            {
                if (isWarningSoundReady == 1)
                {
                    continuousWarningParticles.Play();
                    warningAudio.Play();
                    isWarningSoundReady = 0;
                }
            }
                foreach (PlayerControllerB p in detectedPlayersList)
                {
                    playerTargets[p] += 1;
                    if (playerTargets[p] == 1)
                    {
                        for (int i = 0; i < spawnedLightningPrefabs.Count; i++)
                        {
                            Destroy(spawnedLightningPrefabs[i]);
                        }
                        spawnedLightningPrefabs.Clear();
                    }
                    if (playerTargets[p] == altAttackThreshold - 2)
                    {
                        altAttackBigExplosion.Play();
                        altAttackAudio.Play();
                        altAttackAudioFar.Play();
                    }
                    if (playerTargets[p] >= altAttackThreshold)
                    {
                        ExternalScripts.SpawnLightningBolt(p.transform.position, true, lightningPosition.position);
                        playerTargets[p] = 0;
                        for (int i = 0; i < spawnedLightningPrefabs.Count; i++)
                        {
                            Destroy(spawnedLightningPrefabs[i]);
                        }
                        spawnedLightningPrefabs.Clear();
                    }
                    else
                    {
                        foreach (PlayerControllerB player in detectedPlayersList)
                        {
                            SmallLightning(lightningPosition.position, player.transform.position);
                            Instantiate(abibabouAudioSource, player.transform.position, player.transform.rotation);
                        }
                    }
                }
        }

        [ClientRpc]
        private void SmallLightningClientRpc(Vector3 playerLocation)
        {
            SmallLightning(lightningPosition.position, playerLocation);
            Instantiate(abibabouAudioSource, playerLocation, new Quaternion(0.0f, 0.0f, 0.0f, 0.0f));
        }

        public void SmallLightning(Vector3 source, Vector3 strikePosition)
        {
            LightningBoltPrefabScript lightning = Instantiate(FindObjectOfType<StormyWeather>(true).targetedThunder);
            lightning.enabled = true;
            lightning.Camera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
            lightning.AutomaticModeSeconds = 0.2f;
            lightning.LightningTintColor = new Color(1f, 0.98f, 0.71f, 1f);
            lightning.GlowTintColor = new Color(1f, 0.98f, 0.71f, 1f);
            lightning.Generations = 2;
            lightning.GrowthMultiplier = 0.2f;
            lightning.Source.transform.position = source;
            lightning.Destination.transform.position = strikePosition;
            lightning.GlowIntensity = 4;
            lightning.Intensity = 10;
            lightning.LightParameters.LightIntensity = 0.2f;
            lightning.CreateLightningBoltsNow();
        }
    }
}