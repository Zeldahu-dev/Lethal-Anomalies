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

    class ScrapBunnyAI : EnemyAI
    {
        public Transform turnCompass = null!;
        public Transform attackArea = null!;
        public class BunnyType
        {
            public string name = null!;
            public int pickweight = 100;
            public string scanName = null!;
            public BunnyType(string name, int pickweight, string scanName)
            {
                this.name = name;
                this.pickweight = pickweight;
                this.scanName = scanName;
            }
        }
        public BunnyType currentBunnyType = null!;
        public List<BunnyType> bunnyTypesList = new List<BunnyType>()
        {
            new BunnyType("Acid", 120, "Burp Bunny"),
            new BunnyType("Broken", 200, "Broken Bunny"),
            new BunnyType("Charge", 100, "Hopped-Up Hare"),
            new BunnyType("Electric", 150, "Bolt Bunny"),
            new BunnyType("Explosive", 80, "Boom Bunny"),
            new BunnyType("Heal", 60, "Happy Hare"),
            new BunnyType("Radioactive", 180, "Dust Bunny")
        };
        public ScanNodeProperties scanNode = null!;
        public bool hasAttacked = false;
        enum State
        {
            Roaming,
            Playful,
            Latching,
        }

        [Conditional("DEBUG")]
        public void LogIfDebugBuild(string text)
        {
            Plugin.Logger.LogInfo(text);
        }
        public override void Start()
        {
            base.Start();
            PickBunnyType();
            LogIfDebugBuild("Bunny Type selected : " + currentBunnyType.scanName);
            SwitchToBehaviourClientRpc((int)State.Roaming);
        }

        public override void Update()
        {
            base.Update();

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
                case (int)State.Roaming:
                    if (!currentSearch.inProgress)
                    {
                        StartSearch(transform.position);
                    }

                    if (!hasAttacked)
                    {
                        AttackClientRpc();
                    }
                    break;

                case (int)State.Playful:

                    break;

                case (int)State.Latching:

                    break;

                default:
                    LogIfDebugBuild("This Behavior State doesn't exist!");
                    break;
            }
        }
        public void PickBunnyType()
        {
            int maxweight = 0;
            foreach (BunnyType type in bunnyTypesList)
            {
                maxweight += type.pickweight;
            }
            int randomPick = Random.RandomRangeInt(1, maxweight + 1);
            foreach (BunnyType type in bunnyTypesList)
            {
                randomPick -= type.pickweight;
                if (randomPick <= 0)
                {
                    currentBunnyType = type;
                    scanNode.headerText = type.scanName;
                    break;
                }
            }
            if ((currentBunnyType.name == "Charge") || (currentBunnyType.name == "Heal"))
            {
                scanNode.nodeType = 2;
            }
        }

        [ClientRpc]
        private void AttackClientRpc()
        {
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                PlayerControllerB tempPlayer = StartOfRound.Instance.allPlayerScripts[i];
                if (PlayerIsTargetable(tempPlayer) && Vector3.Distance(transform.position, tempPlayer.transform.position) < 10 && !hasAttacked)
                {
                    LogIfDebugBuild("Affecting player with acid");
                    tempPlayer.GetComponentInChildren<DamageTypeHandler>().DamageRadioactive(40);
                    hasAttacked = true;
                }
            }
        }
    }
}