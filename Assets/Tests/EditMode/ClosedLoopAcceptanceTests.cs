using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// The acceptance of the campaign as played (round 83b; the gates of round 87). The thresholds are frozen in
    /// Tools/PlayTest/closed_loop_thresholds.txt (2026-11-20, before any measurement of round 87), the one place
    /// Tools/PlayTest/closed_loop.py reads them from too; this test holds no number of its own.
    ///
    ///   Every cell: its reference rung (Easy the weaker of sensible and skilled, Medium and Hard the sensible rung,
    ///   Psycho the skilled rung) breaks even within one ladder step of its target, on both sides, by a real reading:
    ///   a rung that holds or loses its whole ladder is a finding. Easy has an upper bound like every difficulty
    ///   (round 86 accepted "over x1.86"; the Easy ladder now reaches x2.21, Tools/PlayTest/r87_measure.sh).
    ///   Medium and Hard: the best of the better rungs (skilled, laser, cannon) itself reads the guard (round 86
    ///   accepted the sensible rung in the skilled rung's place, with both a step under).
    ///   Every cell: the laser and the cannon archetype read within one ladder step of each other.
    ///   Every level: Hard breaks the sensible rung earlier than Medium does, and of its lost runs no more than the
    ///   gate's share fall in a boss wave (the critic: levels 1 and 2 were a safe corridor with a boss wall).
    ///
    /// The measurement is named in Tools/PlayTest/closed_loop_records.txt (one records file per line). It must be of
    /// the data as it stands: with another data stamp the test is inconclusive (measure first), never passed. Writes
    /// Tools/PlayTest/closed_loop_acceptance.txt.
    /// </summary>
    public sealed class ClosedLoopAcceptanceTests
    {
        private const string PointerPath = "Tools/PlayTest/closed_loop_records.txt";
        private const string ReportPath = "Tools/PlayTest/closed_loop_acceptance.txt";
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string DataFolder = "Assets/_Project/Data";
        private const string Sensible = "sensible";
        private const string Skilled = "skilled";
        private const int Easy = 0;
        private const int Medium = 1;
        private const int Hard = 2;
        private const int Psycho = 3;

        private const string ThresholdsPath = "Tools/PlayTest/closed_loop_thresholds.txt";
        private const string Laser = "laser";
        private const string Cannon = "cannon";

        /// <summary>Seeds of three that hold a multiple.</summary>
        private const int SeedsHeld = 2;

        /// <summary>The frozen thresholds, by their names in the file.</summary>
        private sealed class Thresholds
        {
            private readonly Dictionary<string, float> _values = new Dictionary<string, float>();

            public Thresholds(string path)
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length == 2 && !line.StartsWith("#") && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    {
                        _values[parts[0]] = value;
                    }
                }
            }

            public float this[string name] => _values[name];

            public float Target(int difficulty) => _values[difficulty == Easy ? "target_easy" : difficulty == Medium ? "target_medium" : difficulty == Hard ? "target_hard" : "target_psycho"];

            /// <summary>The widest ratio of two readings that are this many ladder steps apart, with the rounding of the readings.</summary>
            public float Steps(string count) => (float)Math.Pow(_values["ladder_step"], _values[count]) * _values["rounding"];
        }

        /// <summary>The field of a run's record that holds the wave it reached: the wave a lost run fell in.</summary>
        private const int LostWaveField = 7;

        private const byte CarriageReturn = 13;
        private const int HashLength = 8;

        private enum End
        {
            Measured,
            UnderTheLadder,
            OverTheLadder,
        }

        private readonly struct Reading
        {
            public readonly float Value;
            public readonly End End;
            public readonly string Row;
            public readonly bool Played;

            public Reading(float value, End end, string row)
            {
                Value = value;
                End = end;
                Row = row;
                Played = true;
            }

            public string Text => !Played ? "not played" : $"`{Row}` {(End == End.UnderTheLadder ? "under " : End == End.OverTheLadder ? "over " : string.Empty)}x{Value.ToString("0.00", CultureInfo.InvariantCulture)}";
        }

        [Test]
        public void EveryCell_BreaksEvenWithinOneLadderStepOfItsTarget()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            Assert.IsTrue(File.Exists(PointerPath), $"{PointerPath} names no measurement: run the pressure batch and write its records file there (Docs/COMMANDS.md, \"Round 83b\")");
            var files = new List<string>();
            foreach (string line in File.ReadAllLines(PointerPath))
            {
                if (line.Trim().Length > 0 && !line.TrimStart().StartsWith("#"))
                {
                    files.Add(line.Trim());
                }
            }

            Assert.IsNotEmpty(files, $"{PointerPath} names no records file");
            var wins = new Dictionary<(string Level, int Difficulty, string Rung, float Multiple), int>();
            var played = new Dictionary<(string Level, int Difficulty, string Rung, float Multiple), int>();
            var stamps = new HashSet<string>();
            foreach (string file in files)
            {
                Assert.IsTrue(File.Exists(file), $"{PointerPath} names {file}, which does not exist");
                foreach (string line in File.ReadAllLines(file))
                {
                    string[] f = line.Split('|');
                    if (f.Length < 7 || f[0] != "prun")
                    {
                        continue;
                    }

                    var key = (f[1], int.Parse(f[2], CultureInfo.InvariantCulture), f[3], float.Parse(f[5], CultureInfo.InvariantCulture));
                    played[key] = (played.TryGetValue(key, out int runs) ? runs : 0) + 1;
                    wins[key] = (wins.TryGetValue(key, out int won) ? won : 0) + (f[6] == "won" ? 1 : 0);
                    Match stamp = Regex.Match(line, "data=([0-9a-f]+)");
                    stamps.Add(stamp.Success ? stamp.Groups[1].Value : "unstamped");
                }
            }

            Assert.IsNotEmpty(played, $"no runs in {string.Join(", ", files)}");
            Assert.AreEqual(1, stamps.Count, $"the records are of {stamps.Count} data states ({string.Join(", ", stamps)}): one measurement, one data state");
            string measured = new List<string>(stamps)[0];
            string now = DataStamp();

            Assert.IsTrue(File.Exists(ThresholdsPath), $"{ThresholdsPath} is missing: the frozen thresholds of the closed loop");
            var frozen = new Thresholds(ThresholdsPath);
            var report = new StringBuilder($"Closed-loop acceptance of {string.Join(", ", files)} (data {measured}; the project's data is {now}; thresholds {ThresholdsPath})\n");
            report.Append("| Cell | Sensible | Skilled | Laser | Cannon | Target | Verdict |\n| --- | --- | --- | --- | --- | --- | --- |\n");
            var failures = new List<string>();
            int cells = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                string level = node.Level.name;
                var sensibleOf = new Reading[catalog.Difficulties.Count];
                for (int d = 0; d < catalog.Difficulties.Count; d++)
                {
                    Reading sensible = Read(wins, played, level, d, Sensible);
                    Reading skilled = Read(wins, played, level, d, Skilled);
                    sensibleOf[d] = sensible;
                    string name = $"{level} {catalog.Difficulties[d].name}";
                    Reading laser = Read(wins, played, level, d, Laser);
                    Reading cannon = Read(wins, played, level, d, Cannon);
                    string verdict = Judge(frozen, d, sensible, skilled, laser, cannon);
                    cells++;
                    report.Append($"| {name} | {sensible.Text} | {skilled.Text} | {laser.Text} | {cannon.Text} | {TargetText(frozen, d)} | {verdict ?? "accepted"} |\n");
                    if (verdict != null)
                    {
                        failures.Add($"{name}: {verdict} (sensible {sensible.Text}, skilled {skilled.Text}, laser {laser.Text}, cannon {cannon.Text})");
                    }
                }

                // The same player meets Hard earlier than Medium on every level (the critic's item 3). An open end is read as its bound.
                if (sensibleOf.Length > Hard && sensibleOf[Medium].Played && sensibleOf[Hard].Played && sensibleOf[Hard].Value >= sensibleOf[Medium].Value)
                {
                    failures.Add($"{level}: Hard is not above Medium for the sensible rung (Hard {sensibleOf[Hard].Text}, Medium {sensibleOf[Medium].Text})");
                }

                string wall = BossWall(frozen, node.Level, catalog, files);
                if (wall != null)
                {
                    failures.Add(wall);
                }
            }

            report.Append(failures.Count == 0 ? $"\nAll {cells} cells accepted.\n" : $"\n{failures.Count} findings:\n  " + string.Join("\n  ", failures) + "\n");
            File.WriteAllText(ReportPath, report.ToString());
            Assert.Greater(cells, 0);
            if (measured != now)
            {
                Assert.Inconclusive($"The measurement in {PointerPath} is of data {measured}; the project's data is {now}. Measure the data as it stands (Docs/COMMANDS.md, \"Round 83b\") before the acceptance says anything. As measured: {(failures.Count == 0 ? "all accepted" : string.Join("; ", failures))}");
            }

            Assert.IsEmpty(failures, string.Join("\n", failures) + "\nSee " + ReportPath);
        }

        // Null when the cell is accepted, else what is off.
        private static string Judge(Thresholds frozen, int difficulty, Reading sensible, Reading skilled, Reading laser, Reading cannon)
        {
            if (!sensible.Played || !skilled.Played || !laser.Played || !cannon.Played)
            {
                return "not measured on all four rungs";
            }

            float target = frozen.Target(difficulty);
            string reference;
            switch (difficulty)
            {
                case Easy:
                    reference = Near(frozen, sensible.Value < skilled.Value ? sensible : skilled, target, "the weaker rung");
                    break;
                case Medium:
                case Hard:
                    Reading best = skilled;
                    foreach (Reading other in new[] { laser, cannon })
                    {
                        best = other.Value > best.Value ? other : best;
                    }

                    reference = Near(frozen, sensible, target, "the sensible rung") ?? AtLeast(frozen, best, "the best of the better rungs");
                    break;
                default:
                    reference = Near(frozen, skilled, target, "the skilled rung");
                    break;
            }

            return reference ?? Together(frozen, laser, cannon);
        }

        // A target is met from both sides, and by a measured break-even: a rung that holds the whole ladder or loses all
        // of it is at least a step off and nobody knows how far.
        private static string Near(Thresholds frozen, Reading reading, float target, string who)
        {
            if (reading.End != End.Measured)
            {
                return $"{who} {(reading.End == End.OverTheLadder ? "holds the whole ladder" : "loses the whole ladder")} (target x{target.ToString("0.00", CultureInfo.InvariantCulture)})";
            }

            float step = frozen.Steps("accept_within_steps");
            float off = reading.Value / target;
            return off > step || off < 1f / step
                ? $"{who} breaks even at x{reading.Value.ToString("0.00", CultureInfo.InvariantCulture)}, more than a step from x{target.ToString("0.00", CultureInfo.InvariantCulture)}"
                : null;
        }

        // The guard is read as it is written: no step of slack under it, only the rounding of the reading.
        private static string AtLeast(Thresholds frozen, Reading reading, string who)
        {
            float guard = frozen["better_rung_least"];
            return reading.End == End.OverTheLadder || reading.Value >= guard / frozen["rounding"]
                ? null
                : $"{who} breaks even at x{reading.Value.ToString("0.00", CultureInfo.InvariantCulture)}, under its guard x{guard.ToString("0.00", CultureInfo.InvariantCulture)}";
        }

        // The two archetypes play the same cell about as well: a level that only one favourite gun wins is a level
        // with one answer.
        private static string Together(Thresholds frozen, Reading laser, Reading cannon)
        {
            float step = frozen.Steps("archetype_spread_steps");
            float ratio = laser.Value / cannon.Value;
            return ratio > step || ratio < 1f / step ? "the laser and the cannon archetype are more than a ladder step apart" : null;
        }

        // Of the lost runs of a level (every difficulty, rung and multiple of the measurement) the share that fell in a
        // wave with a boss in it; null when it is inside the gate.
        private static string BossWall(Thresholds frozen, LevelDefinition level, CampaignCatalog catalog, List<string> files)
        {
            int lost = 0;
            int inBossWave = 0;
            foreach (string file in files)
            {
                foreach (string line in File.ReadAllLines(file))
                {
                    string[] f = line.Split('|');
                    if (f.Length <= LostWaveField || f[0] != "prun" || f[1] != level.name || f[6] != "lost")
                    {
                        continue;
                    }

                    int difficulty = int.Parse(f[2], CultureInfo.InvariantCulture);
                    int wave = int.Parse(f[LostWaveField], CultureInfo.InvariantCulture);
                    WaveDefinition[] waves = level.GetWaves(catalog.Difficulties[difficulty]);
                    lost++;
                    if (wave >= 1 && wave <= waves.Length && HasBoss(waves[wave - 1]))
                    {
                        inBossWave++;
                    }
                }
            }

            float most = frozen["boss_wave_share_most"];
            return lost > 0 && inBossWave > lost * most
                ? $"{level.name}: {inBossWave} of {lost} lost runs fell in a boss wave ({(inBossWave / (float)lost).ToString("P0", CultureInfo.InvariantCulture)}; at most {most.ToString("P0", CultureInfo.InvariantCulture)})"
                : null;
        }

        private static bool HasBoss(WaveDefinition wave)
        {
            foreach (SpawnGroup group in wave.Groups)
            {
                if (group.Enemy != null && group.Enemy.IsBoss)
                {
                    return true;
                }
            }

            return false;
        }

        private static string TargetText(Thresholds frozen, int difficulty)
        {
            string target = frozen.Target(difficulty).ToString("0.00", CultureInfo.InvariantCulture);
            string guard = frozen["better_rung_least"].ToString("0.00", CultureInfo.InvariantCulture);
            switch (difficulty)
            {
                case Easy:
                    return $"weaker rung x{target}";
                case Medium:
                case Hard:
                    return $"sensible x{target}, best better rung x{guard} at least";
                default:
                    return $"skilled x{target}";
            }
        }

        // The break-even of one rung on one cell, as closed_loop.py reads it: the geometric middle of the last multiple
        // held from the bottom up (two of three seeds) and the first one lost.
        private static Reading Read(Dictionary<(string, int, string, float), int> wins, Dictionary<(string, int, string, float), int> played, string level, int difficulty, string rung)
        {
            var multiples = new List<float>();
            foreach ((string Level, int Difficulty, string Rung, float Multiple) key in played.Keys)
            {
                if (key.Level == level && key.Difficulty == difficulty && key.Rung == rung)
                {
                    multiples.Add(key.Multiple);
                }
            }

            if (multiples.Count < 2)
            {
                return default;
            }

            multiples.Sort();
            var row = new StringBuilder();
            foreach (float multiple in multiples)
            {
                row.Append(row.Length > 0 ? " " : string.Empty).Append(wins[(level, difficulty, rung, multiple)]);
            }

            float held = -1f;
            foreach (float multiple in multiples)
            {
                if (wins[(level, difficulty, rung, multiple)] >= SeedsHeld)
                {
                    held = multiple;
                    continue;
                }

                return held < 0f
                    ? new Reading(multiple / (float)Math.Sqrt(multiples[1] / multiples[0]), End.UnderTheLadder, row.ToString())
                    : new Reading((float)Math.Sqrt(held * multiple), End.Measured, row.ToString());
            }

            float step = multiples[multiples.Count - 1] / multiples[multiples.Count - 2];
            return new Reading(held * (float)Math.Sqrt(step), End.OverTheLadder, row.ToString());
        }

        // The data stamp of the probe records (ProbeStamp in the PlayMode tests): the names and bytes of every asset under
        // the data folder in ordinal order, carriage returns dropped.
        private static string DataStamp()
        {
            var files = new List<string>();
            foreach (string file in Directory.GetFiles(DataFolder, "*.asset", SearchOption.AllDirectories))
            {
                files.Add(file.Replace(Path.DirectorySeparatorChar, '/'));
            }

            files.Sort(StringComparer.Ordinal);
            using (MD5 md5 = MD5.Create())
            {
                foreach (string file in files)
                {
                    byte[] name = Encoding.UTF8.GetBytes(file);
                    md5.TransformBlock(name, 0, name.Length, null, 0);
                    byte[] bytes = Array.FindAll(File.ReadAllBytes(file), b => b != CarriageReturn);
                    md5.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }

                md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                var text = new StringBuilder();
                foreach (byte b in md5.Hash)
                {
                    text.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                }

                return text.ToString(0, HashLength);
            }
        }
    }
}
