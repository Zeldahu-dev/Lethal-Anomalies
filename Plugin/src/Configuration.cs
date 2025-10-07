using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;

namespace LethalAnomalies.Configuration {
    public class PluginConfig
    {
        // For more info on custom configs, see https://lethal.wiki/dev/intermediate/custom-configs
        public ConfigEntry<string> SparkTowerSpawnWeight;
        public ConfigEntry<float> SparkTowerPowerLevel;
        public ConfigEntry<int> SparkTowerMaxCount;
        public ConfigEntry<float> SparkTowerEffectiveRange;
        public ConfigEntry<float> SparkTowerDetectionThreshold;
        public ConfigEntry<float> SparkTowerCooldownMultiplier;
        public ConfigEntry<bool> SparkTowerAltBehaviorEnabled;
        public ConfigEntry<int> SparkTowerAltAttackThreshold;
        public ConfigEntry<string> TouristSpawnWeight;
        public ConfigEntry<float> TouristDetectionRange;
        public ConfigEntry<float> TouristSpeed;
//        public ConfigEntry<float> SparkTowerLightningLeniency;
        public PluginConfig(ConfigFile cfg)
        {
            SparkTowerSpawnWeight = cfg.Bind("Spark Tower", "Spawn weight", "ExperimentationLevel:50,AssuranceLevel:15,VowLevel:20,OffenseLevel:80,MarchLevel:55,AdamanceLevel:5,RendLevel:20,DineLevel:15,ArtificeLevel:20,EmbrionLevel:100,Bozoros:35,Infernis:25,Synthesis:25",
                "The spawn chance weight for the Spark Tower, relative to other existing enemies.\n" +
                "Comma separated list allowing you to specify a value for each moon, including modded moons.\n" +
                "Goes up from 0, lower is more rare, 100 and up is very common.");
            
            SparkTowerPowerLevel = cfg.Bind("Spark Tower", "Power level", 1f,
                "The power level of the tower. General setting that applies for all moons");
            
            SparkTowerMaxCount = cfg.Bind("Spark Tower", "Max count", 10,
                "The maximum amount of towers that spawn on a moon. General setting that applies for all moons.");
            
            SparkTowerEffectiveRange = cfg.Bind("Spark Tower", "Effective Range", 30f,
                "The range at which the tower detects and attacks enemies");
            
            SparkTowerDetectionThreshold = cfg.Bind("Spark Tower", "Attack speed", 2.5f,
                "Determines how long it takes for the tower to attack players while they stand in its detection range.\n" +
                "Lower values make the tower attack faster, higher values require players to stand close for longer before being attacked");
            
            SparkTowerCooldownMultiplier = cfg.Bind("Spark Tower", "Cooldown multiplier", 1f,
                "Modifies how long the tower takes after attacking, before detecting players again \n" +
                "The normal cooldown value is randomly chosen between 3-10 seconds.");

            //            SparkTowerLightningLeniency = cfg.Bind("Spark Tower", "Lightning Leniency", 0.6f,
            //                "The amount in seconds before the tower acquiring the player's position, and the lightning hitting that position.\n" +
            //                "This is NOT supposed to be visible, if it is contact me on Discord to fix it.");

            SparkTowerAltBehaviorEnabled = cfg.Bind("Spark Tower Alternative Behavior", "Enabled", false,
                "Check this to enable the alternative behavior for the spark tower. \n" +
                "While this is on, the tower will repeatedly emit sparks towards players within range and line of sight. \n" +
                "If a player is zapped 12 times (configurable) in a row, they will be hit by lightning and die. \n" +
                "Break line of sight to reset the counter. Recommended for larger groups.");

            SparkTowerAltAttackThreshold = cfg.Bind("Spark Tower Alternative Behavior", "Alternative attack Threshold", 12,
                "How many times a player can be zapped by the spark tower in a row before triggering the lightning.");

            TouristSpawnWeight = cfg.Bind("Tourist", "Spawn weight", "ExperimentationLevel:30,AssuranceLevel:40,VowLevel:25,OffenseLevel:40,MarchLevel:30,AdamanceLevel:25,RendLevel:30,DineLevel:25,ArtificeLevel:30,EmbrionLevel:25,Bozoros:55,Infernis:25,Synthesis:25",
                "The spawn chance weight for the Tourist, relative to other existing enemies.\n" +
                "Comma separated list allowing you to specify a value for each moon, including modded moons.\n" +
                "Goes up from 0, lower is more rare, 100 and up is very common.");
            
            TouristDetectionRange = cfg.Bind("Tourist", "Detection Range", 50f,
                "The range at which the Tourists are able to detect potential targets");

            TouristSpeed = cfg.Bind("Tourist", "Speed Multiplier", 1f,
                "The multiplier for tourists speed");
            ClearUnusedEntries(cfg);
        }

        private void ClearUnusedEntries(ConfigFile cfg) {
            // Normally, old unused config entries don't get removed, so we do it with this piece of code. Credit to Kittenji.
            PropertyInfo orphanedEntriesProp = cfg.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg, null);
            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            cfg.Save(); // Save the config file to save these changes
        }
    }
}