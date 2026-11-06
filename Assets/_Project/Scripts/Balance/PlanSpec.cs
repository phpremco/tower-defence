using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Balance
{
    /// <summary>One premeditated move of a plan: what, where and from which wave on (round 22, Docs/BALANCE.md "Plans").</summary>
    [Serializable]
    public sealed class PlanStep
    {
        public const string Build = "build";
        public const string Upgrade = "upgrade";
        public const string Mine = "mine";
        public const string Field = "field";
        public const string Drone = "drone";
        public const string Sell = "sell";

        /// <summary>One of the kinds above.</summary>
        public string Kind = Build;

        /// <summary>Tower id to build, gadget id to lay, or drone id to buy; empty for an upgrade or a sale.</summary>
        public string Item = string.Empty;

        /// <summary>The cell: the build spot of a tower (also for its upgrade or sale), or the road cell of a gadget.</summary>
        public int X;
        public int Y;

        /// <summary>The step is taken from this wave (1-based) on, as soon as the purse has the price; earlier steps go first.</summary>
        public int Wave = 1;

        public Cell Cell => new Cell(X, Y);

        public override string ToString() => $"{Kind} {Item} @({X},{Y}) w{Wave}";
    }

    /// <summary>
    /// A whole plan for one level and difficulty: the ordered steps a replaying player would take. Replayable without any
    /// decision at run time (PlanReplayTests), judged on paper by <see cref="PlanEvaluator"/>. Written as JSON under
    /// Tools/PlayTest/Plans so Remco can read and try a plan himself.
    /// </summary>
    [Serializable]
    public sealed class PlanSpec
    {
        public string Level = string.Empty;
        public int Difficulty;
        public string Name = string.Empty;

        /// <summary>"searched" for a plan the search found, else the obvious family it belongs to (mono, naive, affordable).</summary>
        public string Family = "searched";
        public PlanStep[] Steps = new PlanStep[0];

        /// <summary>Which of the game's features the plan leans on, for the acceptance rules (upgrades, gadgets, drones, sells).</summary>
        public int Upgrades
        {
            get
            {
                int n = 0;
                foreach (PlanStep step in Steps)
                {
                    n += step.Kind == PlanStep.Upgrade ? 1 : 0;
                }

                return n;
            }
        }

        public bool Uses(string kind)
        {
            foreach (PlanStep step in Steps)
            {
                if (step.Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The distinct spots the plan builds on, as keys.</summary>
        public HashSet<long> SpotKeys()
        {
            var keys = new HashSet<long>();
            foreach (PlanStep step in Steps)
            {
                if (step.Kind == PlanStep.Build)
                {
                    keys.Add(((long)step.X << 32) ^ (uint)step.Y);
                }
            }

            return keys;
        }

        /// <summary>Towers built per type.</summary>
        public Dictionary<string, int> Mix()
        {
            var mix = new Dictionary<string, int>();
            foreach (PlanStep step in Steps)
            {
                if (step.Kind == PlanStep.Build)
                {
                    mix[step.Item] = (mix.TryGetValue(step.Item, out int n) ? n : 0) + 1;
                }
            }

            return mix;
        }

        /// <summary>
        /// Two plans are distinct when they differ in at least two of: the spots built on (less than 60% shared), the tower mix
        /// (a type's count differs by two or more, or a type only one has), the gadget or drone timing (a step kind one has and
        /// the other not, or its first wave differs by three or more).
        /// </summary>
        public bool DistinctFrom(PlanSpec other)
        {
            int differences = 0;
            HashSet<long> mine = SpotKeys();
            HashSet<long> theirs = other.SpotKeys();
            int shared = 0;
            foreach (long key in mine)
            {
                shared += theirs.Contains(key) ? 1 : 0;
            }

            int most = Math.Max(1, Math.Max(mine.Count, theirs.Count));
            differences += shared / (float)most < 0.6f ? 1 : 0;

            Dictionary<string, int> a = Mix();
            Dictionary<string, int> b = other.Mix();
            bool mixDiffers = false;
            foreach (KeyValuePair<string, int> pair in a)
            {
                mixDiffers |= !b.TryGetValue(pair.Key, out int n) || Math.Abs(n - pair.Value) >= 2;
            }

            foreach (string key in b.Keys)
            {
                mixDiffers |= !a.ContainsKey(key);
            }

            differences += mixDiffers ? 1 : 0;
            differences += TimingDiffers(other, PlanStep.Mine) || TimingDiffers(other, PlanStep.Field) || TimingDiffers(other, PlanStep.Drone) ? 1 : 0;
            return differences >= 2;
        }

        private bool TimingDiffers(PlanSpec other, string kind)
        {
            int mine = FirstWave(kind);
            int theirs = other.FirstWave(kind);
            return (mine < 0) != (theirs < 0) || (mine >= 0 && Math.Abs(mine - theirs) >= 3);
        }

        private int FirstWave(string kind)
        {
            foreach (PlanStep step in Steps)
            {
                if (step.Kind == kind)
                {
                    return step.Wave;
                }
            }

            return -1;
        }

        /// <summary>The plan as a player would read it: wave by wave, which tower where and what to upgrade.</summary>
        public string Describe(Func<string, string> displayName = null)
        {
            string Name(string id) => displayName != null ? displayName(id) : id;
            var text = new StringBuilder();
            var byWave = new SortedDictionary<int, List<string>>();
            foreach (PlanStep step in Steps)
            {
                string line;
                switch (step.Kind)
                {
                    case PlanStep.Build:
                        line = $"build {Name(step.Item)} at ({step.X},{step.Y})";
                        break;
                    case PlanStep.Upgrade:
                        line = $"upgrade the tower at ({step.X},{step.Y})";
                        break;
                    case PlanStep.Sell:
                        line = $"sell the tower at ({step.X},{step.Y})";
                        break;
                    case PlanStep.Drone:
                        line = $"buy the {Name(step.Item)}";
                        break;
                    default:
                        line = $"lay a {Name(step.Item)} on the road at ({step.X},{step.Y})";
                        break;
                }

                if (!byWave.TryGetValue(step.Wave, out List<string> lines))
                {
                    byWave[step.Wave] = lines = new List<string>();
                }

                lines.Add(line);
            }

            foreach (KeyValuePair<int, List<string>> wave in byWave)
            {
                text.Append(string.Format(CultureInfo.InvariantCulture, "  from wave {0,2}: {1}\n", wave.Key, string.Join("; ", wave.Value)));
            }

            return text.ToString();
        }
    }
}
