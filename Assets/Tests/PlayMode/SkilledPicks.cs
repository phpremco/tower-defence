using System.Collections.Generic;
using System.Globalization;

namespace Game.Tests
{
    /// <summary>
    /// The skilled rung's strategy per cell (round 78): Tools/PlayTest/skilled_picks.txt names, per level and difficulty, the
    /// searched strategy that did best when every strategy of the cell was played as the skilled rung plays one, on the data
    /// of that day (seeds 1-3 at the data; the rule and the candidates are in Tools/PlayTest/skilled_picks.py). Until then the
    /// rung took the best strategy of round 68's plan_records.txt, which went stale with every wave change (round 77: Quartz
    /// Flats Medium). The file is part of the bot stamp (<see cref="ProbeStamp"/>): refreshing the picks makes a new bot.
    /// </summary>
    internal static class SkilledPicks
    {
        public const string Path = "Tools/PlayTest/skilled_picks.txt";
        private const string Tag = "pick";
        private const int NameField = 3;
        private static Dictionary<(string Level, int Difficulty), string> _picks;

        /// <summary>The name of the cell's picked strategy ("Level_C1_05_1_strategy3"), or null when the file has none for it.</summary>
        public static string For(string levelName, int difficulty)
        {
            if (_picks == null)
            {
                _picks = Read();
            }

            return _picks.TryGetValue((levelName, difficulty), out string name) ? name : null;
        }

        // Lines "pick|level|difficulty|strategy|..."; everything else (the header, the evidence after the name) is for readers.
        private static Dictionary<(string, int), string> Read()
        {
            var picks = new Dictionary<(string, int), string>();
            if (!System.IO.File.Exists(Path))
            {
                return picks;
            }

            foreach (string line in System.IO.File.ReadAllLines(Path))
            {
                string[] fields = line.Split('|');
                if (fields.Length > NameField && fields[0] == Tag && int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int difficulty))
                {
                    picks[(fields[1], difficulty)] = fields[NameField];
                }
            }

            return picks;
        }
    }
}
