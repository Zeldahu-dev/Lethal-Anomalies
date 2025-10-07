using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using DunGen;
using GameNetcodeStuff;
using LethalAnomalies.Configuration;
using LethalAnomalies.External;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UIElements;
namespace LethalAnomalies {

    class TouristAI : EnemyAI
    {
        public class ObjectInterest
        {
            public float interestValue = 2;
            public int objectType = 0;
            public float defaultInterestValue = 2;
            public bool isBrightWhenPocketed = false;

            public ObjectInterest(float interestValue, int objectType = 0, float defaultInterestValue = 2, bool isBrightWhenPocketed = false)
            {
                this.interestValue = interestValue;
                this.objectType = objectType;
                this.defaultInterestValue = defaultInterestValue;
                this.isBrightWhenPocketed = isBrightWhenPocketed;
            }
        }

//This dictionary stores the interest values for bright items. It ranges from 0 to 2, with 0 being brightest (will always be picked) and 2 least interesting.
//Players get assigned a value of 1, unless they are holding an item with a lower value in their hands
//The second number determines how the code handles the brightness of an object if it is conditional
//0 / none - No conditions | 1 - Checks "isEnabled" | 2 - For the radar booster, checks "isRadarEnabled"
//Third number is the alternate value if the condition fails
//Last argument determines whether an item is still targetable when not held in hands, is used for items that still emit light when pocketed
        public Dictionary<string, ObjectInterest> grabbableObjectInterest = new Dictionary<string, ObjectInterest>(){
            //Vanilla
            {"Apparatus", new ObjectInterest(0.4f)},
            {"Fancy lamp", new ObjectInterest(0.7f, 1)},
            {"Flashlight", new ObjectInterest(0.8f, 1, 2, true)},
            {"Radar-booster", new ObjectInterest(0.9f, 2)},
            {"Rubber Ducky", new ObjectInterest(0.5f, 0, 2, true)},
            {"Pro-flashlight", new ObjectInterest(0.5f, 1, 2, true)},
            //Premium Scraps
            {"Ainz Ooal Gown", new ObjectInterest(1f)},
            {"Mystic Cristal", new ObjectInterest(0.6f, 0, 2, true)},
            {"Balan Statue", new ObjectInterest(1.5f)},
            {"The talking orb", new ObjectInterest(0.85f)},
            //Chillax Scraps
            {"Nokia", new ObjectInterest(1.7f, 1, 2)},
            {"Uno Reverse Card DX", new ObjectInterest(1.1f, 0, 2, true)},
            //Emergency Dice
            {"Emergency Die", new ObjectInterest(1.5f)},
            {"Gambler", new ObjectInterest(1.3f)},
            {"The Saint", new ObjectInterest(1.9f)},
            //Legend Weathers
            {"Moon's Tear", new ObjectInterest(0.6f, 0, 2, true)}
        };
        public List<int> checkedItems = new List<int>();
        public Transform feet = null!;
        public KeyValuePair<Transform, float> bestTarget = new KeyValuePair<Transform, float>();
        public Transform currentTarget = null!;
        public bool isBeingLookedAt = false;
        public float currentTargetInterest = 0;
        public float losingInterestTimer = 0;
        public float timeSpentMoving = 0;
        public float timeSpentStalking = 0;
        public float adhdValue = 0;
        public float stalkAdhdValue = 0;
        public float walkSpeed = 12f;
        public bool hasChangedPose = false;
        public int currentPose = 0;
        public bool isAlreadyAttacking = false;
        public List<GameObject> posesList = new List<GameObject>();
        public List<GameObject> boxEdgesList = new List<GameObject>();
        public GameObject collisionBox = null!;
        public Light chestLight = null!;
        // 1500 when blowing up
        public float chestLightIntensity = 0;
        public bool isNaturallySpawned = true;
        enum State {
            Roaming,
            Reaching,
            Stalking,
        }
        // Config related values
        float detectionRange = Plugin.BoundConfig.TouristDetectionRange.Value;
        [Conditional("DEBUG")]
        public void LogIfDebugBuild(string text) {
            Plugin.Logger.LogInfo(text);
        }
        public override void Start() {
            base.Start();
            if(IsServer)
            {
                adhdValue = Random.Range(4, 8);
                stalkAdhdValue = Random.Range(25, 35);
                walkSpeed = Random.Range(12f, 15f) * Plugin.BoundConfig.TouristSpeed.Value;
                StartCoroutine(NaturalSpawnCoroutine());
                int randomPose = Random.RandomRangeInt(0, 7);
                ChangePoseClientRPC(randomPose);
            }
            creatureAnimator.Play("Spawn");
            SwitchToBehaviourClientRpc((int)State.Roaming);
        }

