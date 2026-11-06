using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>One wave of a plan on paper: the purse before it, what the plan bought, and the pressure its towers meet.</summary>
    public sealed class PlanWaveLine
    {
        public int Wave;
        public bool Boss;
        public int Money;
        public int Spent;
        public int Towers;
        public float GroundPressure;
        public float AirPressure;
        public int KillMoney;

        public float Pressure => Math.Max(GroundPressure, AirPressure);

        /// <summary>One over the pressure: above one the towers alone hold the wave.</summary>
        public float Cover => Pressure > 0f ? (float.IsInfinity(Pressure) ? 0f : 1f / Pressure) : PlanEvaluator.NoThreatCover;

        public override string ToString() => string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "w{0,-2}{1} money {2,5} spent {3,4} towers {4,2} pressure {5:0.00}/{6:0.00}", Wave, Boss ? "B" : " ", Money, Spent, Towers, GroundPressure, AirPressure);
    }

    /// <summary>The paper verdict on a plan.</summary>
    public sealed class PlanOutcome
    {
        public PlanSpec Plan;
        public readonly List<PlanWaveLine> Lines = new List<PlanWaveLine>();

        /// <summary>The worst wave's cap over its pressure: above one every wave sits inside the hero cap.</summary>
        public float Margin;
        public float MeanCover;
        public int CoinsLeft;
        public int StepsTaken;

        /// <summary>Steps the purse never paid for (the plan asks for more than the level pays).</summary>
        public int StepsBlocked;

        /// <summary>First wave (1-based) above the cap, or 0 when the plan holds throughout.</summary>
        public int FirstBreak;

        /// <summary>Every wave inside the cap; steps the purse never reached are wishes, not a fall (<see cref="StepsBlocked"/> says how many).</summary>
        public bool Holds => FirstBreak == 0;

        /// <summary>Worst-wave margin first, then the mean cover, then the coins left: the search ranks plans by it.</summary>
        public float Score => Margin + 0.05f * Math.Min(MeanCover, 4f) + 0.0001f * CoinsLeft;
    }

    /// <summary>
    /// Judges a <see cref="PlanSpec"/> on paper, wave by wave (round 22): the purse walks like the ledger's (start, kills paid
    /// on arrival at the defence, the mines once the harvester is bought), each due step is taken as soon as the purse has
    /// it (an unaffordable step blocks the ones after it, as it would a player following the plan), and the standing towers
    /// meet the wave from their own spots (<see cref="LevelGeometry.CoverAt"/>): the time a gun gets is the route in its reach,
    /// and the gate's hold only when it reaches the queue. A mine takes its blast off the wave's stream once, a slow field
    /// multiplies the guns that cover its cell for one wave, an attack drone fires everywhere, a sale refunds its share.
    /// The same capacities, with the realised standing set of a probe run, are the analytic side of the calibration.
    /// </summary>
    public static class PlanEvaluator
    {
        public const float NoThreatCover = 1e6f;

        /// <summary>Seconds a slow field lasts against the wave it is laid in, as a share of the wave's window it holds.</summary>
        private const float FieldWaveShare = 1f;

        private sealed class Standing
        {
            public int Tower;
            public int Level;
            public Cell Cell;
            public int Spent;
        }

        public static PlanOutcome Evaluate(LevelInput level, IReadOnlyList<WaveSpec> waves, DifficultyStats difficulty, BalanceRules rules, PlanSpec plan, float cap)
        {
            var outcome = new PlanOutcome { Plan = plan };
            var purse = new KillPurse(level, difficulty, rules);
            var standing = new List<Standing>();
            int spent = 0;
            int next = 0;
            int harvestWave = 0;
            float attackDps = 0f;
            int attackTargets = 0;
            float coverSum = 0f;
            int covered = 0;
            float margin = float.MaxValue;

            for (int i = 0; i < waves.Count; i++)
            {
                int wave = i + 1;
                purse.Begin(waves[i]);
                WaveLoad load = Ledger.LoadOf(level, waves[i], difficulty, rules, purse.Carried(waves[i]));
                int money = level.StartingMoney + (int)purse.Paid + MineIncome(level, i, waves.Count, rules, harvestWave) - spent;
                var line = new PlanWaveLine { Wave = wave, Boss = load.HasBoss, Money = money, KillMoney = load.KillMoney };
                var mines = new List<GadgetStats>();
                var fields = new List<(GadgetStats Gadget, Cell Cell)>();

                // Due steps in order, while the purse has them.
                while (next < plan.Steps.Length && plan.Steps[next].Wave <= wave)
                {
                    PlanStep step = plan.Steps[next];
                    int price = Price(level, standing, step, out int towerIndex);
                    if (price < 0)
                    {
                        // A step that cannot be taken at all (no such tower, nothing to upgrade) is skipped, not waited for.
                        next++;
                        continue;
                    }

                    if (price > money)
                    {
                        break;
                    }

                    money -= price;
                    spent += price;
                    line.Spent += price;
                    outcome.StepsTaken++;
                    next++;
                    switch (step.Kind)
                    {
                        case PlanStep.Build:
                            standing.Add(new Standing { Tower = towerIndex, Level = 0, Cell = step.Cell, Spent = price });
                            break;
                        case PlanStep.Upgrade:
                            Standing up = At(standing, step.Cell);
                            up.Level++;
                            up.Spent += price;
                            break;
                        case PlanStep.Sell:
                            standing.Remove(At(standing, step.Cell));
                            break;
                        case PlanStep.Mine:
                            mines.Add(Gadget(level, step.Item));
                            break;
                        case PlanStep.Field:
                            fields.Add((Gadget(level, step.Item), step.Cell));
                            break;
                        case PlanStep.Drone:
                            DroneStats drone = Drone(level, step.Item);
                            harvestWave = drone.Role == DroneRole.Harvest && harvestWave == 0 ? wave : harvestWave;
                            attackDps += drone.Role == DroneRole.Attack ? drone.Dps : 0f;
                            attackTargets |= drone.Role == DroneRole.Attack ? drone.Targets : 0;
                            break;
                    }
                }

                // Mines take their blast off the stream before the guns meet it.
                foreach (GadgetStats mine in mines)
                {
                    load.StreamHealth = Math.Max(0f, load.StreamHealth - mine.Damage * Ledger.Splash(mine.Radius, load.GroundDensity, rules.MaxSplashTargets));
                }

                float stream = Ledger.HeroStream(level, load, rules);
                float single = Ledger.HeroSingle(level, load, rules);
                float air = 0f;
                foreach (Standing tower in standing)
                {
                    TowerStats stats = level.Towers[tower.Tower];
                    float range = stats.Levels[tower.Level].Range * level.BoostOf(stats.Id).Range;
                    Ledger.Option option = Ledger.Capacity(level, load, rules, tower.Tower, tower.Level, CoverOf(level, tower.Cell, range));
                    float slow = FieldGain(level, fields, tower.Cell, range, load);
                    stream += option.Stream * slow;
                    single += option.Single * slow;
                    air += option.Air;
                }

                if (attackDps > 0f)
                {
                    float drone = attackDps * rules.Utilisation;
                    stream += Layer.Has(attackTargets, Layer.Ground) ? drone * load.Window : 0f;
                    single += Layer.Has(attackTargets, Layer.Ground) ? drone * load.BossSeconds(load.BossWindow + load.BossFight, rules) : 0f;
                    air += Layer.Has(attackTargets, Layer.Air) ? drone * load.AirWindow : 0f;
                }

                line.Towers = standing.Count;
                line.GroundPressure = load.GroundPressure(stream, single);
                line.AirPressure = load.AirPressure(air);
                outcome.Lines.Add(line);
                float pressure = line.Pressure;
                if (pressure > 0f)
                {
                    float waveMargin = float.IsInfinity(pressure) ? 0f : rules.CapFor(cap, load.HasBoss) / pressure;
                    margin = Math.Min(margin, waveMargin);
                    coverSum += Math.Min(line.Cover, 4f);
                    covered++;
                    if (waveMargin < 1f && outcome.FirstBreak == 0)
                    {
                        outcome.FirstBreak = wave;
                    }
                }

                purse.Add(waves[i], new WaveLine { GroundPressure = line.GroundPressure, AirPressure = line.AirPressure });
                outcome.CoinsLeft = money;
            }

            outcome.StepsBlocked = plan.Steps.Length - next;
            outcome.Margin = margin == float.MaxValue ? 0f : margin;
            outcome.MeanCover = covered > 0 ? coverSum / covered : 0f;
            return outcome;
        }

        /// <summary>
        /// The paper capacity of a standing set against one wave, per tower type, at the given efficiency (-1 = calibrated):
        /// the analytic side of the calibration fit, the realised damage of the same towers being the other.
        /// </summary>
        public static Dictionary<string, float> CapacityByType(LevelInput level, WaveLoad load, BalanceRules rules, IEnumerable<(string Id, int Level, Cell Cell)> standing, float efficiency)
        {
            var sum = new Dictionary<string, float>();
            foreach ((string id, int levelIndex, Cell cell) in standing)
            {
                int index = level.Towers.FindIndex(t => t.Id == id);
                if (index < 0 || levelIndex < 0 || levelIndex >= level.Towers[index].Levels.Length)
                {
                    continue;
                }

                TowerStats stats = level.Towers[index];
                float range = stats.Levels[levelIndex].Range * level.BoostOf(id).Range;
                Ledger.Option option = Ledger.Capacity(level, load, rules, index, levelIndex, CoverOf(level, cell, range), efficiency);
                sum[id] = (sum.TryGetValue(id, out float known) ? known : 0f) + option.Stream + option.Single + option.Air;
            }

            return sum;
        }

        /// <summary>What a spot covers for a reach; without a map every spot is ideal.</summary>
        public static SpotCover CoverOf(LevelInput level, Cell cell, float range) =>
            level.Geometry != null ? level.Geometry.CoverAt(cell, range) : SpotCover.Ideal(range);

        // Ore the harvester has brought in by a wave: nothing before it is bought, then the mines empty over the level as the ledger reckons.
        private static int MineIncome(LevelInput level, int waveIndex, int waveCount, BalanceRules rules, int harvestWave)
        {
            if (harvestWave <= 0 || level.MineCount == 0)
            {
                return 0;
            }

            int open = Math.Max(rules.MinesOpenFromWave, harvestWave);
            if (waveIndex < open)
            {
                return 0;
            }

            float progress = Math.Min(1f, (waveIndex - open) / Math.Max(1f, waveCount * rules.MineDepletionShare));
            return Math.Max(0, (int)Math.Round(level.MineCount * level.MineStock * progress * level.Income));
        }

        // A slow field on a cell the gun reaches holds every creature there longer: the gun's damage per creature grows by the
        // extra seconds, over the share of its route the field covers (Docs/BALANCE.md, "Slow as damage").
        private static float FieldGain(LevelInput level, List<(GadgetStats Gadget, Cell Cell)> fields, Cell spot, float range, WaveLoad load)
        {
            float gain = 1f;
            foreach ((GadgetStats gadget, Cell cell) in fields)
            {
                if (gadget.SlowFactor <= 0f || gadget.SlowFactor >= 1f || level.Geometry == null || !level.Geometry.Reaches(spot, range, cell))
                {
                    continue;
                }

                float pass = Math.Max(1f, CoverOf(level, spot, range).GroundPass);
                float share = Math.Min(1f, 2f * gadget.Radius / level.Geometry.CellSize / pass);
                gain += (1f / gadget.SlowFactor - 1f) * share * FieldWaveShare;
            }

            return gain;
        }

        // The price of a step now, -1 when it cannot be taken at all.
        private static int Price(LevelInput level, List<Standing> standing, PlanStep step, out int towerIndex)
        {
            towerIndex = -1;
            switch (step.Kind)
            {
                case PlanStep.Build:
                    towerIndex = level.Towers.FindIndex(t => t.Id == step.Item);
                    return towerIndex >= 0 && level.Towers[towerIndex].Levels.Length > 0 && At(standing, step.Cell) == null ? level.Towers[towerIndex].Levels[0].Cost : -1;
                case PlanStep.Upgrade:
                    Standing up = At(standing, step.Cell);
                    return up != null && up.Level + 1 < level.Towers[up.Tower].Levels.Length ? level.Towers[up.Tower].Levels[up.Level + 1].Cost : -1;
                case PlanStep.Sell:
                    Standing sold = At(standing, step.Cell);
                    return sold != null ? -(int)Math.Round(sold.Spent * level.Towers[sold.Tower].SellRefund) : -1;
                case PlanStep.Mine:
                case PlanStep.Field:
                    GadgetStats gadget = Gadget(level, step.Item);
                    return gadget != null ? gadget.Cost : -1;
                case PlanStep.Drone:
                    DroneStats drone = Drone(level, step.Item);
                    return drone != null ? drone.Cost : -1;
                default:
                    return -1;
            }
        }

        private static Standing At(List<Standing> standing, Cell cell)
        {
            foreach (Standing tower in standing)
            {
                if (tower.Cell.X == cell.X && tower.Cell.Y == cell.Y)
                {
                    return tower;
                }
            }

            return null;
        }

        private static GadgetStats Gadget(LevelInput level, string id) => level.Gadgets.Find(g => g.Id == id);

        private static DroneStats Drone(LevelInput level, string id) => level.Drones.Find(d => d.Id == id);
    }
}
