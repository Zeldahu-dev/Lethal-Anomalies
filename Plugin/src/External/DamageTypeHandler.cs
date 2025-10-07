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
using UnityEngine.Animations;

namespace LethalAnomalies {

    class DamageTypeHandler : NetworkBehaviour
    {
        public PlayerControllerB player = null!;
        private float acidEffectTimer = -1f;
        public GameObject? acidParticles = null!;
        private float stunEffectTimer = -1f;
        public ParticleSystem? electricBurstParticles = null!;
        public ParticleSystem? electricLingeringParticles = null!;
        public int radioactiveStacks = 0;
        public float timeSinceLastRadioactiveDamage = 0;
        public bool shouldRadioactiveTickDamageAllies = false;

        public void Start()
        {
            player = transform.parent.gameObject.GetComponent<PlayerControllerB>();
        }

        public void Update()
        {
            if (acidEffectTimer >= 0)
            {
                acidEffectTimer -= Time.deltaTime;
            }
            else
            {
                acidEffectTimer = -1;
                acidParticles!.SetActive(false);
            }

            if (stunEffectTimer >= 0)
            {
                stunEffectTimer -= Time.deltaTime;
            }
            else
            {
                stunEffectTimer = -1;
            }

            timeSinceLastRadioactiveDamage += Time.deltaTime;
            if (timeSinceLastRadioactiveDamage >= 0.25f && radioactiveStacks > 0)
            {
                if (shouldRadioactiveTickDamageAllies)
                {
                    for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
                    {
                        if (Vector3.Distance(player.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) <= 10)
                        {
                            StartOfRound.Instance.allPlayerScripts[i].DamagePlayer(1, true, causeOfDeath: CauseOfDeath.Unknown);
                        }
                    }
                    shouldRadioactiveTickDamageAllies = false;
                }
                else
                {
                    player.DamagePlayer(1, false, causeOfDeath: CauseOfDeath.Unknown);
                    shouldRadioactiveTickDamageAllies = true;
                }
                timeSinceLastRadioactiveDamage = 0f;
                radioactiveStacks -= 1;
            }
        }

        public void OnPlayerDamaged(int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default(Vector3))
        {
            if (acidEffectTimer >= 0)
            {
                acidEffectTimer = -1;
                acidParticles!.SetActive(false);
                player.DamagePlayer(damageNumber, hasDamageSFX, callRPC, causeOfDeath, deathAnimation, fallDamage, force);
            }
        }

        public void DamageAcid(int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default(Vector3))
        {
            player.DamagePlayer(damageNumber, hasDamageSFX, callRPC, causeOfDeath, deathAnimation, fallDamage, force);
            acidEffectTimer = damageNumber * 2;
            acidParticles!.SetActive(true);
            HUDManager.Instance.DisplayTip("Acid damage detected!", "The next hit will deal twice the damage, stay out of trouble until the acid dries off", true, true, "LA_AcidDamageTip");
        }

        public void DamageElectric(int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default(Vector3))
        {
            player.DamagePlayer(damageNumber, hasDamageSFX, callRPC, causeOfDeath, deathAnimation, fallDamage, force);
            electricBurstParticles!.Play();
            var main = electricLingeringParticles!.main;
            main.duration = damageNumber / 5;
            electricLingeringParticles!.Play();
            stunEffectTimer = Mathf.Clamp(damageNumber / 5, 0, 10);
            GrabbableObject[] array = FindObjectsOfType<GrabbableObject>();
            foreach (GrabbableObject item in array)
            {
                if (item.playerHeldBy == player && item.itemProperties.requiresBattery)
                {
                    item.insertedBattery.charge = Mathf.Clamp(item.insertedBattery.charge - 0.2f, 0f, 100f);
                }
            }
            HUDManager.Instance.DisplayTip("Electric damage detected!", "Your batteries have been drained, and your movement is slowed", true, true, "LA_ElectricDamageTip");
        }

        public void DamageRadioactive(int damageNumber)
        {
            //Enable particles
            radioactiveStacks += damageNumber;
            HUDManager.Instance.DisplayTip("Radioactive damage detected!", "You take damage over, and apply some of to nearby allies too", true, true, "LA_AcidDamageTip");
        }
    }
}