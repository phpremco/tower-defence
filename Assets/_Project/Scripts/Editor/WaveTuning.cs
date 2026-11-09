using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Balance;

namespace Game.Editor
{
    /// <summary>The builder's script-level levers of one cell (round 22e): plated weight, tempo and the raid wave; 1, 1 and 0 are "as written".</summary>
    public struct ScriptLever
    {
        public float Plated;
        public float Swarm;
        public float Tempo;
        public int Raid;

        public static ScriptLever None => new ScriptLever { Plated = 1f, Swarm = 1f, Tempo = 1f, Raid = 0 };

        public bool IsNone => Plated == 1f && Swarm == 1f && Tempo == 1f && Raid == 0;
    }

    /// <summary>
    /// The builder bot's levers (round 22): per level and difficulty a scale on the human target pressure the waves are
    /// calibrated to (1 = the curve; 1.15 = fifteen percent harder), and since round 22e the script-level levers for cells the
    /// scale cannot move (the curator's decision 4): plated troops weighted up, a faster tempo, an early raid. Kept in
    /// Tools/PlayTest/wave_tuning.txt ("Key difficulty scale" and "Key difficulty plated|tempo|raid value" per line), written by
    /// the builder stage for failing cells only, read by <see cref="WaveScripts"/> every time the scripts are built, so the
    /// generator, the BalanceLab tool and the tests all see the same tuned scripts. Pure C#.
    /// </summary>
    public static class WaveTuning
    {
        public const string Path = "Tools/PlayTest/wave_tuning.txt";
        private const string PlatedKey = "plated";
        private const string SwarmKey = "swarm";
        private const string TempoKey = "tempo";
        private const string RaidKey = "raid";

        /// <summary>Applies the file's scales and levers to the scripts (a script not listed keeps 1 on every difficulty).</summary>
        public static void Apply(IEnumerable<WaveScriptSpec> scripts)
        {
            Dictionary<(string, int), float> scales = Read();
            Dictionary<(string, int), ScriptLever> levers = ReadLevers();
            foreach (WaveScriptSpec script in scripts)
            {
                var values = new float[4];
                var plated = new float[4];
                var swarm = new float[4];
                var tempo = new float[4];
                var raid = new int[4];
                bool any = false;
                bool anyLever = false;
                for (int d = 0; d < values.Length; d++)
                {
                    values[d] = scales.TryGetValue((script.Key, d), out float scale) ? scale : 1f;
                    any |= values[d] != 1f;
                    ScriptLever lever = levers.TryGetValue((script.Key, d), out ScriptLever found) ? found : ScriptLever.None;
                    plated[d] = lever.Plated;
                    swarm[d] = lever.Swarm;
                    tempo[d] = lever.Tempo;
                    raid[d] = lever.Raid;
                    anyLever |= !lever.IsNone;
                }

                script.TargetScale = any ? values : new[] { 1f };
                script.PlatedBias = anyLever ? plated : new[] { 1f };
                script.SwarmBias = anyLever ? swarm : new[] { 1f };
                script.Tempo = anyLever ? tempo : new[] { 1f };
                script.RaidWave = anyLever ? raid : new[] { 0 };
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

        public static Dictionary<(string Key, int Difficulty), ScriptLever> ReadLevers()
        {
            var levers = new Dictionary<(string, int), ScriptLever>();
            foreach (string[] parts in Lines())
            {
                if (parts.Length != 4 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int difficulty)
                    || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    continue;
                }

                ScriptLever lever = levers.TryGetValue((parts[0], difficulty), out ScriptLever found) ? found : ScriptLever.None;
                switch (parts[2])
                {
                    case PlatedKey:
                        lever.Plated = value;
                        break;
                    case SwarmKey:
                        lever.Swarm = value;
                        break;
                    case TempoKey:
                        lever.Tempo = value;
                        break;
                    case RaidKey:
                        lever.Raid = (int)value;
                        break;
                }

                levers[(parts[0], difficulty)] = lever;
            }

            return levers;
        }

        public static void Write(Dictionary<(string Key, int Difficulty), float> scales) => Write(scales, ReadLevers());

        public static void Write(Dictionary<(string Key, int Difficulty), float> scales, Dictionary<(string Key, int Difficulty), ScriptLever> levers)
        {
            var lines = new List<string>();
            foreach (KeyValuePair<(string Key, int Difficulty), float> pair in scales)
            {
                if (pair.Value != 1f)
                {
                    lines.Add(pair.Key.Key + " " + pair.Key.Difficulty.ToString(CultureInfo.InvariantCulture) + " " + pair.Value.ToString("0.###", CultureInfo.InvariantCulture));
                }
            }

            foreach (KeyValuePair<(string Key, int Difficulty), ScriptLever> pair in levers)
            {
                string cell = pair.Key.Key + " " + pair.Key.Difficulty.ToString(CultureInfo.InvariantCulture) + " ";
                if (pair.Value.Plated != 1f)
                {
                    lines.Add(cell + PlatedKey + " " + pair.Value.Plated.ToString("0.###", CultureInfo.InvariantCulture));
                }

                if (pair.Value.Swarm != 1f)
                {
                    lines.Add(cell + SwarmKey + " " + pair.Value.Swarm.ToString("0.###", CultureInfo.InvariantCulture));
                }

                if (pair.Value.Tempo != 1f)
                {
                    lines.Add(cell + TempoKey + " " + pair.Value.Tempo.ToString("0.###", CultureInfo.InvariantCulture));
                }

                if (pair.Value.Raid != 0)
                {
                    lines.Add(cell + RaidKey + " " + pair.Value.Raid.ToString(CultureInfo.InvariantCulture));
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
