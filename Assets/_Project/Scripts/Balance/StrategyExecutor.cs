using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// Executes a <see cref="StrategySpec"/> on paper (round 22e): the same decisions the adaptive PlayMode bot takes each wave
    /// (BalanceProbeTests.Act with a strategy), judged with the ledger from the towers' own spots. Each wave it repairs by the
    /// bots' rule, buys the harvester while the guns hold, saves for a sky gun before a raid the standing sky guns cannot hold and
    /// a splash gun before the first crowd wave, lays mines and a slow field by the strategy's triggers, buys the repair and attack
    /// drones by its timing, and otherwise takes the best step per coin (an upgrade against a new tower of the favoured type on its
    /// best free spot), waiting a wave of kills for a clearly better step. The outcome's plan is the concrete order taken, which the
    /// README prints beside the strategy's words. The paper is a pre-screen; the replay is the judge.
    /// </summary>
    public static class StrategyExecutor
    {
        // The bot's own constants (BalanceProbeTests), mirrored: the horizon it judges against, the ground pressure above which it
        // builds instead of waiting, the margin of income before a raid that starts the saving, how much better a step a wave away
        // must be before it waits, the least a spot counts, the guns before a second sky gun and the sky guns per ground gun.
        private const int Horizon = 1;
        private const float GroundCeiling = 1.6f;
        private const float ReserveCeiling = 1.6f;
        private const float ReserveMargin = 2f;
        private const float WaitRatio = 1.3f;
        private const float MinSpotQuality = 0.3f;
        private const float OtherSideWeight = 0.01f;
        private const int GroundBeforeSecondSkyGun = 3;
        private const int SkyPerGround = 2;
        private const int HarvestAfterTowers = 3;
        private const int NoHarvestLastWaves = 2;
        private const int RepairAfterTowers = 4;
        private const int MinesAfterTowers = 4;
        private const int MinesPerWave = 2;
        private const float CrowdSplashTargets = 2f;
        private const int PurchasesPerWave = 8;
        private const int QueueCells = 4;

        /// <summary>Ground creatures that make a wave a swarm for the threat shares.</summary>
        private const float SwarmCount = 12f;

        private sealed class Context
        {
            public LevelInput Level;
            public IReadOnlyList<WaveSpec> Waves;
            public BalanceRules Rules;
            public StrategySpec Spec;
            public readonly List<WaveLoad> Loads = new List<WaveLoad>();
            public readonly List<int> Ground = new List<int>();
            public readonly List<int> Sky = new List<int>();
            public readonly Dictionary<int, List<Cell>> Spots = new Dictionary<int, List<Cell>>();
            public readonly Dictionary<int, float> BestPass = new Dictionary<int, float>();
            public readonly List<Cell> Queue = new List<Cell>();
            public GadgetStats Mine;
            public GadgetStats Field;
            public DroneStats Harvester;
            public DroneStats Repair;
            public DroneStats Attack;
            public int CheapestGun;
            public int Splash = -1;
            public int MinesWave = -1;
            public int MinesThisWave;
            public int FieldWave = -1;
            public bool Air;

            /// <summary>The defence gate's upgrade level this wave (round 26), from the evaluator's siege state; its gun counts in every cover.</summary>
            public int GateLevel;
            public PlanEvaluator.PolicyView View;
        }

        /// <summary>Judges a strategy on paper; the outcome's plan is the build order it took.</summary>
        public static PlanOutcome Evaluate(PlanSearchInput input, StrategySpec spec)
        {
            var plan = new PlanSpec { Level = input.LevelKey, Difficulty = input.Difficulty, Name = spec.Name, Family = spec.Family, Steps = new PlanStep[0] };
            return PlanEvaluator.Evaluate(input.Level, input.Waves, input.Stats, input.Judge, plan, input.Cap, Policy(input, spec));
        }

        /// <summary>
        /// The spots per tower type the search draws from and the executor falls back on: the geometry's best few by the ranking
        /// (approach and queue first), plus the rear spots (round 31): the spot with the most road behind the defence gate and the
        /// best doorstep spot. A nest or an entrance that joins the road behind the gate walks past the gate's guns (Crystal
        /// Plains' rift nest), so a thought-out plan may need one gun at the rear, and the search can only find it when the spot
        /// is on offer; the paper's passed-stream term decides whether it pays.
        /// </summary>
        public static List<Cell> RankedSpots(LevelInput level, TowerStats tower, int count)
        {
            bool airOnly = Layer.Has(tower.Targets, Layer.Air) && !Layer.Has(tower.Targets, Layer.Ground);
            if (level.Geometry == null)
            {
                return new List<Cell>();
            }

            float reach = tower.Levels[0].Range;
            List<Cell> ranked = level.Geometry.Rank(reach, airOnly, tower.Splash > 0f);
            List<Cell> spots = ranked.GetRange(0, Math.Min(count, ranked.Count));
            if (airOnly || !level.Geometry.DefenceGate.HasValue)
            {
                return spots;
            }

            Cell? rear = null;
            Cell? door = null;
            float rearPass = 0f;
            float doorPass = 0f;
            foreach (Cell spot in ranked)
            {
                SpotCover cover = level.Geometry.CoverAt(spot, reach);
                if (cover.BehindPass > rearPass)
                {
                    rearPass = cover.BehindPass;
                    rear = spot;
                }

                if (cover.Base && cover.GroundPass > doorPass)
                {
                    doorPass = cover.GroundPass;
                    door = spot;
                }
            }

            AddSpot(spots, rear);
            AddSpot(spots, door);
            return spots;
        }

        private static void AddSpot(List<Cell> spots, Cell? spot)
        {
            if (spot.HasValue && !spots.Exists(c => c.X == spot.Value.X && c.Y == spot.Value.Y))
            {
                spots.Add(spot.Value);
            }
        }

        private static PlanEvaluator.PlanPolicy Policy(PlanSearchInput input, StrategySpec spec)
        {
            var context = new Context { Level = input.Level, Waves = input.Waves, Rules = input.Judge, Spec = spec };
            LevelInput level = input.Level;
            for (int t = 0; t < level.Towers.Count; t++)
            {
                TowerStats tower = level.Towers[t];
                if (!tower.IsGun)
                {
                    continue;
                }

                bool airOnly = Layer.Has(tower.Targets, Layer.Air) && !Layer.Has(tower.Targets, Layer.Ground);
                (airOnly ? context.Sky : context.Ground).Add(t);
                var spots = new List<Cell>();
                SpotPriority priority = spec.SpotsOf(tower.Id);
                for (int i = 0; priority != null && i < priority.Count; i++)
                {
                    spots.Add(priority.At(i));
                }

                foreach (Cell cell in RankedSpots(level, tower, input.SpotsPerType))
                {
                    if (!spots.Exists(c => c.X == cell.X && c.Y == cell.Y))
                    {
                        spots.Add(cell);
                    }
                }

                context.Spots[t] = spots;
                float best = 0f;
                foreach (Cell cell in spots)
                {
                    best = Math.Max(best, PlanEvaluator.CoverOf(level, cell, tower.Levels[0].Range).GroundPass);
                }

                context.BestPass[t] = best;
                if (tower.Splash > 0f && !airOnly && (context.Splash < 0 || tower.Levels[0].Cost < level.Towers[context.Splash].Levels[0].Cost))
                {
                    context.Splash = t;
                }
            }

            foreach (WaveSpec wave in input.Waves)
            {
                WaveLoad load = WaveLoad.Of(level, wave, input.Stats, input.Judge);
                context.Loads.Add(load);
                context.Air |= load.AirHealth > 0f;
            }

            if (level.Geometry != null)
            {
                context.Queue.AddRange(level.Geometry.QueueCellsBeforeGate(QueueCells));
            }

            context.Mine = level.Gadgets.Find(g => g.Kind == GadgetKind.Mine);
            context.Field = level.Gadgets.Find(g => g.Kind == GadgetKind.SlowField);
            context.Harvester = level.Drones.Find(d => d.Role == DroneRole.Harvest);
            context.Repair = level.Drones.Find(d => d.Role == DroneRole.Repair);
            context.Attack = level.Drones.Find(d => d.Role == DroneRole.Attack);
            context.CheapestGun = PlanEvaluator.CheapestGun(level);
            return (view, steps) => Decide(context, view, steps);
        }

        // One wave of decisions: what the bot does between the wave's buys, with the purse as the paper sees it at the wave's start.
        private static void Decide(Context context, PlanEvaluator.PolicyView view, List<PlanStep> steps)
        {
            StrategySpec spec = context.Spec;
            LevelInput level = context.Level;
            int money = view.Money;
            int wave = view.WaveIndex;
            int waveCount = context.Loads.Count;
            var standing = new List<(int Tower, int Level, Cell Cell)>(view.Standing);
            var used = new HashSet<long>();
            foreach ((int, int, Cell) tower in standing)
            {
                used.Add(Key(tower.Item3));
            }

            if (context.MinesWave != wave)
            {
                context.MinesWave = wave;
                context.MinesThisWave = 0;
            }

            context.View = view;
            context.GateLevel = view.Siege != null ? view.Siege.GateLevel : 0;

            // Repairs by the bots' rule: the evaluator takes the step only when the gate would fall or a hit point is cheaper to restore.
            steps.Add(new PlanStep { Kind = PlanStep.Repair, Item = PlanStep.Gates });

            void Take(PlanStep step, int price)
            {
                steps.Add(step);
                money -= price;
            }

            bool Build(int tower)
            {
                foreach (Cell cell in context.Spots[tower])
                {
                    if (used.Add(Key(cell)))
                    {
                        Take(new PlanStep { Kind = PlanStep.Build, Item = level.Towers[tower].Id, X = cell.X, Y = cell.Y }, level.Towers[tower].Levels[0].Cost);
                        standing.Add((tower, 0, cell));
                        return true;
                    }
                }

                return false;
            }

            for (int purchase = 0; purchase < PurchasesPerWave; purchase++)
            {
                int ground = 0;
                int sky = 0;
                foreach ((int, int, Cell) stands in standing)
                {
                    ground += context.Sky.Contains(stands.Item1) ? 0 : 1;
                    sky += context.Sky.Contains(stands.Item1) ? 1 : 0;
                }

                float groundCover = Cover(context, standing, wave, true);
                bool groundHolds = groundCover >= 1f / GroundCeiling;

                // The harvester: an early investment while the guns hold the waves at hand (the strategy says how well and how long).
                bool harvesterWanted = spec.HarvesterCover > 0f && context.Harvester != null && !view.Harvester && !view.Drones.Contains(context.Harvester.Id)
                    && level.MineCount > 0 && standing.Count >= HarvestAfterTowers && wave + 1 <= Math.Ceiling(waveCount * spec.HarvesterShare)
                    && wave + 1 <= waveCount - NoHarvestLastWaves && groundCover >= spec.HarvesterCover;
                if (harvesterWanted)
                {
                    if (money < context.Harvester.Cost)
                    {
                        return;
                    }

                    Take(new PlanStep { Kind = PlanStep.Drone, Item = context.Harvester.Id }, context.Harvester.Cost);
                    view.Drones.Add(context.Harvester.Id);
                    continue;
                }

                int reserve = AirReserve(context, standing, wave, ground, sky);
                if (reserve == 0 && Gadgets(context, view, standing, wave, groundCover, ref money, steps))
                {
                    continue;
                }

                bool noSkyGun = sky == 0;
                if (!(groundCover >= 1f / ReserveCeiling || noSkyGun))
                {
                    reserve = 0;
                }

                if (reserve > 0 && money >= reserve && Build(CheapestSky(context)))
                {
                    continue;
                }

                // The purse is short of the sky gun: its price stays aside and the rest buys what it can (a sale on paper is not
                // modelled; the replay may sell on Psycho).
                int splash = SplashReserve(context, standing, wave);
                if (splash > 0 && money >= reserve + splash && Build(context.Splash))
                {
                    continue;
                }

                int purse = money - reserve - splash;
                int reach = groundHolds ? purse + context.Loads[wave].KillMoney : purse;
                if (!BestStep(context, standing, used, wave, purse, reach, groundHolds, out PlanStep step, out int price, out int tower, out Cell cell, out bool wait) || wait)
                {
                    return;
                }

                Take(step, price);
                if (step.Kind == PlanStep.GateUpgrade)
                {
                    // The evaluator raises the siege state when it pays the step; the rest of this wave's choices see the new gun.
                    context.GateLevel++;
                }
                else if (step.Kind == PlanStep.Build)
                {
                    standing.Add((tower, 0, cell));
                    used.Add(Key(cell));
                }
                else
                {
                    int at = standing.FindIndex(s => s.Cell.X == cell.X && s.Cell.Y == cell.Y);
                    standing[at] = (standing[at].Tower, standing[at].Level + 1, cell);
                }
            }
        }

        // The best step per coin, with the strategy's biases: an upgrade (times UpgradeBias and the gun's own spot) or a new tower of the
        // favoured type (times the threat weight and its next free spot). True with wait when a step a wave of kills away is clearly better.
        private static bool BestStep(Context context, List<(int Tower, int Level, Cell Cell)> standing, HashSet<long> used, int wave, int purse, int reach, bool groundHolds,
            out PlanStep step, out int price, out int tower, out Cell cell, out bool wait)
        {
            LevelInput level = context.Level;
            StrategySpec spec = context.Spec;
            step = null;
            price = 0;
            tower = -1;
            cell = default;
            wait = false;
            float now = Math.Min(Cover(context, standing, wave, true), Cover(context, standing, wave, false));
            float nowGround = Cover(context, standing, wave, true);
            float nowAir = Cover(context, standing, wave, false);
            float bestValue = 0f;
            float waitValue = 0f;
            int groundSpots = 0;
            foreach (int t in context.Ground)
            {
                groundSpots = Math.Max(groundSpots, context.Spots[t].Count);
            }

            for (int i = 0; i < standing.Count; i++)
            {
                (int t, int l, Cell c) = standing[i];
                TowerStats stats = level.Towers[t];
                if (l + 1 >= stats.Levels.Length || stats.Levels[l + 1].Cost > reach || (standing.Count < spec.TowersBeforeUpgrades && standing.Count < groundSpots))
                {
                    continue;
                }

                var next = new List<(int, int, Cell)>(standing);
                next[i] = (t, l + 1, c);
                float value = Gain(context, next, wave, now, nowGround, nowAir) / Math.Max(1, stats.Levels[l + 1].Cost) * Quality(context, t, c) * spec.UpgradeBias;
                if (stats.Levels[l + 1].Cost > purse)
                {
                    waitValue = Math.Max(waitValue, value);
                }
                else if (value > bestValue)
                {
                    bestValue = value;
                    step = new PlanStep { Kind = PlanStep.Upgrade, X = c.X, Y = c.Y };
                    price = stats.Levels[l + 1].Cost;
                    tower = t;
                    cell = c;
                }
            }

            // The defence gate's next level (round 26): its gun's gain per coin, the health bonus credited at the repair price, the
            // strategy's own weight on it; the gate stands in front of the queue, so no spot quality applies.
            bool gateStands = context.View != null && context.View.Siege != null && context.View.Siege.TrunkGates > 0 && context.View.Siege.DefenceGate > 0f;
            int gatePrice = context.Spec.GateUpgradeBias > 0f && gateStands && level.HasGateUpgrades && context.GateLevel + 1 < level.Gate.Levels.Length ? level.Gate.Levels[context.GateLevel + 1].Cost : -1;
            if (gatePrice > 0 && gatePrice <= reach && standing.Count >= spec.TowersBeforeUpgrades)
            {
                context.GateLevel++;
                float gain = Gain(context, standing, wave, now, nowGround, nowAir);
                context.GateLevel--;
                int worth = PlanEvaluator.GateUpgradeWorth(level, context.GateLevel);
                float value = gain / Math.Max(1, gatePrice - worth) * spec.UpgradeBias * spec.GateUpgradeBias;
                if (gatePrice > purse)
                {
                    waitValue = Math.Max(waitValue, value);
                }
                else if (value > bestValue)
                {
                    bestValue = value;
                    step = new PlanStep { Kind = PlanStep.GateUpgrade, Item = PlanStep.Gates, X = level.GateCell.Value.X, Y = level.GateCell.Value.Y };
                    price = gatePrice;
                    tower = -1;
                    cell = level.GateCell.Value;
                }
            }

            if (standing.Count < groundSpots)
            {
                foreach (int t in context.Ground)
                {
                    TowerStats stats = level.Towers[t];
                    if (stats.Levels[0].Cost > reach || !NextFree(context, t, used, out Cell free))
                    {
                        continue;
                    }

                    var next = new List<(int, int, Cell)>(standing) { (t, 0, free) };
                    float value = Gain(context, next, wave, now, nowGround, nowAir) / Math.Max(1, stats.Levels[0].Cost) * Quality(context, t, free) * ThreatWeight(context, wave, stats.Id);
                    if (stats.Levels[0].Cost > purse)
                    {
                        waitValue = Math.Max(waitValue, value);
                    }
                    else if (value > bestValue)
                    {
                        bestValue = value;
                        step = new PlanStep { Kind = PlanStep.Build, Item = stats.Id, X = free.X, Y = free.Y };
                        price = stats.Levels[0].Cost;
                        tower = t;
                        cell = free;
                    }
                }
            }

            wait = waitValue > bestValue * WaitRatio;
            return step != null;
        }

        // The strategy's answer to the threats of the waves at hand: the mean of its weights for the type over the threat shares.
        private static float ThreatWeight(Context context, int wave, string towerId)
        {
            float sum = 0f;
            float weight = 0f;
            for (int i = wave; i <= wave + Horizon && i < context.Loads.Count; i++)
            {
                foreach ((string threat, float share) in ThreatShares(context, context.Loads[i]))
                {
                    sum += share;
                    weight += share * context.Spec.WeightOf(threat, towerId);
                }
            }

            return sum > 0f ? weight / sum : 1f;
        }

        /// <summary>How much of a wave is each threat (0 to 1 each): a swarm by count, packs by density, armour by the share of its damage the cheapest gun loses to it, air and boss by presence.</summary>
        private static List<(string Threat, float Share)> ThreatShares(Context context, WaveLoad load) => ThreatShares(context.Level, load, context.CheapestGun);

        public static List<(string Threat, float Share)> ThreatShares(LevelInput level, WaveLoad load, int cheapestGun)
        {
            float lightFactor = 1f;
            foreach (TowerStats tower in level.Towers)
            {
                if (tower.IsGun && tower.Levels[0].Cost == cheapestGun)
                {
                    lightFactor = tower.ArmourFactor;
                }
            }

            float total = load.StreamHealth + load.BossHealth + load.AirHealth;
            float groundShare = total > 0f ? load.StreamHealth / total : 0f;
            var shares = new List<(string, float)>
            {
                (StrategySpec.Swarm, groundShare * Math.Min(1f, load.GroundCount / SwarmCount)),
                (StrategySpec.Pack, groundShare * Math.Min(1f, 1.5f * load.GroundDensity)),
                (StrategySpec.Plated, groundShare * (load.StreamArmour.IsSoft ? 0f : 1f - load.StreamArmour.Share(lightFactor))),
                (StrategySpec.Air, total > 0f ? load.AirHealth / total : 0f),
                (StrategySpec.Boss, total > 0f ? load.BossHealth / total : 0f),
            };
            return shares;
        }

        private static bool Gadgets(Context context, PlanEvaluator.PolicyView view, List<(int Tower, int Level, Cell Cell)> standing, int wave, float groundCover, ref int money, List<PlanStep> steps)
        {
            StrategySpec spec = context.Spec;
            int keep = context.CheapestGun;
            bool pressed = standing.Count >= MinesAfterTowers;
            // Drones by the strategy's timing: the repair drone once the gate has worn, the attack drone with a full line.
            if (context.Repair != null && spec.RepairDroneWear < 1f && view.Wear >= spec.RepairDroneWear && standing.Count >= RepairAfterTowers && !view.Drones.Contains(context.Repair.Id) && money >= context.Repair.Cost)
            {
                steps.Add(new PlanStep { Kind = PlanStep.Drone, Item = context.Repair.Id });
                view.Drones.Add(context.Repair.Id);
                money -= context.Repair.Cost;
                return true;
            }

            if (context.Attack != null && spec.AttackDroneAfterTowers > 0 && standing.Count >= spec.AttackDroneAfterTowers && !view.Drones.Contains(context.Attack.Id) && money >= context.Attack.Cost)
            {
                steps.Add(new PlanStep { Kind = PlanStep.Drone, Item = context.Attack.Id });
                view.Drones.Add(context.Attack.Id);
                money -= context.Attack.Cost;
                return true;
            }

            if (context.Mine != null && context.Queue.Count > 0 && pressed && context.MinesThisWave < MinesPerWave && money >= context.Mine.Cost + keep)
            {
                bool wanted = false;
                for (int i = wave; i <= wave + Horizon && i < context.Loads.Count; i++)
                {
                    WaveLoad load = context.Loads[i];
                    bool crowd = spec.MineCoverBelow > 0f && groundCover < spec.MineCoverBelow && 1f + 2f * context.Mine.Radius * load.GroundDensity >= CrowdSplashTargets;
                    wanted |= (spec.MineOnBoss && load.HasBoss) || crowd;
                }

                if (wanted)
                {
                    Cell cell = context.Queue[context.MinesThisWave % context.Queue.Count];
                    steps.Add(new PlanStep { Kind = PlanStep.Mine, Item = context.Mine.Id, X = cell.X, Y = cell.Y });
                    money -= context.Mine.Cost;
                    context.MinesThisWave++;
                    return true;
                }
            }

            if (context.Field != null && spec.FieldCoverBelow > 0f && context.Queue.Count > 0 && context.FieldWave != wave && pressed && groundCover < spec.FieldCoverBelow && money >= context.Field.Cost + keep)
            {
                Cell cell = context.Queue[0];
                steps.Add(new PlanStep { Kind = PlanStep.Field, Item = context.Field.Id, X = cell.X, Y = cell.Y });
                money -= context.Field.Cost;
                context.FieldWave = wave;
                return true;
            }

            return false;
        }

        // The price of a sky gun while a raid ahead would beat the standing sky guns and the saving has started (the bot's AirReserve).
        private static int AirReserve(Context context, List<(int Tower, int Level, Cell Cell)> standing, int wave, int ground, int sky)
        {
            if (!context.Air || context.Sky.Count == 0 || ground == 0 || (sky > 0 && ground < GroundBeforeSecondSkyGun) || (sky > 0 && sky * SkyPerGround > ground))
            {
                return 0;
            }

            int price = context.Level.Towers[CheapestSky(context)].Levels[0].Cost;
            int raid = -1;
            for (int i = wave; i < context.Loads.Count && raid < 0; i++)
            {
                if (context.Loads[i].AirHealth > 0f && Pressure(context, standing, i).Air > 1f)
                {
                    raid = i;
                }
            }

            if (raid < 0)
            {
                return 0;
            }

            // Saving starts when the kills from here to the raid cover the price with the bot's margin.
            float income = 0f;
            for (int i = wave; i < raid; i++)
            {
                income += context.Loads[i].KillMoney;
            }

            return income <= price * ReserveMargin || raid == wave ? price : 0;
        }

        private static int SplashReserve(Context context, List<(int Tower, int Level, Cell Cell)> standing, int wave)
        {
            if (context.Splash < 0 || standing.Exists(s => context.Level.Towers[s.Tower].Splash > 0f && Layer.Has(context.Level.Towers[s.Tower].Targets, Layer.Ground)))
            {
                return 0;
            }

            float radius = context.Level.Towers[context.Splash].Splash;
            int price = context.Level.Towers[context.Splash].Levels[0].Cost;
            for (int i = wave; i < context.Loads.Count; i++)
            {
                if (1f + 2f * radius * context.Loads[i].GroundDensity >= CrowdSplashTargets)
                {
                    float income = 0f;
                    for (int j = wave; j < i; j++)
                    {
                        income += context.Loads[j].KillMoney;
                    }

                    return income <= price * ReserveMargin || i == wave ? price : 0;
                }
            }

            return 0;
        }

        private static int CheapestSky(Context context)
        {
            int best = context.Sky[0];
            foreach (int t in context.Sky)
            {
                best = context.Level.Towers[t].Levels[0].Cost < context.Level.Towers[best].Levels[0].Cost ? t : best;
            }

            return best;
        }

        private static float Gain(Context context, List<(int, int, Cell)> next, int wave, float now, float nowGround, float nowAir)
        {
            float gain = Math.Min(Cover(context, next, wave, true), Cover(context, next, wave, false)) - now;
            if (gain > 0f)
            {
                return gain;
            }

            float side = Math.Max(Lift(Cover(context, next, wave, true), nowGround), Lift(Cover(context, next, wave, false), nowAir));
            return side * OtherSideWeight;
        }

        private static float Lift(float after, float before) => after >= PlanEvaluator.NoThreatCover || before >= PlanEvaluator.NoThreatCover ? 0f : Math.Max(0f, after - before);

        // The worse cover (one over the pressure) over the wave at hand and the next, on one side.
        private static float Cover(Context context, List<(int, int, Cell)> standing, int wave, bool ground)
        {
            float cover = PlanEvaluator.NoThreatCover;
            for (int i = wave; i <= wave + Horizon && i < context.Loads.Count; i++)
            {
                (float g, float a) = Pressure(context, standing, i);
                float pressure = ground ? g : a;
                if (pressure > 0f)
                {
                    cover = Math.Min(cover, float.IsInfinity(pressure) ? 0f : 1f / pressure);
                }
            }

            return cover;
        }

        /// <summary>The pressure of a wave against a standing set from its own spots: ground from every gun, air from the sky guns only (as the bot judges).</summary>
        private static (float Ground, float Air) Pressure(Context context, List<(int, int, Cell)> standing, int wave) =>
            Pressure(context.Level, context.Loads[wave], context.Rules, standing, context.Sky, context.GateLevel);

        public static (float Ground, float Air) Pressure(LevelInput level, WaveLoad load, BalanceRules rules, List<(int Tower, int Level, Cell Cell)> standing, List<int> sky, int gateLevel = 0)
        {
            float stream = Ledger.HeroStream(level, load, rules);
            float single = Ledger.HeroSingle(level, load, rules);
            float air = 0f;
            Ledger.Option gate = Ledger.GateCapacity(level, load, rules, gateLevel);
            stream += gate.Stream;
            single += gate.Single;
            foreach ((int t, int l, Cell cell) in standing)
            {
                TowerStats stats = level.Towers[t];
                float range = stats.Levels[l].Range * level.BoostOf(stats.Id).Range;
                Ledger.Option option = Ledger.Capacity(level, load, rules, t, l, PlanEvaluator.CoverOf(level, cell, range));
                stream += option.Stream;
                single += option.Single;
                air += sky.Contains(t) ? option.Air : 0f;
            }

            return (load.GroundPressure(stream, single), load.AirHealth > 0f ? load.AirPressure(air) : 0f);
        }

        private static float Quality(Context context, int tower, Cell cell)
        {
            if (context.Sky.Contains(tower))
            {
                return 1f;
            }

            float best = context.BestPass.TryGetValue(tower, out float b) ? b : 0f;
            float pass = PlanEvaluator.CoverOf(context.Level, cell, context.Level.Towers[tower].Levels[0].Range).GroundPass;
            return best > 0f ? Math.Max(MinSpotQuality, pass / best) : 1f;
        }

        private static bool NextFree(Context context, int tower, HashSet<long> used, out Cell cell)
        {
            foreach (Cell candidate in context.Spots[tower])
            {
                if (!used.Contains(Key(candidate)))
                {
                    cell = candidate;
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private static long Key(Cell cell) => ((long)cell.X << 32) ^ (uint)cell.Y;
    }
}
