using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Balance;

namespace Game.Editor
{
    /// <summary>
    /// A level's scalar from the closed loop (round 87: one per level; rounds 83 to 86 kept one per level and difficulty,
    /// which was the level's effect plus noise): one factor on the health of every ordinary creature of the level, on
    /// every difficulty alike, measured against the probe (Tools/PlayTest/closed_loop.py), never typed. Kept in
    /// Tools/PlayTest/wave_tuning.txt, one line "Key scalar" per level; a level not listed is at one. Read by
    /// <see cref="WaveScripts"/> every time the scripts are built, so the generator, the BalanceLab tool and the tests all
    /// see the same scripts. The file holds nothing else. Pure C#.
    /// </summary>
    public static class WaveTuning
    {
        public const string Path = "Tools/PlayTest/wave_tuning.txt";

        /// <summary>Applies the file's scalars to the scripts (a script not listed keeps one).</summary>
        public static void Apply(IEnumerable<WaveScriptSpec> scripts)
        {
            Dictionary<string, float> scalars = Read();
            foreach (WaveScriptSpec script in scripts)
            {
                script.TargetScale = new[] { scalars.TryGetValue(script.Key, out float scalar) ? scalar : 1f };
                script.GraceWaves = GrowthWindow.GraceWaves;
                script.FallWithinWaves = GrowthWindow.FallWithinWaves;
            }
        }

        public static Dictionary<string, float> Read()
        {
            var scalars = new Dictionary<string, float>();
            foreach (string[] parts in Lines())
            {
                if (IsScalar(parts, out float scalar))
                {
                    scalars[parts[0]] = scalar;
                }
            }

            return scalars;
        }

        /// <summary>The lines of the file that are not a level's scalar: none are allowed (a test holds the file to its format).</summary>
        public static List<string> OtherLines()
        {
            var others = new List<string>();
            foreach (string[] parts in Lines())
            {
                if (!IsScalar(parts, out _) && string.Join(" ", parts).Trim().Length > 0)
                {
                    others.Add(string.Join(" ", parts));
                }
            }

            return others;
        }

        private static bool IsScalar(string[] parts, out float scalar)
        {
            scalar = 1f;
            return parts.Length == 2 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out scalar);
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
