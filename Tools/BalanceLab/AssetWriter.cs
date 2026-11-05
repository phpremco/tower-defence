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
        private const string DifficultyLine = "  - Difficulty:";
        private const string WaveLine = "    - _startDelay:";
        private const string GroupIndent = "      ";
        private const string GroupsLine = GroupIndent + "_groups:";
        private const string HealthLine = GroupIndent + "_healthMultiplier:";

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
                ReplaceOrAdd(path, "_incomeMultiplier", F(stats.Income), "_structureDamageMultiplier");
                ReplaceOrAdd(path, "_startingMoneyMultiplier", F(stats.StartMoney), "_incomeMultiplier");
                ReplaceOrAdd(path, "_bossHealthShare", F(stats.BossHealth), "_startingMoneyMultiplier");
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

        // Rewrites the groups and health multiplier of every wave in the level's one LevelWaves file, in place. Unity writes
        // each difficulty as "  - Difficulty:" and each wave as a list item that starts with its first field, _startDelay.
        private void WriteWaves(LevelData level, List<List<WaveSpec>> sets)
        {
            if (level.WavesPath == null || sets.Count != level.Waves.Count)
            {
                throw new InvalidOperationException($"{level.Key}: the script has {sets.Count} difficulties but the level has {level.Waves.Count}; generate the waves in Unity.");
            }

            for (int d = 0; d < sets.Count; d++)
            {
                if (sets[d].Count != level.Waves[d].Count)
                {
                    throw new InvalidOperationException($"{level.Key}: the script has {sets[d].Count} waves but the level has {level.Waves[d].Count}; change wave counts in Unity.");
                }
            }

            var output = new List<string>();
            int difficulty = -1;
            int wave = -1;
            bool skipping = false;
            foreach (string line in File.ReadAllLines(level.WavesPath))
            {
                if (line.StartsWith(DifficultyLine, StringComparison.Ordinal))
                {
                    difficulty++;
                    wave = -1;
                }
                else if (line.StartsWith(WaveLine, StringComparison.Ordinal))
                {
                    wave++;
                }

                if (line.StartsWith(GroupsLine, StringComparison.Ordinal))
                {
                    output.AddRange(Groups(sets[difficulty][wave]));
                    skipping = true;
                    continue;
                }

                if (skipping && (line.StartsWith(GroupIndent + "- ", StringComparison.Ordinal) || line.StartsWith(GroupIndent + "  ", StringComparison.Ordinal)))
                {
                    continue;
                }

                skipping = false;
                output.Add(line.StartsWith(HealthLine, StringComparison.Ordinal) ? HealthLine + " " + F(sets[difficulty][wave].HealthMultiplier) : line);
            }

            Save(level.WavesPath, output);
        }

        private IEnumerable<string> Groups(WaveSpec wave)
        {
            if (wave.Groups.Count == 0)
            {
                yield return GroupsLine + " []";
                yield break;
            }

            yield return GroupsLine;
            foreach (GroupSpec g in wave.Groups)
            {
                yield return $"{GroupIndent}- Enemy: {{fileID: 11400000, guid: {_data.EnemyGuids[g.Enemy.Id]}, type: 2}}";
                yield return $"{GroupIndent}  Count: {g.Count}";
                yield return $"{GroupIndent}  PathIndex: {g.Path}";
                yield return $"{GroupIndent}  Source: {g.Source}";
                yield return $"{GroupIndent}  Interval: {F(g.Interval)}";
                yield return $"{GroupIndent}  Delay: {F(g.Delay)}";
                yield return $"{GroupIndent}  Stagger: {F(g.Stagger)}";
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

        // A field Unity has not written yet (new on the class, still at its default) goes in right after its neighbour.
        private void ReplaceOrAdd(string path, string field, string value, string after)
        {
            string text = File.ReadAllText(path);
            if (new Regex("^  " + Regex.Escape(field) + ": ", RegexOptions.Multiline).IsMatch(text))
            {
                Replace(path, field, value);
                return;
            }

            var anchor = new Regex("^  " + Regex.Escape(after) + ": .*$", RegexOptions.Multiline);
            if (!anchor.IsMatch(text))
            {
                throw new InvalidOperationException($"{path}: no field {after}");
            }

            File.WriteAllText(path, anchor.Replace(text, m => m.Value + "\n  " + field + ": " + value, 1));
            _files++;
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