        public override void DoAIInterval() {
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
                return;
            };
            switch(currentBehaviourStateIndex) {
                case (int)State.Roaming:
                    if (!currentSearch.inProgress)
                    {
                        StartSearch(transform.position);
                    }
                    
                    ScanForTargets();
                    if (bestTarget.Key != null)
                    {
                        currentTarget = bestTarget.Key;
                        currentTargetInterest = bestTarget.Value;
                        timeSpentMoving = 0;
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.Reaching);
                    }
                    break;
                case (int)State.Reaching:

                    //is the item still in sight?
                    if (Physics.Linecast(eye.transform.position, currentTarget.position + Vector3.up * 0.5f, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
                    {
                        losingInterestTimer += AIIntervalTime;
                        if (losingInterestTimer >= adhdValue)
                        {
                            SwitchToBehaviourClientRpc((int)State.Roaming);
                        }

                    }
                    else
                    {
                        losingInterestTimer = 0;
                    }
                    
                    //how long has the tourist been moving without reaching its target?
                    if (timeSpentMoving >= 30f)
                    {
                        SwitchToBehaviourClientRpc((int)State.Roaming);
                    }

                    //Is the tourist distracted by another object?
                    ScanForTargets();
                    if (bestTarget.Key != currentTarget && bestTarget.Value <= currentTargetInterest * 0.75f)
                    {
                        currentTarget = bestTarget.Key;
                        currentTargetInterest = bestTarget.Value;
                    }

                    SetDestinationToPosition(currentTarget.position);

                    //Has the tourist reached its target?
                    if (Vector3.Distance(transform.position, currentTarget.position) <= 7f)
                    {
                        SetDestinationToPosition(transform.position);
                        timeSpentStalking = 0f;
                        chestLightIntensity = 3;
                        SwitchToBehaviourClientRpc((int)State.Stalking);
                    }

                    break;
                    
                case (int)State.Stalking:

                    float distanceToTarget = Vector3.Distance(currentTarget.position, transform.position);
                    if (distanceToTarget >= 12f)
                    {
                        SetDestinationToPosition(currentTarget.position);
                    }
                    if (distanceToTarget <= 7f)
                    {
                        SetDestinationToPosition(transform.position);
                    }

                    timeSpentStalking += AIIntervalTime;

                    if (timeSpentStalking >= 30f)
                    {
                        if (checkedItems.Contains(currentTarget.gameObject.GetInstanceID()))
                        {
                            LogIfDebugBuild("ERROR : checkedItems already contained item " + currentTarget.gameObject + ", ID " + currentTarget.gameObject.GetInstanceID() + "!");
                        }
                        else
                        {
                            checkedItems.Add(currentTarget.gameObject.GetInstanceID());
                        }
                        chestLightIntensity = 0;
                        SwitchToBehaviourClientRpc((int)State.Roaming);
                    }
                    break;
                default:
                    LogIfDebugBuild("This Behavior State doesn't exist!");
                    break;
            }
            base.DoAIInterval();
        }
        public override void Update()
        {
            base.Update();
            isBeingLookedAt = false;
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                PlayerControllerB tempPlayer = StartOfRound.Instance.allPlayerScripts[i];
                if(PlayerIsTargetable(tempPlayer))
                {
                    for (int j = 0; j < boxEdgesList.Count; j++)
                    {
                        if(tempPlayer.HasLineOfSightToPosition(boxEdgesList[j].transform.position, range: 150) && PlayerHasHorizontalLOS(tempPlayer))
                        {
                            isBeingLookedAt = true;
                        }
                    }
                }
            }

