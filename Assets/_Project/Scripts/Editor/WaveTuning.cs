using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Balance;

namespace Game.Editor
{
    /// <summary>
    /// A cell's scalar from the closed loop (round 83): per level and difficulty one factor on the health of every creature,
    /// measured against the probe (Tools/PlayTest/closed_loop.py), never typed. Kept in Tools/PlayTest/wave_tuning.txt, one
    /// line "Key difficulty scale" per cell; a cell not listed is at one. Read by <see cref="WaveScripts"/> every time the
    /// scripts are built, so the generator, the BalanceLab tool and the tests all see the same scripts. The file holds
    /// nothing else: the builder's per-cell composition levers of rounds 22 to 68 (plated, swarm, tempo, raid, bosscrowd,
    /// armourgrowth) are rules per difficulty in <see cref="BalanceRules"/> or gone since round 84. Pure C#.
    /// </summary>
    public static class WaveTuning
    {
        public const string Path = "Tools/PlayTest/wave_tuning.txt";

        /// <summary>Applies the file's scalars to the scripts (a script not listed keeps 1 on every difficulty).</summary>
        public static void Apply(IEnumerable<WaveScriptSpec> scripts)
        {
            Dictionary<(string, int), float> scales = Read();
            foreach (WaveScriptSpec script in scripts)
            {
                var values = new float[4];
                bool any = false;
                for (int d = 0; d < values.Length; d++)
                {
                    values[d] = scales.TryGetValue((script.Key, d), out float scale) ? scale : 1f;
                    any |= values[d] != 1f;
                }

                script.TargetScale = any ? values : new[] { 1f };
                script.GraceWaves = GrowthWindow.GraceWaves;
                script.FallWithinWaves = GrowthWindow.FallWithinWaves;
            }
        }

        public static Dictionary<(string Key, int Difficulty), float> Read()
        {
            var scales = new Dictionary<(string, int), float>();
            foreach (string[] parts in Lines())
            {
                if (parts.Length == 3 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int difficulty)
                    && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
                {
                    scales[(parts[0], difficulty)] = scale;
                }
            }

            return scales;
        }

        /// <summary>The lines of the file that are not a cell's scalar: none are allowed (a test holds the file to its format).</summary>
        public static List<string> OtherLines()
        {
            var others = new List<string>();
            foreach (string[] parts in Lines())
            {
                bool scalar = parts.Length == 3 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                    && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                if (!scalar && string.Join(" ", parts).Trim().Length > 0)
                {
                    others.Add(string.Join(" ", parts));
                }
            }

            return others;
        }

        public static void Write(Dictionary<(string Key, int Difficulty), float> scales)
        {
            var lines = new List<string>();
            foreach (KeyValuePair<(string Key, int Difficulty), float> pair in scales)
            {
                if (pair.Value != 1f)
                {
                    lines.Add(pair.Key.Key + " " + pair.Key.Difficulty.ToString(CultureInfo.InvariantCulture) + " " + pair.Value.ToString("0.###", CultureInfo.InvariantCulture));
                }
            }

            lines.Sort(System.StringComparer.Ordinal);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            File.WriteAllLines(Path, lines);
        }

        private static IEnumerable<string[]> Lines()
        {
            if (!File.Exists(Path))
            {
                yield break;
            }

            foreach (string line in File.ReadAllLines(Path))
            {
                yield return line.Split(' ');
            }
        }
    }
}
