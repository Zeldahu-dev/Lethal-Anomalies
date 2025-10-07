using UnityEngine;
using Unity.Netcode;
using DigitalRuby.ThunderAndLightning;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine.Analytics;

namespace LethalAnomalies.External
{
	internal class ExternalScripts
	{
		public static void SpawnLightningBolt(Vector3 strikePosition, bool isLethal, Vector3 origin = default)
		{
			// This will ignore if the strikePosition is inside something, oh well

			LightningBoltPrefabScript localLightningBoltPrefabScript;
			UnityEngine.Vector3 vector = Vector3.zero;
			System.Random random;

			random = new System.Random(StartOfRound.Instance.randomMapSeed);
			float num = (float)random.Next(-32, 32);
			float num2 = (float)random.Next(-32, 32);

			if (origin == default)
			{
				vector = strikePosition + Vector3.up * 160f + new Vector3((float)random.Next(-32, 32), 0f, (float)random.Next(-32, 32));
			}
			else
			{
				vector = origin;
			}

			StormyWeather stormy = UnityEngine.Object.FindObjectOfType<StormyWeather>(true);

			localLightningBoltPrefabScript = Object.Instantiate(stormy.targetedThunder);
			localLightningBoltPrefabScript.enabled = true;

			localLightningBoltPrefabScript.Camera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
			localLightningBoltPrefabScript.AutomaticModeSeconds = 0.2f;

			localLightningBoltPrefabScript.Source.transform.position = vector;
			localLightningBoltPrefabScript.Destination.transform.position = strikePosition;
			localLightningBoltPrefabScript.CreateLightningBoltsNow();

			AudioSource audioSource = Object.Instantiate(stormy.targetedStrikeAudio);
			audioSource.transform.position = strikePosition + Vector3.up * 0.5f;
			audioSource.enabled = true;

			stormy.PlayThunderEffects(strikePosition, audioSource);

			if (isLethal)
			{
				Landmine.SpawnExplosion(strikePosition + Vector3.up * 0.25f, spawnExplosionEffect: false, 2.4f, 5f);
			}
		}

		public static NetworkObjectReference Spawn(SpawnableEnemyWithRarity enemy, Vector3 position, float yRot = 0f)
		{
			GameObject gameObject = Object.Instantiate(enemy.enemyType.enemyPrefab, position, Quaternion.Euler(new Vector3(0f, yRot, 0f)));
			gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
			RoundManager.Instance.SpawnedEnemies.Add(gameObject.GetComponent<EnemyAI>());
			return new NetworkObjectReference(gameObject);
		}

		public static LightningBoltPrefabScript? CreateLightningBolt(StormyWeather stormy, out Vector3 source, ref Vector3 strikePosition, bool redirectInside)
		{
			LightningBoltPrefabScript lightning;
			var random = new System.Random(StartOfRound.Instance.randomMapSeed);
			random.Next(-32, 32); random.Next(-32, 32);
			source = strikePosition + Vector3.up * 160f + new Vector3(random.Next(-32, 32), 0f, random.Next(-32, 32));
			if (redirectInside && Physics.Linecast(source, strikePosition + Vector3.up * 0.5f, out _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				if (!Physics.Raycast(source, strikePosition - source, out var rayHit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
					return null;
				strikePosition = rayHit.point;
			}
			lightning = Object.Instantiate(stormy.targetedThunder);
			lightning.enabled = true;
			lightning.Camera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
			lightning.AutomaticModeSeconds = 0.2f;
			return lightning;
		}
		public static void SpawnLightningBolt(LightningBoltPrefabScript lightning, StormyWeather stormy, Vector3 source, Vector3 destination, bool damage = true)
        {
            lightning.Source.transform.position = source;
            lightning.Destination.transform.position = destination;
            lightning.CreateLightningBoltsNow();
            var audioSource = Object.Instantiate(stormy.targetedStrikeAudio);
            audioSource.transform.position = destination + Vector3.up * 0.5f;
            audioSource.enabled = true;
            if (damage)
                Landmine.SpawnExplosion(destination + Vector3.up * 0.25f, spawnExplosionEffect: false, 2.4f, 5f);
            stormy.PlayThunderEffects(destination, audioSource);
        }
	}
}