            if(isBeingLookedAt)
            {
                hasChangedPose = false;
                agent.speed = 0f;
            }
            else
            {
                agent.speed = walkSpeed;
                timeSpentMoving += Time.deltaTime;
                if (!hasChangedPose)
                {
                    if (IsServer)
                    {
                        int randomPose = Random.RandomRangeInt(0, 7);
                        ChangePoseClientRPC(randomPose);
                        hasChangedPose = true;
                    }                    
                }
            }
            if (IsServer)
            {
                SyncPositionToClients();
            
                for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
                {
                    if (Vector3.Distance(feet.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) <= 3 && !isAlreadyAttacking)
                    {
                        RemoteExplode();
                    }
                }
                SetChestLightIntensityClientRPC(chestLightIntensity);
            }
        }

        public KeyValuePair<Transform, float> ScanForTargets()
        {
            //Scanning items
            GrabbableObject[] array = FindObjectsOfType<GrabbableObject>();
            bestTarget = new KeyValuePair<Transform, float>(null!, detectionRange * 4);
            for (int i = 0; i < array.Length; i++)
            {
                string tempItemName = array[i].itemProperties.itemName;
                float tempdistance = Vector3.Distance(transform.position, array[i].transform.position);
                float interestByDistance = detectionRange * 2;

                //initial checks
                if (tempdistance >= detectionRange || checkedItems.Contains(array[i].transform.gameObject.GetInstanceID()))
                {
                    continue;
                }
                if (array[i].isPocketed && (!grabbableObjectInterest.ContainsKey(tempItemName) || !grabbableObjectInterest[tempItemName].isBrightWhenPocketed))
                {
                    continue;
                }
                if (Physics.Linecast(eye.transform.position, array[i].transform.position + Vector3.up * 0.5f, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                //Does the item have a specific interest value ?
                if (grabbableObjectInterest.ContainsKey(tempItemName))
                {
                    bool flag = false;
                    switch(grabbableObjectInterest[tempItemName].objectType){
                        case 0:
                            flag = true;
                            break;
                        case 1:
                            flag = array[i].isBeingUsed;
                            break;
                        case 2:
                            flag = tempItemName == "Radar-booster" && array[i] is RadarBoosterItem radar && radar.radarEnabled;
                            break;
                        default:
                            break;
                    }
                    if (flag)
                    {
                        interestByDistance = tempdistance * grabbableObjectInterest[tempItemName].interestValue;
                    }
                    else
                    {
                        interestByDistance = tempdistance * grabbableObjectInterest[tempItemName].defaultInterestValue;
                    }
                    if (bestTarget.Value > interestByDistance)
                    {
                        bestTarget = new KeyValuePair<Transform, float>(array[i].transform, interestByDistance);
                    }
                }
                // else
                // {
                //     interestByDistance = tempdistance * 2;
                // }
            }

            //Scanning players
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                PlayerControllerB tempPlayer = StartOfRound.Instance.allPlayerScripts[i];
                float tempdistance = Vector3.Distance(transform.position, tempPlayer.transform.position);
                if (tempdistance >= detectionRange || !PlayerIsTargetable(tempPlayer) || Physics.Linecast(eye.transform.position, tempPlayer.transform.position + Vector3.up * 1.5f, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                if (bestTarget.Value > tempdistance)
                {
                    bestTarget = new KeyValuePair<Transform, float>(tempPlayer.transform, tempdistance);
                }
            }
            return bestTarget;
        }
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null!, bool playHitSFX = true, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            if (!isAlreadyAttacking && playerWhoHit != null && playerWhoHit.playerClientId == GameNetworkManager.Instance.localPlayerController.playerClientId)
            {
                AttackServerRPC();
            }
        }

        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy = null!)
        {
            base.OnCollideWithEnemy(other, collidedEnemy);
            if (!collidedEnemy.isEnemyDead && isBeingLookedAt)
            {
                RemoteExplode();
            }
        }

        public void RemoteExplode()
        {
            if (!isAlreadyAttacking && IsServer)
            {
                isAlreadyAttacking = true;
                AttackClientRPC();
            }
            return;
        }
        [ClientRpc]
        public void ChangePoseClientRPC(int poseID)
        {
            foreach (GameObject i in posesList)
            {
                i.SetActive(false);
            }
            posesList[poseID].SetActive(true);
            currentPose = poseID;
            return;
        }

        [ServerRpc(RequireOwnership = false)]
        public void AttackServerRPC()
        {
            AttackClientRPC();
        }

        [ClientRpc]
        public void AttackClientRPC()
        {
            StartCoroutine(AttackCoroutine());
        }

        private IEnumerator AttackCoroutine()
        {
            creatureAnimator.Play("Death");
            yield return new WaitForSeconds(1f);
            TouristAI[] array = FindObjectsOfType<TouristAI>();
            for (int i = 0; i < array.Length; i++)
            {
                if (Vector3.Distance(transform.position, array[i].transform.position) <= 10)
                {
                    collisionBox.SetActive(false);
                    if (IsServer)
                    {
                        array[i].SendMessage("RemoteExplode");
                    }
                }
            }
            Landmine.SpawnExplosion(transform.position, true, 3, 10, 50, 10);
            posesList[currentPose].SetActive(false);
            if (IsServer)
            {
                yield return new WaitForSeconds(1f);
                NetworkObject touristNetworkObject = GetComponent<NetworkObject>();
                if (touristNetworkObject != null && touristNetworkObject.IsSpawned)
                {
                    touristNetworkObject.Despawn();
                }
            }
            yield break;
        }
        [ClientRpc]
        public void SetChestLightIntensityClientRPC(float intensity)
        {
            float lerpValue = Mathf.Clamp(Time.deltaTime * 0.4f, 0, 1);
            chestLight.intensity = Mathf.Lerp(chestLight.intensity, intensity, lerpValue);
            return;
        }

        public bool PlayerHasHorizontalLOS(PlayerControllerB player)
        {
            Vector3 to = base.transform.position - player.transform.position;
            to.y = 0f;
            return Vector3.Angle(player.transform.forward, to) < 120f;
        }
        private IEnumerator NaturalSpawnCoroutine()
        {
            yield return new WaitForSeconds(0.1f);
            if (isNaturallySpawned)
            {
                for (int i = 0; i < Random.RandomRangeInt(5, 16); i++)
                {
                    Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
                    spawnPosition = RoundManager.Instance.GetNavMeshPosition(spawnPosition);
                    if (!RoundManager.Instance.GotNavMeshPositionResult)
                    {
                        spawnPosition = transform.position;
                    }
                    var spawnTourist = ExternalScripts.Spawn(GetEnemies.Tourist, spawnPosition);
                    NetworkObject? spawnedTouristNetworkObject = null;
                    var startTime = Time.realtimeSinceStartup;
                    while(Time.realtimeSinceStartup - startTime < 8 && !spawnTourist.TryGet(out spawnedTouristNetworkObject))
                    {
                        yield return new WaitForSeconds(0.03f);
                    }
                    if (spawnedTouristNetworkObject == null)
                    {
                        LogIfDebugBuild("spawnedTouristNetworkObject is null!");
                        yield break;
                    }
                    yield return new WaitForEndOfFrame();
                    var tourist = ((GameObject)spawnTourist).GetComponent<TouristAI>();
                    tourist.isNaturallySpawned = false;
                }

            }
            yield break;
        }
    }
}