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
    /// The acceptance of a cell as played (round 83b; it replaces BalanceAcceptanceTests, which judged recorded plans by
    /// the rules of round 22 and was red on 17 of 24 cells). A cell is accepted when the break-even of its difficulty's
    /// reference rung, read from the latest pressure measurement, lies within one step of the ladder of its target:
    /// the same targets and the same reading as Tools/PlayTest/closed_loop.py, which sets the cell's scalar.
    ///
    ///   Easy    both rungs win every seed at the data; the weaker rung holds the whole ladder (target x1.4)
    ///   Medium  the sensible rung breaks even at x1.18; the better of the two rungs at x1.06 at least
    ///   Hard    the sensible rung breaks even at x0.95; the better of the two rungs at x1.06 at least
    ///   Psycho  the better of the two rungs breaks even at x1.06
    ///   and on every level Hard breaks the sensible rung earlier than Medium does.
    ///
    /// The measurement is named in Tools/PlayTest/closed_loop_records.txt (one records file per line). It must be of
    /// the data as it stands: with another data stamp the test is inconclusive (measure first; Docs/COMMANDS.md,
    /// "Round 83b"), never passed. Writes Tools/PlayTest/closed_loop_acceptance.txt.
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

        /// <summary>The reference rung's target per difficulty (closed_loop.py, TARGETS).</summary>
        private static readonly float[] Target = { 1.4f, 1.18f, 0.95f, 1.06f };

        /// <summary>What the better of the two rungs must reach at least on Medium and Hard.</summary>
        private const float SkilledGuard = 1.06f;

        /// <summary>Seeds of three that hold a multiple.</summary>
        private const int SeedsHeld = 2;

        /// <summary>One step of the ladder (x0.8 / 0.9 / 1 / 1.12 / 1.25: the widest is 1.125) with the rounding of the targets.</summary>
        private const float LadderStep = 1.125f * 1.02f;

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
            public readonly bool WinsAllAtTheData;

            public Reading(float value, End end, string row, bool winsAllAtTheData)
            {
                Value = value;
                End = end;
                Row = row;
                Played = true;
                WinsAllAtTheData = winsAllAtTheData;
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

            var report = new StringBuilder($"Closed-loop acceptance of {string.Join(", ", files)} (data {measured}; the project's data is {now})\n");
            report.Append("| Cell | Sensible | Skilled | Target | Verdict |\n| --- | --- | --- | --- | --- |\n");
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
                    string verdict = Judge(d, sensible, skilled);
                    cells++;
                    report.Append($"| {name} | {sensible.Text} | {skilled.Text} | {TargetText(d)} | {verdict ?? "accepted"} |\n");
                    if (verdict != null)
                    {
                        failures.Add($"{name}: {verdict} (sensible {sensible.Text}, skilled {skilled.Text})");
                    }
                }

                // The same player meets Hard earlier than Medium on every level (the critic's item 3). An open end is read as its bound.
                if (sensibleOf.Length > Hard && sensibleOf[Medium].Played && sensibleOf[Hard].Played && sensibleOf[Hard].Value >= sensibleOf[Medium].Value)
                {
                    failures.Add($"{level}: Hard is not above Medium for the sensible rung (Hard {sensibleOf[Hard].Text}, Medium {sensibleOf[Medium].Text})");
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
        private static string Judge(int difficulty, Reading sensible, Reading skilled)
        {
            if (!sensible.Played || !skilled.Played)
            {
                return "not measured on both rungs";
            }

            // The skilled rung stands for the better reference player: where its picked strategy breaks before the sensible
            // rung does, the pick is stale and the sensible rung is read in its place (closed_loop.py). Not on Easy, where
            // the weaker rung is the measure.
            Reading better = sensible.Value > skilled.Value ? sensible : skilled;
            switch (difficulty)
            {
                case Easy:
                    if (!sensible.WinsAllAtTheData || !skilled.WinsAllAtTheData)
                    {
                        return "a seed is lost at the data";
                    }

                    Reading weaker = sensible.Value < skilled.Value ? sensible : skilled;
                    return weaker.End == End.OverTheLadder ? null : "the weaker rung does not hold the whole ladder";
                case Medium:
                case Hard:
                    return Near(sensible, Target[difficulty], "the sensible rung") ?? AtLeast(better, SkilledGuard, "the better rung");
                default:
                    return Near(better, Target[Psycho], "the better rung");
            }
        }

        // A binding target is met from both sides, and by a measured break-even: a rung that holds the whole ladder or
        // loses all of it is at least a step off and nobody knows how far.
        private static string Near(Reading reading, float target, string who)
        {
            if (reading.End != End.Measured)
            {
                return $"{who} {(reading.End == End.OverTheLadder ? "holds the whole ladder" : "loses the whole ladder")} (target x{target.ToString("0.00", CultureInfo.InvariantCulture)})";
            }

            float off = reading.Value / target;
            return off > LadderStep || off < 1f / LadderStep
                ? $"{who} breaks even at x{reading.Value.ToString("0.00", CultureInfo.InvariantCulture)}, more than a step from x{target.ToString("0.00", CultureInfo.InvariantCulture)}"
                : null;
        }

        private static string AtLeast(Reading reading, float guard, string who) =>
            reading.End == End.OverTheLadder || reading.Value >= guard / LadderStep
                ? null
                : $"{who} breaks even at x{reading.Value.ToString("0.00", CultureInfo.InvariantCulture)}, more than a step under its guard x{guard.ToString("0.00", CultureInfo.InvariantCulture)}";

        private static string TargetText(int difficulty)
        {
            switch (difficulty)
            {
                case Easy:
                    return "both hold the ladder";
                case Medium:
                    return "sensible x1.18, better rung x1.06 at least";
                case Hard:
                    return "sensible x0.95, better rung x1.06 at least";
                default:
                    return "better rung x1.06";
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
            bool allAtTheData = false;
            foreach (float multiple in multiples)
            {
                row.Append(row.Length > 0 ? " " : string.Empty).Append(wins[(level, difficulty, rung, multiple)]);
                if (Math.Abs(multiple - 1f) < 1e-4f)
                {
                    allAtTheData = wins[(level, difficulty, rung, multiple)] == played[(level, difficulty, rung, multiple)];
                }
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
                    ? new Reading(multiple / (float)Math.Sqrt(multiples[1] / multiples[0]), End.UnderTheLadder, row.ToString(), allAtTheData)
                    : new Reading((float)Math.Sqrt(held * multiple), End.Measured, row.ToString(), allAtTheData);
            }

            float step = multiples[multiples.Count - 1] / multiples[multiples.Count - 2];
            return new Reading(held * (float)Math.Sqrt(step), End.OverTheLadder, row.ToString(), allAtTheData);
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
