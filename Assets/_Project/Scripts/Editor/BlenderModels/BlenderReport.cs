using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Game.Editor
{
    /// <summary>
    /// Reads the few numbers the import needs from an export's <c>&lt;Asset&gt;_report.json</c>: the detail families with
    /// their tiling and mask channel, and named numbers such as the distance a gait loop covers. The reports use free
    /// keys per family, which JsonUtility cannot map, so the values are matched by pattern.
    /// </summary>
    public sealed class BlenderReport
    {
        private static readonly Regex DetailPattern = new Regex("\"(\\w+)\"\\s*:\\s*\\{\\s*\"map\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"tiling\"\\s*:\\s*([0-9.]+)\\s*,\\s*\"mask\"\\s*:\\s*\"([AB])\"");

        private readonly string _json;

        private BlenderReport(string json) => _json = json ?? string.Empty;

        /// <summary>One tiling detail normal of the model: the shared map, tiles per UV unit, and the mask channel.</summary>
        public readonly struct Detail
        {
            public Detail(string family, string map, float tiling, char channel)
            {
                Family = family;
                Map = map;
                Tiling = tiling;
                Channel = channel;
            }

            public string Family { get; }
            public string Map { get; }
            public float Tiling { get; }
            public char Channel { get; }
        }

        public static BlenderReport Load(string path) => new BlenderReport(File.Exists(path) ? File.ReadAllText(path) : string.Empty);

        public List<Detail> Details()
        {
            var details = new List<Detail>(2);
            foreach (Match match in DetailPattern.Matches(_json))
            {
                details.Add(new Detail(match.Groups[1].Value, match.Groups[2].Value, float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture), match.Groups[4].Value[0]));
            }

            return details;
        }

        /// <summary>The strings of the array under a key (the gate's loose panel bones), or none.</summary>
        public List<string> Strings(string key)
        {
            var values = new List<string>();
            Match array = Regex.Match(_json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!array.Success)
            {
                return values;
            }

            foreach (Match value in Regex.Matches(array.Groups[1].Value, "\"([^\"]*)\""))
            {
                values.Add(value.Groups[1].Value);
            }

            return values;
        }

        /// <summary>The numbers of the array under a key (a muzzle position), or none.</summary>
        public List<float> Numbers(string key)
        {
            var values = new List<float>();
            Match array = Regex.Match(_json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!array.Success)
            {
                return values;
            }

            foreach (Match value in Regex.Matches(array.Groups[1].Value, "-?[0-9.]+"))
            {
                values.Add(float.Parse(value.Value, CultureInfo.InvariantCulture));
            }

            return values;
        }

        /// <summary>The first number under any of the keys, or zero.</summary>
        public float Number(params string[] keys)
        {
            foreach (string key in keys)
            {
                Match match = Regex.Match(_json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?[0-9.]+)");
                if (match.Success)
                {
                    return float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                }
            }

            return 0f;
        }
    }
}
