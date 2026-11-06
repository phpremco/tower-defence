using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Balance;

namespace Game.Editor
{
    /// <summary>
    /// The builder bot's only lever (round 22): per level and difficulty a scale on the human target pressure the waves are
    /// calibrated to (1 = the curve; 1.15 = fifteen percent harder). Kept in Tools/PlayTest/wave_tuning.txt ("Key difficulty
    /// scale" per line), written by the builder stage for failing cells only, read by <see cref="WaveScripts"/> every time the
    /// scripts are built, so the generator, the BalanceLab tool and the tests all see the same tuned scripts. Pure C#.
    /// </summary>
    public static class WaveTuning
    {
        public const string Path = "Tools/PlayTest/wave_tuning.txt";

        /// <summary>Applies the file's scales to the scripts (a script not listed keeps 1 on every difficulty).</summary>
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
            }
        }

        public static Dictionary<(string Key, int Difficulty), float> Read()
        {
            var scales = new Dictionary<(string, int), float>();
            if (!File.Exists(Path))
            {
                return scales;
            }

            foreach (string line in File.ReadAllLines(Path))
            {
                string[] parts = line.Split(' ');
                if (parts.Length == 3 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int difficulty)
                    && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
                {
                    scales[(parts[0], difficulty)] = scale;
                }
            }

            return scales;
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
    }
}
