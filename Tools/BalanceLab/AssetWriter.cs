using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Game.Balance;

namespace BalanceLab
{
    /// <summary>
    /// Writes the solved balance into the existing asset files, field by field, in the form the generator writes them:
    /// wave groups and health multipliers, kill rewards, tower prices, difficulty profiles and level rewards. It only
    /// rewrites files that already exist (same GUIDs, no new .meta files), so a wave count can never change here.
    /// </summary>
    public sealed class AssetWriter
    {
        private readonly GameData _data;
        private readonly CampaignModel _model;
        private int _files;

        public AssetWriter(GameData data, CampaignModel model)
        {
            _data = data;
            _model = model;
        }

        public void WriteAll(Dictionary<int, List<List<WaveSpec>>> waves)
        {
            foreach (var pair in waves)
            {
                WriteWaves(_data.Levels[pair.Key], pair.Value);
            }

            foreach (EnemyStats enemy in _model.Enemies.Values)
            {
                Replace(_data.EnemyPaths[enemy.Id], "_reward", enemy.Reward.ToString(CultureInfo.InvariantCulture));
            }

            foreach (TowerStats tower in _model.Towers.Values)
            {
                if (BalanceSheet(tower.Id))
                {
                    WriteTowerPrices(_data.TowerPaths[tower.Id], tower);
                }
            }

            for (int d = 0; d < _model.Difficulties.Count; d++)
            {
                DifficultyStats stats = _model.Difficulties[d];
                string path = _data.DifficultyPaths[d];
                Replace(path, "_healthMultiplier", F(stats.Health));
                Replace(path, "_countMultiplier", F(stats.Count));
                Replace(path, "_spawnIntervalMultiplier", F(stats.Interval));
                Replace(path, "_rewardMultiplier", F(stats.Research));
                Replace(path, "_powerCurrencyMultiplier", F(stats.Power));
                Replace(path, "_structureDamageMultiplier", F(stats.StructureDamage));
            }

            for (int l = 0; l < _model.Levels.Count; l++)
            {
                string path = _data.Levels[l].Path;
                Replace(path, "ResearchPoints", _model.Levels[l].Research.ToString(CultureInfo.InvariantCulture), "    ");
                Replace(path, "PowerCurrency", _model.Levels[l].Power.ToString(CultureInfo.InvariantCulture), "    ");
                float bonus = Math.Min(1f, Math.Max(0f, _model.PlayerAt(l, WaveSolver.SolveDifficulty).MeanTowerBonus));
                Replace(path, "_expectedUpgradeBonus", F((float)Math.Round(bonus, 2)));
            }

            Console.WriteLine($"Wrote {_files} asset fields/files.");
        }

        public void WriteWavesOnly(Dictionary<int, List<List<WaveSpec>>> waves)
        {
            foreach (var pair in waves)
            {
                WriteWaves(_data.Levels[pair.Key], pair.Value);
            }

            Console.WriteLine($"Wrote {_files} wave files.");
        }

        private static bool BalanceSheet(string towerId) => Game.Editor.BalanceSheet.Prices(towerId) != null;

        /// <summary>Unity writes floats in their shortest round-trip form.</summary>
        public static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        private void WriteWaves(LevelData level, List<List<WaveSpec>> sets)
        {
            for (int d = 0; d < sets.Count && d < level.WavePaths.Count; d++)
            {
                if (sets[d].Count != level.WavePaths[d].Count)
                {
                    throw new InvalidOperationException($"{level.Key}: the script has {sets[d].Count} waves but the level has {level.WavePaths[d].Count} assets; change wave counts in Unity.");
                }

                for (int i = 0; i < sets[d].Count; i++)
                {
                    WriteWave(level.WavePaths[d][i], sets[d][i]);
                }
            }
        }

        private void WriteWave(string path, WaveSpec wave)
        {
            string[] lines = File.ReadAllLines(path);
            var output = new List<string>();
            bool skipping = false;
            foreach (string line in lines)
            {
                if (line.StartsWith("  _groups:", StringComparison.Ordinal))
                {
                    output.AddRange(Groups(wave));
                    skipping = true;
                    continue;
                }

                if (skipping && (line.StartsWith("  - ", StringComparison.Ordinal) || line.StartsWith("    ", StringComparison.Ordinal)))
                {
                    continue;
                }

                skipping = false;
                output.Add(line.StartsWith("  _healthMultiplier:", StringComparison.Ordinal) ? "  _healthMultiplier: " + F(wave.HealthMultiplier) : line);
            }

            Save(path, output);
        }

        private IEnumerable<string> Groups(WaveSpec wave)
        {
            if (wave.Groups.Count == 0)
            {
                yield return "  _groups: []";
                yield break;
            }

            yield return "  _groups:";
            foreach (GroupSpec g in wave.Groups)
            {
                yield return $"  - Enemy: {{fileID: 11400000, guid: {_data.EnemyGuids[g.Enemy.Id]}, type: 2}}";
                yield return $"    Count: {g.Count}";
                yield return $"    PathIndex: {g.Path}";
                yield return $"    Source: {g.Source}";
                yield return $"    Interval: {F(g.Interval)}";
                yield return $"    Delay: {F(g.Delay)}";
                yield return $"    Stagger: {F(g.Stagger)}";
            }
        }

        private void WriteTowerPrices(string path, TowerStats tower)
        {
            string[] lines = File.ReadAllLines(path);
            int level = 0;
            for (int i = 0; i < lines.Length && level < tower.Levels.Length; i++)
            {
                if (lines[i].StartsWith("  - Cost: ", StringComparison.Ordinal))
                {
                    lines[i] = "  - Cost: " + tower.Levels[level].Cost.ToString(CultureInfo.InvariantCulture);
                    level++;
                }
            }

            Save(path, new List<string>(lines));
        }

        // Replaces the value of one top-level field (or a nested one at the given indentation).
        private void Replace(string path, string field, string value, string indent = "  ")
        {
            string text = File.ReadAllText(path);
            var pattern = new Regex("^" + Regex.Escape(indent + field) + ": .*$", RegexOptions.Multiline);
            if (!pattern.IsMatch(text))
            {
                throw new InvalidOperationException($"{path}: no field {field}");
            }

            string updated = pattern.Replace(text, indent + field + ": " + value, 1);
            if (updated != text)
            {
                File.WriteAllText(path, updated);
                _files++;
            }
        }

        private void Save(string path, List<string> lines)
        {
            var text = new StringBuilder();
            foreach (string line in lines)
            {
                text.Append(line).Append('\n');
            }

            string updated = text.ToString();
            if (updated != File.ReadAllText(path))
            {
                File.WriteAllText(path, updated);
                _files++;
            }
        }
    }
}
