using UnityEngine;
using Unity.Netcode;
using DigitalRuby.ThunderAndLightning;

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
	}
}
