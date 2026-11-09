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

        /// <summary>Health that reached the gate's queue or the base alive, what the siege took off the gates and the base, and the base left after the wave.</summary>
        public float Leaked;
        public float GateDamage;
        public float BaseDamage;
        public float BaseLeft;

        /// <summary>The trunk gates' health together after the wave, and what a repair drone restored during it.</summary>
        public float GateLeft;
        public float DroneRepaired;

        /// <summary>True when the hero went down in this wave's siege.</summary>
        public bool HeroFell;

        public float Pressure => Math.Max(GroundPressure, AirPressure);

        /// <summary>One over the pressure: above one the towers alone hold the wave.</summary>
        public float Cover => Pressure > 0f ? (float.IsInfinity(Pressure) ? 0f : 1f / Pressure) : PlanEvaluator.NoThreatCover;

        public override string ToString() => string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "w{0,-2}{1} money {2,5} spent {3,4} towers {4,2} pressure {5:0.00}/{6:0.00} base {7,4:0}", Wave, Boss ? "B" : " ", Money, Spent, Towers, GroundPressure, AirPressure, BaseLeft);
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

        /// <summary>Coins the plan spent on gate repairs by hand, how often, and the gates' health after the last wave (Remco: repairs are a factor).</summary>
        public int RepairSpent;
        public int Repairs;
        public float GateLeft;

        /// <summary>First wave (1-based) above the cap (something leaks there), or 0 when every wave sits inside it.</summary>
        public int FirstBreak;

        /// <summary>The base's health when the level starts and what the leak model leaves of it; the wave it falls in (0 = it stands).</summary>
        public float BaseStart;
        public float BaseLeft;
        public int FallWave;
        public int WaveCount;

        /// <summary>Stars the base left earns (the game's thresholds); zero for a fall.</summary>
        public int Stars;

        /// <summary>The plan survives the level: the base stands after the last wave (the curator, round 22: a win with a damaged base is a win).</summary>
        public bool Holds => FallWave == 0 && BaseLeft > 0f;

        /// <summary>The same score a recorded run gets (<see cref="Leak.Score"/>), so paper and play rank alike.</summary>
        public float AnalyticScore => Leak.Score(Holds, Stars, BaseLeft, CoinsLeft, WaveCount, FallWave > 0 ? FallWave : WaveCount);

        /// <summary>The analytic score, then the mean cover and the worst-wave margin as tie-breakers: the search ranks plans by it.</summary>
        public float Score => AnalyticScore + 20f * Math.Min(MeanCover, 4f) + 10f * Math.Min(Margin, 3f);
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

        /// <summary>What a run-time policy sees at the start of a wave on paper (round 22e, <see cref="StrategyExecutor"/>).</summary>
        public sealed class PolicyView
        {
            public int WaveIndex;
            public int Money;
            public readonly List<(int Tower, int Level, Cell Cell)> Standing = new List<(int, int, Cell)>();
            public SiegeState Siege;
            public bool Harvester;
            public readonly HashSet<string> Drones = new HashSet<string>();

            /// <summary>The largest share of its health the defence gate has lost so far (guns do not wear on paper).</summary>
            public float Wear;
        }

        /// <summary>A policy adds the steps it takes this wave, in order; an unaffordable one blocks the rest, as in a plan.</summary>
        public delegate void PlanPolicy(PolicyView view, List<PlanStep> steps);

        /// <summary>Seconds a slow field lasts against the wave it is laid in, as a share of the wave's window it holds.</summary>
        private const float FieldWaveShare = 1f;

        private sealed class Standing
        {
            public int Tower;
            public int Level;
            public Cell Cell;
            public int Spent;
        }

        public static PlanOutcome Evaluate(LevelInput level, IReadOnlyList<WaveSpec> waves, DifficultyStats difficulty, BalanceRules rules, PlanSpec plan, float cap) =>
            Evaluate(level, waves, difficulty, rules, plan, cap, null);

        /// <summary>As above; a <paramref name="policy"/> adds steps at the start of every wave, and the outcome's plan is then the concrete order taken.</summary>
        public static PlanOutcome Evaluate(LevelInput level, IReadOnlyList<WaveSpec> waves, DifficultyStats difficulty, BalanceRules rules, PlanSpec plan, float cap, PlanPolicy policy)
        {
            var outcome = new PlanOutcome { Plan = plan };
            var steps = new List<PlanStep>(plan.Steps);
            var view = policy != null ? new PolicyView() : null;
            var purse = new KillPurse(level, difficulty, rules);
            var standing = new List<Standing>();
            int spent = 0;
            int next = 0;
            int harvestWave = 0;
            int trunkGates = TrunkGates(level);
            SiegeState siege = SiegeState.Start(level, trunkGates);
            float repairRate = 0f;
            int cheapestGun = CheapestGun(level);
            float attackDps = 0f;
            int attackTargets = 0;
            float coverSum = 0f;
            int covered = 0;
            float margin = float.MaxValue;
            outcome.BaseStart = level.BaseHealth;
            outcome.WaveCount = waves.Count;

            for (int i = 0; i < waves.Count; i++)
            {
                int wave = i + 1;
                purse.Begin(waves[i]);
                WaveLoad load = Ledger.LoadOf(level, waves[i], difficulty, rules, purse.Carried(waves[i]), trunkGates > 0 ? siege.GatesLeft / trunkGates : -1f);
                int money = level.StartingMoney + (int)purse.Paid + MineIncome(level, i, waves.Count, rules, harvestWave) - spent;
                var line = new PlanWaveLine { Wave = wave, Boss = load.HasBoss, Money = money, KillMoney = load.KillMoney };
                var mines = new List<GadgetStats>();
                var fields = new List<(GadgetStats Gadget, Cell Cell)>();
                if (policy != null)
                {
                    view.WaveIndex = i;
                    view.Money = money;
                    view.Siege = siege;
                    view.Harvester = harvestWave > 0;
                    view.Standing.Clear();
                    foreach (Standing tower in standing)
                    {
                        view.Standing.Add((tower.Tower, tower.Level, tower.Cell));
                    }

                    var chosen = new List<PlanStep>();
                    policy(view, chosen);
                    foreach (PlanStep step in chosen)
                    {
                        step.Wave = wave;
                    }

                    steps.InsertRange(next, chosen);
                }

                // Due steps in order, while the purse has them.
                while (next < steps.Count && steps[next].Wave <= wave)
                {
                    PlanStep step = steps[next];
                    int towerIndex = -1;
                    int price = step.Kind == PlanStep.Repair ? RepairPrice(level, load, siege, cheapestGun, waves.Count - i)
                        : step.Kind == PlanStep.GateUpgrade ? GateUpgradePrice(level, siege)
                        : Price(level, standing, step, out towerIndex);
                    if (price < 0)
                    {
                        // A step that cannot be taken at all (no such tower, nothing to upgrade) is skipped, not waited for; a policy's
                        // skipped step leaves the concrete order, so the README shows what was really done.
                        if (policy != null)
                        {
                            steps.RemoveAt(next);
                        }
                        else
                        {
                            next++;
                        }

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
                            view?.Drones.Add(step.Item);
                            harvestWave = drone.Role == DroneRole.Harvest && harvestWave == 0 ? wave : harvestWave;
                            attackDps += drone.Role == DroneRole.Attack ? drone.Dps : 0f;
                            attackTargets |= drone.Role == DroneRole.Attack ? drone.Targets : 0;
                            repairRate += drone.Role == DroneRole.Repair ? drone.RepairPerSecond : 0f;
                            break;
                        case PlanStep.Repair:
                            outcome.RepairSpent += price;
                            outcome.Repairs++;
                            siege.RepairGates(level);
                            break;
                        case PlanStep.GateUpgrade:
                            siege.UpgradeGate(level);
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
                var guards = new SiegeGuards();
                Leak.AddBaseTurrets(ref guards, level, load, rules);
                foreach (Standing tower in standing)
                {
                    TowerStats stats = level.Towers[tower.Tower];
                    float range = stats.Levels[tower.Level].Range * level.BoostOf(stats.Id).Range;
                    SpotCover cover = CoverOf(level, tower.Cell, range);
                    Ledger.Option option = Ledger.Capacity(level, load, rules, tower.Tower, tower.Level, cover);
                    float slow = FieldGain(level, fields, tower.Cell, range, load);
                    stream += option.Stream * slow;
                    single += option.Single * slow;
                    air += option.Air;
                    Leak.AddTower(ref guards, level, load, rules, tower.Tower, tower.Level, cover, -1f, slow);
                }

                // The defence gate's own gun (round 26): a queue guard that also fires on the approach in its reach.
                if (siege.GateLevel > 0 && siege.DefenceGate > 0f)
                {
                    Ledger.Option gateGun = Ledger.GateCapacity(level, load, rules, siege.GateLevel);
                    stream += gateGun.Stream;
                    single += gateGun.Single;
                    Leak.AddGate(ref guards, level, load, rules, siege.GateLevel);
                }

                if (attackDps > 0f)
                {
                    // The attack drone fires everywhere: its kills count on the walk.
                    float drone = attackDps * rules.Utilisation;
                    float droneStream = Layer.Has(attackTargets, Layer.Ground) ? drone * load.Window : 0f;
                    float droneSingle = Layer.Has(attackTargets, Layer.Ground) ? drone * load.BossSeconds(load.BossWindow + load.BossFight, rules) : 0f;
                    float droneAir = Layer.Has(attackTargets, Layer.Air) ? drone * load.AirWindow : 0f;
                    stream += droneStream;
                    single += droneSingle;
                    air += droneAir;
                    guards.WalkStream += droneStream;
                    guards.WalkSingle += droneSingle;
                    guards.WalkAir += droneAir;
                }

                line.Towers = standing.Count;
                line.GroundPressure = load.GroundPressure(stream, single);
                line.AirPressure = load.AirPressure(air);
                float waveSeconds = waves[i].StartDelay + load.Window;
                SiegeLine leak = Leak.Resolve(level, load, rules, guards, siege, waveSeconds);
                line.Leaked = leak.GroundLeak + leak.AirLeak;
                line.GateDamage = leak.GateDamage;
                line.BaseDamage = leak.BaseDamage;
                line.HeroFell = leak.HeroFell;

                // A repair drone restores the gates first, then the base.
                if (repairRate > 0f && siege.BaseLeft > 0f)
                {
                    line.DroneRepaired = siege.Restore(level, repairRate * rules.RepairDroneBusyShare * waveSeconds);
                }

                line.GateLeft = siege.GatesLeft;
                line.BaseLeft = siege.BaseLeft;
                if (view != null && trunkGates > 0 && level.GateHealth > 0f)
                {
                    view.Wear = Math.Max(view.Wear, 1f - siege.DefenceGate / level.GateHealth);
                }
                if (siege.BaseLeft <= 0f && outcome.FallWave == 0)
                {
                    outcome.FallWave = wave;
                }

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

            outcome.StepsBlocked = steps.Count - next;
            if (policy != null)
            {
                outcome.Plan = new PlanSpec { Level = plan.Level, Difficulty = plan.Difficulty, Name = plan.Name, Family = plan.Family, Steps = steps.ToArray() };
            }
            outcome.Margin = margin == float.MaxValue ? 0f : margin;
            outcome.MeanCover = covered > 0 ? coverSum / covered : 0f;
            outcome.BaseLeft = siege.BaseLeft;
            outcome.GateLeft = siege.GatesLeft;
            outcome.Stars = Leak.Stars(siege.BaseLeft, outcome.BaseStart, level.StarFractions);
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

        /// <summary>Gates on the trunk (the ones every walker passes), whose health the plan model tracks; zero without gates.</summary>
        public static int TrunkGates(LevelInput level)
        {
            int gates = 0;
            int firstLane = level.Shape.Lanes.Count > 0 ? level.Shape.Lanes[0].PathIndex : 0;
            foreach (LevelShape.Gate gate in level.Shape.Gates)
            {
                gates += gate.OnTrunk || gate.PathIndex == firstLane ? 1 : 0;
            }

            return level.GateHealth > 0f ? gates : 0;
        }

        /// <summary>What a whole queue takes off one trunk gate over a full hold: the bots' yardstick for "the gate falls in the coming wave".</summary>
        public static float ExpectedGateLoss(WaveLoad load, int trunkGates)
        {
            float perGate = Math.Max(1, trunkGates);
            return (load.GateStrike * load.GateHold + load.BossGateStrike * load.BossGateHold) / perGate;
        }

        /// <summary>
        /// The price of repairing the gates now, or -1 when the step is not taken: nothing to repair, or not worth it by the bot's rule
        /// (Remco 2026-11-06): repair only when the defence gate would fall in the coming wave, or when a hit point is cheaper to
        /// restore than to stop with the cheapest gun over the waves left. A wrecked gate costs its build price.
        /// </summary>
        /// <summary>
        /// The price of raising the defence gate a level (round 26), or -1 when it cannot be raised: no gate upgrades, the top level
        /// reached, or the gate wrecked (a rebuild first). The bots and the strategies weigh it like a tower upgrade: the gun's gain
        /// per coin, with the health bonus credited at the repair price of that much health (<see cref="GateUpgradeWorth"/>).
        /// </summary>
        public static int GateUpgradePrice(LevelInput level, SiegeState siege) =>
            level.HasGateUpgrades && siege.TrunkGates > 0 && siege.DefenceGate > 0f && siege.GateLevel + 1 < level.Gate.Levels.Length ? level.Gate.Levels[siege.GateLevel + 1].Cost : -1;

        /// <summary>The coins the next gate level's health bonus would cost as a repair: what the step is worth before its gun counts.</summary>
        public static int GateUpgradeWorth(LevelInput level, int gateLevel)
        {
            if (!level.HasGateUpgrades || gateLevel + 1 >= level.Gate.Levels.Length)
            {
                return 0;
            }

            float bonus = level.Gate.Levels[gateLevel + 1].HealthBonus - level.Gate.Levels[gateLevel].HealthBonus;
            return Math.Max(0, (int)Math.Round(bonus * level.GateRepairCostPerHealth));
        }

        public static int RepairPrice(LevelInput level, WaveLoad load, SiegeState siege, int cheapestGun, int wavesLeft)
        {
            float full = level.GateHealth * siege.TrunkGates + siege.GateBonus;
            if (siege.TrunkGates == 0 || siege.GatesLeft >= full)
            {
                return -1;
            }

            int wrecked = (siege.DefenceGate <= 0f ? 1 : 0) + (siege.TrunkGates > 1 && siege.RestGates <= 0f ? siege.TrunkGates - 1 : 0);
            float missing = full - siege.GatesLeft;
            float repairable = Math.Max(0f, missing - wrecked * level.GateHealth - (siege.DefenceGate <= 0f ? siege.GateBonus : 0f));
            int price = wrecked * level.GateRebuildCost + (int)Math.Ceiling(repairable * level.GateRepairCostPerHealth);
            float expected = ExpectedGateLoss(load, siege.TrunkGates);
            bool falls = siege.DefenceGate <= expected;
            float repairPerPoint = price / Math.Max(1f, missing);
            float gunPerPoint = cheapestGun / Math.Max(1f, expected * Math.Max(1, wavesLeft));
            return falls || (cheapestGun > 0 && repairPerPoint < gunPerPoint) ? price : -1;
        }

        /// <summary>The cheapest gun the level sells (the bots' yardstick for a repair's worth), zero without one.</summary>
        public static int CheapestGun(LevelInput level)
        {
            int cheapest = 0;
            foreach (TowerStats tower in level.Towers)
            {
                if (tower.IsGun && (cheapest == 0 || tower.Levels[0].Cost < cheapest))
                {
                    cheapest = tower.Levels[0].Cost;
                }
            }

            return cheapest;
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
