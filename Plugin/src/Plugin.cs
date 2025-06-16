using System.Reflection;
using UnityEngine;
using BepInEx;
using LethalLib.Modules;
using BepInEx.Logging;
using System.IO;
using LethalAnomalies.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System;
using BepInEx.Bootstrap;
using System.Linq;

namespace LethalAnomalies {
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    [BepInDependency("me.loaforc.soundapi", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("me.loaforc.facilitymeltdown", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin {
        const string PLUGIN_GUID = "Zeldahu.LethalAnomalies";
        const string PLUGIN_NAME = "Lethal Anomalies";
        const string PLUGIN_VERSION = "0.2.1";
        internal static new ManualLogSource Logger = null!;
        internal static PluginConfig BoundConfig { get; private set; } = null!;
        public static AssetBundle? ModAssets;
        private readonly Harmony harmony = new Harmony(PLUGIN_GUID);

        private void Awake() {
            Logger = base.Logger;

            BoundConfig = new PluginConfig(base.Config);

            InitializeNetworkBehaviours();

            var bundleName = "lethalanomalies";
            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), bundleName));
            if (ModAssets == null) {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }

            // Remember to rename assets both here and in the Unity project.
            var SparkTower = ModAssets.LoadAsset<EnemyType>("Spark Tower");
            var SparkTowerTN = ModAssets.LoadAsset<TerminalNode>("SparkTowerTN");
            var SparkTowerTK = ModAssets.LoadAsset<TerminalKeyword>("SparkTowerTK");

            var Tourist = ModAssets.LoadAsset<EnemyType>("Tourist");
            var TouristTN = ModAssets.LoadAsset<TerminalNode>("TouristTN");
            var TouristTK = ModAssets.LoadAsset<TerminalKeyword>("TouristTK");

            // Network Prefabs need to be registered. See https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
            // LethalLib registers prefabs on GameNetworkManager.Start.
            NetworkPrefabs.RegisterNetworkPrefab(SparkTower.enemyPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(SparkTower.nestSpawnPrefab);
            SparkTower.PowerLevel = BoundConfig.SparkTowerPowerLevel.Value;
            SparkTower.MaxCount = BoundConfig.SparkTowerMaxCount.Value;
            var SparkTowerModdedRarities = new Dictionary<string, int> ();
            var SparkTowerVanillaRarities = new Dictionary<Levels.LevelTypes, int> ();
            var TouristModdedRarities = new Dictionary<string, int> ();
            var TouristVanillaRarities = new Dictionary<Levels.LevelTypes, int> ();
            foreach (string moonrarity in BoundConfig.SparkTowerSpawnWeight.Value.Split(','))
            {
                var entry = moonrarity.Split(':');
                if (entry.Length != 2)
                {
                    continue;
                }

                if (!int.TryParse(entry[1], out var rarity))
                {
                    continue;
                }

                if (Enum.TryParse<Levels.LevelTypes>(entry[0], true, out var moonname))
                {
                    SparkTowerVanillaRarities[moonname] = rarity;
                }
                else
                {
                    SparkTowerModdedRarities[entry[0]] = rarity;
                }
            }
            Enemies.RegisterEnemy(SparkTower, SparkTowerVanillaRarities, SparkTowerModdedRarities, SparkTowerTN, SparkTowerTK);

            foreach (string moonrarity in BoundConfig.TouristSpawnWeight.Value.Split(','))
            {
                var entry = moonrarity.Split(':');
                if (entry.Length != 2)
                {
                    continue;
                }

                if (!int.TryParse(entry[1], out var rarity))
                {
                    continue;
                }

                if (Enum.TryParse<Levels.LevelTypes>(entry[0], true, out var moonname))
                {
                    TouristVanillaRarities[moonname] = rarity;
                }
                else
                {
                    TouristModdedRarities[entry[0]] = rarity;
                }
            }

            NetworkPrefabs.RegisterNetworkPrefab(Tourist.enemyPrefab);
            Enemies.RegisterEnemy(Tourist, TouristVanillaRarities, TouristModdedRarities, TouristTN, TouristTK);

            harmony.PatchAll();
            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
        }

        private static void InitializeNetworkBehaviours() {
            // See https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        } 
    }
}