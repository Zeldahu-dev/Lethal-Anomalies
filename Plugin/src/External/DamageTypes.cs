using UnityEngine;
using Unity.Netcode;
using DigitalRuby.ThunderAndLightning;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine.Analytics;
using LethalLib;

namespace LethalAnomalies
{
	internal class DamageTypes
	{
		public static Dictionary<PlayerControllerB, float> acidAffectedPlayers = new Dictionary<PlayerControllerB, float>();
		public List<string> anomalyTextList = new List<string>
		{
			"PG0xsC0oqHSiNG0t2FfNEzvQEQKapyg4", "d0GlG0mptRfurrbcmB2A6camoHKB7nZL",
			"U12YtGQyDFHay5h9jc1GIUGsOjQrJYnt", "mqcYY9dOrJvX2bNY7NNgFvbhSq1eLC4B",
			"--.. . .-.. -.. .- .... ..-", "... is this coming through? ...Mike... Hello?",
			"Next season of Night Springs airing oNNS%$$£µ!§<¨ù", "                                  "
		};
		public List<string> anomalyShopItemList = new List<string>
		{
			"Zeldahu", "Zigzag_Awaka", "Abibabou", "XyphireTV", "Halgard", "Cowboy_Bigbop", "Xplozivo", "                 ", "?????????", "Vacations straight to hell", "...", "PLEASE HELP PLEASE HELP PLEASE HELP"
		};

		public List<string> anomalyShopPriceList = new List<string>
		{
			"$4.99", "$9.99", "$19.99", "$99.99", "50 quids", "AAAAAAAAAA", "12 kromer", "20% off !", "50% off!!", "80% off!!!", "FREE!!!!", "-48£???", "Two sticks and one stone", "A smile :)"
		};
		public GameObject? acidParticlesPrefab = null;

		public void DamagePlayerAcid(PlayerControllerB target, int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default(Vector3))
		{
			target.DamagePlayer(damageNumber, hasDamageSFX, callRPC, causeOfDeath, deathAnimation, fallDamage, force);
			if (acidAffectedPlayers.ContainsKey(target))
			{
				return;
			}
			
			acidAffectedPlayers.Add(target, damageNumber * 2);
		}

		public void DamagePlayerElectromagnetic(PlayerControllerB target, int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default(Vector3))
		{
			target.DamagePlayer(damageNumber, hasDamageSFX, callRPC, causeOfDeath, deathAnimation, fallDamage, force);
			switch (Random.RandomRangeInt(0, 5))
			{
				case 0:
					HUDManager.Instance.DisplayTip("ERROR", anomalyTextList[Random.RandomRangeInt(0, anomalyTextList.Count - 1)], false);
					break;
				case 1:
					HUDManager.Instance.DisplayTip("ERROR", anomalyTextList[Random.RandomRangeInt(0, anomalyTextList.Count - 1)], true);
					break;
				case 2:
					HUDManager.Instance.BeginDisplayAd(anomalyShopItemList[Random.RandomRangeInt(0, anomalyShopItemList.Count - 1)], anomalyShopPriceList[Random.RandomRangeInt(0, anomalyShopPriceList.Count - 1)]);
					break;
				case 3:
					HUDManager.Instance.DisplayStatusEffect(anomalyTextList[Random.RandomRangeInt(0, anomalyTextList.Count - 1)]);
					break;
				case 4:
					HUDManager.Instance.RadiationWarningHUD();
					break;
				default:
					break;
			}
		}
	}
}
