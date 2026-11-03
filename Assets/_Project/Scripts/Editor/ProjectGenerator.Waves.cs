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
        private const float FirstWaveDelay = 10f;
        private const float WaveDelay = 8f;

        private static void GenerateCampaignWaves(CampaignCatalog campaign, UpgradeCatalog upgrades, UnlockTable unlocks, Bestiary bestiary)
        {
            WriteLevelPayouts(campaign, upgrades);
            CampaignModel model = BalanceLedger.Campaign(campaign, upgrades, unlocks.StartingTowerIds, BalanceSheet.Rules);
            foreach (EnemyDefinition enemy in bestiary.All)
            {
                model.Enemies[enemy.name] = BalanceLedger.Stats(enemy);
            }

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
                WriteWaveSets(level, campaign, sets, bestiary, model.PlayerAt(index, WaveSolver.SolveDifficulty).MeanTowerBonus);
            }
        }

        private static string Key(LevelDefinition level) => level.name.StartsWith("Level_") ? level.name.Substring(6) : level.name;

        // Research grows level by level and sums to the Easy share of all research upgrades; power pays per boss.
        private static void WriteLevelPayouts(CampaignCatalog campaign, UpgradeCatalog upgrades)
        {
            int researchCost = 0;
            foreach (UpgradeDefinition upgrade in upgrades.Upgrades)
            {
                if (upgrade.Currency == UpgradeCurrency.ResearchPoints)
                {
                    for (int i = 0; i < upgrade.MaxLevel; i++)
                    {
                        researchCost += upgrade.CostForNextLevel(i);
                    }
                }
            }

            foreach (MapNode node in campaign.Nodes)
            {
                if (node.Level == null || WaveScripts.ByKey(Key(node.Level)) == null)
                {
                    continue;
                }

                var so = new SerializedObject(node.Level);
                SerializedProperty reward = so.FindProperty("_reward");
                reward.FindPropertyRelative("ResearchPoints").intValue = BalanceSheet.Research(Key(node.Level), researchCost);
                reward.FindPropertyRelative("PowerCurrency").intValue = BalanceSheet.Power(WaveScripts.ByKey(Key(node.Level)));
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void WriteWaveSets(LevelDefinition level, CampaignCatalog campaign, List<List<WaveSpec>> sets, Bestiary bestiary, float expectedBonus)
        {
            var so = new SerializedObject(level);
            SerializedProperty waveArray = so.FindProperty("_difficultyWaves");
            waveArray.arraySize = sets.Count;
            for (int d = 0; d < sets.Count; d++)
            {
                SerializedProperty element = waveArray.GetArrayElementAtIndex(d);
                element.FindPropertyRelative("Difficulty").objectReferenceValue = campaign.Difficulties[d];
                SerializedProperty waves = element.FindPropertyRelative("Waves");
                waves.arraySize = sets[d].Count;
                for (int i = 0; i < sets[d].Count; i++)
                {
                    WaveDefinition wave = WriteWave($"{DataRoot}/Waves/{Key(level)}_{DifficultyNames[d]}_{i + 1}.asset", i, sets[d][i], bestiary);
                    waves.GetArrayElementAtIndex(i).objectReferenceValue = wave;
                }
            }

            // For reference only: the upgrade a Medium player is expected to bring; the waves are already sized for it.
            so.FindProperty("_expectedUpgradeBonus").floatValue = Mathf.Round(Mathf.Clamp01(expectedBonus) * 100f) / 100f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static WaveDefinition WriteWave(string path, int index, WaveSpec spec, Bestiary bestiary)
        {
            var wave = LoadOrCreate<WaveDefinition>(path);
            var so = new SerializedObject(wave);
            so.FindProperty("_startDelay").floatValue = index == 0 ? FirstWaveDelay : WaveDelay;
            so.FindProperty("_healthMultiplier").floatValue = spec.HealthMultiplier;
            SerializedProperty array = so.FindProperty("_groups");
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

            so.ApplyModifiedPropertiesWithoutUndo();
            return wave;
        }
    }
}
