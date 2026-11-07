using System.Collections.Generic;
using Game.Balance;
using Game.Economy;
using Game.Enemies;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the waves and the campaign economy. Runs once every level, tower, creature and upgrade exists:
    /// writes what each level pays between levels (from the balance sheet), builds the balance model of the whole
    /// campaign, lets the wave solver choose how tough every wave's creatures are, and writes the waves of every level
    /// and difficulty from the level's wave script (<see cref="WaveScripts"/>). The BalanceLab tool runs the same code
    /// outside Unity, so both write the same data.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string WavesFolder = DataRoot + "/Waves";

        private static void GenerateCampaignWaves(CampaignCatalog campaign, UpgradeCatalog upgrades, UnlockTable unlocks, Bestiary bestiary)
        {
            WriteLevelPayouts(campaign);
            CampaignModel model = BalanceLedger.Campaign(campaign, upgrades, unlocks.StartingTowerIds, BalanceSheet.Rules, AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath));
            foreach (EnemyDefinition enemy in bestiary.All)
            {
                model.Enemies[enemy.name] = BalanceLedger.Stats(enemy);
            }

            var written = new HashSet<string>();
            for (int l = 0; l < campaign.Nodes.Count; l++)
            {
                LevelDefinition level = campaign.Nodes[l].Level;
                WaveScriptSpec script = level != null ? WaveScripts.ByKey(Key(level)) : null;
                int index = level != null ? model.IndexOf(Key(level)) : -1;
                if (script == null || index < 0)
                {
                    continue;
                }

                List<List<WaveSpec>> sets = model.BuildWaves(index, script, out _);
                WriteWaveSets(level, campaign, sets, bestiary, model.PlayerAt(index, WaveSolver.SolveDifficulty).MeanTowerBonus, script.TargetScale);
                written.Add(WavesPath(level));
            }

            DeleteStaleWaveAssets(written);
        }


        /// <summary>
        /// The pipeline's solve stage (round 22, BalanceStages.SolveWaves): only the "Difficulties" and "Waves" steps, with the
        /// bestiary read from the existing creature assets, so the waves are re-solved against the calibrated model without the
        /// full generator (which syncs the Blender exports). Tools/PlayTest/Scripts/ApplyWaves.cs calls the same method.
        /// </summary>
        public static void SolveWavesOnly()
        {
            var context = new GeneratorContext();
            GenerateDifficulties(context);
            AssetDatabase.SaveAssets();

            var bestiary = new Bestiary();
            var byName = new Dictionary<string, EnemyDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { DataRoot + "/Enemies" }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                byName[enemy.name] = enemy;
            }

            foreach (System.Reflection.FieldInfo field in typeof(Bestiary).GetFields())
            {
                if (field.FieldType != typeof(EnemyDefinition))
                {
                    continue;
                }

                if (!byName.TryGetValue(field.Name, out EnemyDefinition found))
                {
                    foreach (KeyValuePair<string, EnemyDefinition> pair in byName)
                    {
                        found = pair.Key.EndsWith(field.Name, System.StringComparison.Ordinal) ? pair.Value : found;
                    }
                }

                if (found == null)
                {
                    throw new System.InvalidOperationException("No creature asset for " + field.Name);
                }

                field.SetValue(bestiary, found);
            }

            var campaign = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(BalanceStages.CampaignPath);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(BalanceStages.UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(BalanceStages.UnlocksPath);
            GenerateCampaignWaves(campaign, upgrades, unlocks, bestiary);
            AssetDatabase.SaveAssets();
        }

        private static string WavesPath(LevelDefinition level) => $"{WavesFolder}/Waves_{Key(level)}.asset";

        // Removes whatever else lies in the waves folder, such as the one-asset-per-wave files of the old format, so no
        // orphan survives a run; the folder itself goes too once nothing is left in it.
        private static void DeleteStaleWaveAssets(HashSet<string> keep)
        {
            if (!AssetDatabase.IsValidFolder(WavesFolder))
            {
                return;
            }

            var stale = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { WavesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!keep.Contains(path) && !AssetDatabase.IsValidFolder(path))
                {
                    stale.Add(path);
                }
            }

            if (stale.Count > 0 && !AssetDatabase.DeleteAssets(stale.ToArray(), new List<string>()))
            {
                throw new System.InvalidOperationException($"Could not delete {stale.Count} stale wave assets.");
            }

            if (keep.Count == 0 && AssetDatabase.FindAssets(string.Empty, new[] { WavesFolder }).Length == 0)
            {
                AssetDatabase.DeleteAsset(WavesFolder);
            }
        }

        private static string Key(LevelDefinition level) => level.name.StartsWith("Level_") ? level.name.Substring(6) : level.name;

        // Each continent pays its own budget: research rises gently over its twenty levels and power pays per boss, both
        // fixed by a level's index in its continent (BalanceSheet, ContinentEconomy).
        private static void WriteLevelPayouts(CampaignCatalog campaign)
        {
            for (int i = 0; i < campaign.Nodes.Count; i++)
            {
                MapNode node = campaign.Nodes[i];
                WaveScriptSpec script = node.Level != null ? WaveScripts.ByKey(Key(node.Level)) : null;
                if (script == null)
                {
                    continue;
                }

                int index = i - campaign.FirstNode(node.Region);
                var so = new SerializedObject(node.Level);
                SerializedProperty reward = so.FindProperty("_reward");
                reward.FindPropertyRelative("ResearchPoints").intValue = BalanceSheet.Research(node.Region, index);
                reward.FindPropertyRelative("PowerCurrency").intValue = BalanceSheet.Power(node.Region, script);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void WriteWaveSets(LevelDefinition level, CampaignCatalog campaign, List<List<WaveSpec>> sets, Bestiary bestiary, float expectedBonus, float[] targetScale)
        {
            LevelWaves asset = LoadOrCreate<LevelWaves>(WavesPath(level));
            var waveSo = new SerializedObject(asset);
            SerializedProperty waveArray = waveSo.FindProperty("_difficultyWaves");
            waveArray.arraySize = sets.Count;
            for (int d = 0; d < sets.Count; d++)
            {
                SerializedProperty element = waveArray.GetArrayElementAtIndex(d);
                element.FindPropertyRelative("Difficulty").objectReferenceValue = campaign.Difficulties[d];
                SerializedProperty waves = element.FindPropertyRelative("Waves");
                waves.arraySize = sets[d].Count;
                for (int i = 0; i < sets[d].Count; i++)
                {
                    WriteWave(waves.GetArrayElementAtIndex(i), sets[d][i], bestiary);
                }
            }

            waveSo.ApplyModifiedPropertiesWithoutUndo();
            var so = new SerializedObject(level);
            so.FindProperty("_waves").objectReferenceValue = asset;

            // For reference only: the upgrade a Medium player is expected to bring; the waves are already sized for it.
            so.FindProperty("_expectedUpgradeBonus").floatValue = Mathf.Round(Mathf.Clamp01(expectedBonus) * 100f) / 100f;
            // The builder's target scale the waves were calibrated to (WaveTuning), so the ledger checks judge against the same curve.
            SerializedProperty scale = so.FindProperty("_balanceTargetScale");
            float[] values = targetScale != null && targetScale.Length > 0 ? targetScale : new[] { 1f };
            scale.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                scale.GetArrayElementAtIndex(i).floatValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteWave(SerializedProperty wave, WaveSpec spec, Bestiary bestiary)
        {
            wave.FindPropertyRelative("_startDelay").floatValue = spec.StartDelay;
            wave.FindPropertyRelative("_spawnJitter").floatValue = WaveDefinition.DefaultSpawnJitter;
            wave.FindPropertyRelative("_healthMultiplier").floatValue = spec.HealthMultiplier;
            SerializedProperty array = wave.FindPropertyRelative("_groups");
            array.arraySize = spec.Groups.Count;
            for (int g = 0; g < spec.Groups.Count; g++)
            {
                GroupSpec group = spec.Groups[g];
                SerializedProperty element = array.GetArrayElementAtIndex(g);
                element.FindPropertyRelative("Enemy").objectReferenceValue = bestiary.ById(group.Enemy.Id);
                element.FindPropertyRelative("Count").intValue = group.Count;
                element.FindPropertyRelative("Interval").floatValue = group.Interval;
                element.FindPropertyRelative("Delay").floatValue = group.Delay;
                element.FindPropertyRelative("Stagger").floatValue = group.Stagger;
                element.FindPropertyRelative("PathIndex").intValue = group.Path;
                element.FindPropertyRelative("Source").enumValueIndex = group.Source;
            }
        }
    }
}
