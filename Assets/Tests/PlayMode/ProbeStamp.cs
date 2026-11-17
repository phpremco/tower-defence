using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Game.Tests
{
    /// <summary>
    /// What a probe record was measured with (round 76, Docs/BALANCE_CRITIC_2026-11-17b.md): the version of the reference
    /// players' rules with a hash of everything that decides how they play, a hash of the game's data and one of the game's
    /// code. The probe is deterministic per code, data and seed, so two records are only comparable as "the data changed"
    /// when their bot stamps are equal, and as "the bot changed" when their data stamps are. The report tools refuse to
    /// compare records across bot stamps unless asked. Rule: a batch changes the bot or the data, never both.
    /// </summary>
    internal static class ProbeStamp
    {
        /// <summary>
        /// The version of the reference players (the sensible bot, the skilled rung, the obvious family). Bump it with every
        /// change of their rules; the hash beside it catches a change that forgot to. Version 2 (round 76): the sensible bot
        /// knows the level's Codex roster and no wave preview, opens for the roster, sells in the emergency only; the skilled
        /// rung is the cell's best searched strategy played adaptively with the rift reaction. Version 3 (round 78): the
        /// sensible bot also reads the HUD's next-wave preview (the same <see cref="Game.Levels.WavePreview"/>, nothing more);
        /// both rungs put or move a sky gun beside the base when flyers bite it out of reach of every sky gun; the skilled
        /// rung's strategy is the pick of Tools/PlayTest/skilled_picks.txt, measured on the data of its day. Frozen.
        /// </summary>
        public const int BotVersion = 3;

        private const int HashLength = 8;
        private const byte CarriageReturn = 13;

        // Everything that decides how the reference players play: their code, the picks and recorded plans the skilled rung
        // is chosen from and the searched strategies it plays.
        private static readonly string[] BotFiles =
        {
            "Assets/Tests/PlayMode/BalanceProbeTests.cs", "Assets/Tests/PlayMode/BotRun.cs", "Assets/Tests/PlayMode/ProbeStrategy.cs",
            "Assets/Tests/PlayMode/ProbePlayer.cs", "Assets/Tests/PlayMode/PressureProbeTests.cs", "Assets/Tests/PlayMode/HeadroomProbeTests.cs",
            "Assets/Tests/PlayMode/SkilledPicks.cs",
            "Tools/PlayTest/plan_records.txt", SkilledPicks.Path,
        };

        private const string PlansFolder = "Tools/PlayTest/Plans";
        private const string DataFolder = "Assets/_Project/Data";
        private const string CodeFolder = "Assets/_Project/Scripts";
        private static string _line;

        /// <summary>The stamp of this project state, "bot=2:hash;data=hash;game=hash"; computed once per test session.</summary>
        public static string Line => _line ?? (_line = Compute());

        private static string Compute()
        {
            var bot = new List<string>(BotFiles);
            bot.AddRange(Files(PlansFolder, "*.json"));
            return "bot=" + BotVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + Hash(bot)
                + ";data=" + Hash(Files(DataFolder, "*.asset")) + ";game=" + Hash(Files(CodeFolder, "*.cs"));
        }

        private static List<string> Files(string folder, string pattern)
        {
            var files = new List<string>();
            if (Directory.Exists(folder))
            {
                foreach (string file in Directory.GetFiles(folder, pattern, SearchOption.AllDirectories))
                {
                    files.Add(file.Replace(Path.DirectorySeparatorChar, '/'));
                }
            }

            files.Sort(System.StringComparer.Ordinal);
            return files;
        }

        // The files' names and bytes in order, carriage returns dropped so a checkout's line endings do not move the stamp.
        private static string Hash(List<string> files)
        {
            using (MD5 md5 = MD5.Create())
            {
                foreach (string file in files)
                {
                    byte[] name = Encoding.UTF8.GetBytes(file);
                    md5.TransformBlock(name, 0, name.Length, null, 0);
                    if (!File.Exists(file))
                    {
                        continue;
                    }

                    byte[] bytes = System.Array.FindAll(File.ReadAllBytes(file), b => b != CarriageReturn);
                    md5.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }

                md5.TransformFinalBlock(System.Array.Empty<byte>(), 0, 0);
                var text = new StringBuilder();
                foreach (byte b in md5.Hash)
                {
                    text.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                }

                return text.ToString(0, HashLength);
            }
        }
    }
}
