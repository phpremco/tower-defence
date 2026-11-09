using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Balance
{
    /// <summary>The ordered spots a strategy prefers for one tower type (best first); a spot not listed is taken by the bot's own ranking after them.</summary>
    [Serializable]
    public sealed class SpotPriority
    {
        public string Tower = string.Empty;
        public int[] X = new int[0];
        public int[] Y = new int[0];

        public int Count => Math.Min(X.Length, Y.Length);

        public Cell At(int index) => new Cell(X[index], Y[index]);
    }

    /// <summary>How much a strategy favours a tower type when a threat is at hand (1 = the ledger's own value, above 1 favoured).</summary>
    [Serializable]
    public sealed class ThreatWeight
    {
        public string Threat = string.Empty;
        public string Tower = string.Empty;
        public float Weight = 1f;
    }

    /// <summary>
    /// A parametrised strategy an adaptive bot executes (round 22e, the curator's decision 2): instead of a fixed timeline it says
    /// which spots each tower type takes first, which tower types answer which threat, when an upgrade beats a new tower, when
    /// gadgets and the slow field go down, when the drones come in, and whether a sale is allowed (Psycho only). The creative
    /// reference bot is one point of this space (<see cref="Creative"/>); stage A searches the space and pre-screens it on paper
    /// (<see cref="StrategyExecutor"/>), stage B plays the best few in the game, and the build order a strategy really used is
    /// recorded per run (the moves) for the README. Everything a player could follow in words: <see cref="Describe"/>.
    /// </summary>
    [Serializable]
    public sealed class StrategySpec
    {
        public const string Swarm = "swarm";
        public const string Pack = "pack";
        public const string Plated = "plated";
        public const string Air = "air";
        public const string Boss = "boss";
        public static readonly string[] Threats = { Swarm, Pack, Plated, Air, Boss };

        public string Level = string.Empty;
        public int Difficulty;
        public string Name = string.Empty;

        /// <summary>"searched" for a strategy the search found, "creative" for the reference bot.</summary>
        public string Family = "searched";

        public SpotPriority[] Spots = new SpotPriority[0];
        public ThreatWeight[] Weights = new ThreatWeight[0];

        /// <summary>Multiplies an upgrade's value per coin against a new tower's: above 1 the strategy upgrades sooner.</summary>
        public float UpgradeBias = 1f;

        /// <summary>Guns standing before the first upgrade (a single upgraded gun covers little road).</summary>
        public int TowersBeforeUpgrades = 2;

        /// <summary>Mines before a crowd wave when the ground cover (one over the pressure) is below this; 0 never.</summary>
        public float MineCoverBelow = 1f;
        public bool MineOnBoss = true;

        /// <summary>A slow field when the ground cover is below this (or when the field pays by the damage rule); 0 never.</summary>
        public float FieldCoverBelow = 1f;

        /// <summary>The harvester once the ground cover on the waves at hand reaches this (0 never), within the first share of the level.</summary>
        public float HarvesterCover = 1.2f;
        public float HarvesterShare = 0.4f;

        /// <summary>The repair drone once a gun or gate has lost this share of its health (above 1 never).</summary>
        public float RepairDroneWear = 0.1f;

        /// <summary>The attack drone once this many guns stand (0 never).</summary>
        public int AttackDroneAfterTowers = 8;

        /// <summary>
        /// Multiplies a gate upgrade's value per coin against the other steps (round 26, Remco: gates are a feature the plans should
        /// use): its gun's gain plus the health bonus at the repair price; 0 never buys one. The bot in play uses the same knob.
        /// </summary>
        public float GateUpgradeBias;

        /// <summary>Selling a gun for a sky gun the preview calls for: a last resort, Psycho only for searched strategies (Remco).</summary>
        public bool AllowSell;

        /// <summary>The creative reference bot of rounds 20-22 as a strategy: no spot lists (its own role placement), every weight one.</summary>
        public static StrategySpec Creative(string level, int difficulty) => new StrategySpec
        {
            Level = level,
            Difficulty = difficulty,
            Name = "creative",
            Family = "creative",
            AllowSell = true,
        };

        public float WeightOf(string threat, string tower)
        {
            foreach (ThreatWeight weight in Weights)
            {
                if (weight.Threat == threat && weight.Tower == tower)
                {
                    return weight.Weight;
                }
            }

            return 1f;
        }

        public SpotPriority SpotsOf(string tower)
        {
            foreach (SpotPriority priority in Spots)
            {
                if (priority.Tower == tower)
                {
                    return priority;
                }
            }

            return null;
        }

        /// <summary>The tower type the strategy favours most against a threat (null when it has no preference), among the given types.</summary>
        public string FavouriteFor(string threat, IEnumerable<string> towers)
        {
            string best = null;
            float bestWeight = 1f;
            foreach (string tower in towers)
            {
                float weight = WeightOf(threat, tower);
                if (weight > bestWeight)
                {
                    best = tower;
                    bestWeight = weight;
                }
            }

            return best;
        }

        /// <summary>Early, late or never: the category a timing parameter falls in, for distinctness and the README.</summary>
        public static string Timing(float value, float never, float earlyBelow) => value >= never ? "never" : value <= earlyBelow ? "early" : "late";

        /// <summary>
        /// Two strategies are distinct when they differ in at least two of: the spots (fewer than 60% of the first three spots of a type
        /// shared), the mix (a different favourite type for some threat), the gadget or drone timing (mines on bosses, the harvester, the
        /// repair drone or the field in another category).
        /// </summary>
        public bool DistinctFrom(StrategySpec other, IEnumerable<string> towers)
        {
            int differences = 0;
            bool spotsDiffer = false;
            foreach (SpotPriority mine in Spots)
            {
                SpotPriority theirs = other.SpotsOf(mine.Tower);
                int shared = 0;
                int count = Math.Min(3, mine.Count);
                for (int i = 0; i < count; i++)
                {
                    Cell cell = mine.At(i);
                    for (int j = 0; theirs != null && j < Math.Min(3, theirs.Count); j++)
                    {
                        shared += theirs.At(j).X == cell.X && theirs.At(j).Y == cell.Y ? 1 : 0;
                    }
                }

                spotsDiffer |= count > 0 && shared / (float)count < 0.6f;
            }

            differences += spotsDiffer ? 1 : 0;
            bool mixDiffers = false;
            foreach (string threat in Threats)
            {
                mixDiffers |= FavouriteFor(threat, towers) != other.FavouriteFor(threat, towers);
            }

            differences += mixDiffers ? 1 : 0;
            bool timingDiffers = MineOnBoss != other.MineOnBoss
                || (MineCoverBelow <= 0f) != (other.MineCoverBelow <= 0f)
                || Timing(HarvesterCover <= 0f ? 9f : HarvesterShare, 9f, 0.4f) != Timing(other.HarvesterCover <= 0f ? 9f : other.HarvesterShare, 9f, 0.4f)
                || Timing(RepairDroneWear, 1f, 0.15f) != Timing(other.RepairDroneWear, 1f, 0.15f)
                || (FieldCoverBelow <= 0f) != (other.FieldCoverBelow <= 0f)
                || (AttackDroneAfterTowers <= 0) != (other.AttackDroneAfterTowers <= 0)
                || (GateUpgradeBias <= 0f) != (other.GateUpgradeBias <= 0f);
            differences += timingDiffers ? 1 : 0;
            return differences >= 2;
        }

        /// <summary>The strategy in words a player can follow.</summary>
        public string Describe(IEnumerable<string> groundTowers, Func<string, string> displayName = null)
        {
            string Name(string id) => displayName != null ? displayName(id) : id;
            var text = new StringBuilder();
            var towers = new List<string>(groundTowers);
            var answers = new List<string>();
            foreach (string threat in Threats)
            {
                string favourite = FavouriteFor(threat, towers);
                answers.Add(threat + ": " + (favourite != null ? Name(favourite) : "whatever the ledger rates best"));
            }

            text.Append("  Towers per threat: ").Append(string.Join("; ", answers)).Append(".\n");
            foreach (SpotPriority spots in Spots)
            {
                var cells = new List<string>();
                for (int i = 0; i < spots.Count && i < 3; i++)
                {
                    cells.Add(string.Format(CultureInfo.InvariantCulture, "({0},{1})", spots.X[i], spots.Y[i]));
                }

                text.Append("  ").Append(Name(spots.Tower)).Append(" spots, best first: ").Append(string.Join(" ", cells)).Append(".\n");
            }

            text.Append(string.Format(CultureInfo.InvariantCulture, "  Upgrades: {0} guns down first, then an upgrade is worth {1:0.00}x its value per coin against a new gun.\n", TowersBeforeUpgrades, UpgradeBias));
            text.Append("  Mines: ").Append(MineOnBoss ? "before every boss wave" : "not for bosses").Append(MineCoverBelow > 0f ? string.Format(CultureInfo.InvariantCulture, ", and before a crowd wave when the guns would not hold it (cover under {0:0.00})", MineCoverBelow) : ", never for crowds").Append(".\n");
            text.Append("  Slow field: ").Append(FieldCoverBelow > 0f ? string.Format(CultureInfo.InvariantCulture, "when the guns would not hold the wave (cover under {0:0.00}) or the slow pays by the damage rule", FieldCoverBelow) : "never").Append(".\n");
            text.Append("  Harvester: ").Append(HarvesterCover > 0f ? string.Format(CultureInfo.InvariantCulture, "in the first {0:P0} of the level, once the guns hold the waves at hand with cover {1:0.00}", HarvesterShare, HarvesterCover) : "never").Append(".\n");
            text.Append("  Repair drone: ").Append(RepairDroneWear < 1f ? string.Format(CultureInfo.InvariantCulture, "once a gun or gate has lost {0:P0} of its health", RepairDroneWear) : "never").Append(".\n");
            text.Append("  Attack drone: ").Append(AttackDroneAfterTowers > 0 ? string.Format(CultureInfo.InvariantCulture, "once {0} guns stand", AttackDroneAfterTowers) : "never").Append(".\n");
            text.Append("  Gate upgrades: ").Append(GateUpgradeBias > 0f ? string.Format(CultureInfo.InvariantCulture, "the defence gate's gun and health, weighed at {0:0.00}x against a tower step", GateUpgradeBias) : "never").Append(".\n");
            text.Append("  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.\n");
            text.Append("  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.\n");
            text.Append(AllowSell ? "  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).\n" : "  Selling: never.\n");
            return text.ToString();
        }
    }
}
