using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>A level's own numbers that do not change with the campaign position: map, money, mines, gates.</summary>
    public sealed class LevelFacts
    {
        public string Key = string.Empty;
        public int StartingMoney;
        public float HealthScale = 1f;
        public LevelShape Shape = new LevelShape();
        public int MineCount;
        public int MineStock;

        /// <summary>False when the level offers no harvest drone (yet): its ore pays nothing.</summary>
        public bool Harvest = true;
        public int SecondRoad = -1;
        public float GateHealth;
        public float GateRepairCostPerHealth;
        public int GateRebuildCost;

        /// <summary>The base's health and the base shares that earn the stars (LevelDefinition), for the leak model.</summary>
        public float BaseHealth;
        public float[] StarFractions = { 0.5f, 0.9f };

        /// <summary>The base's own turret and how many sides of the base it can stand on (the permanent base weapon upgrade); null without one.</summary>
        public TowerStats BaseTurret;
        public int BaseTurretSides;

        /// <summary>The gadgets the level sells and the drones it offers, and its map for placement (null = ideal placement).</summary>
        public List<GadgetStats> Gadgets = new List<GadgetStats>();
        public List<DroneStats> Drones = new List<DroneStats>();
        public LevelGeometry Geometry;
    }

    /// <summary>
    /// The whole campaign as the balance model sees it: levels in play order, every tower, creature and upgrade, the hero
    /// and the starting set. Builds the ledger input for any level and difficulty, so the generator, the tests and the
    /// BalanceLab tool all judge a level against the same player.
    /// </summary>
    public sealed class CampaignModel
    {
        private const float CalibrationMinScale = 0.25f;
        private const float CalibrationMaxScale = 4f;
        private const int CalibrationIterations = 22;
        private const int CalibrationPasses = 3;

        /// <summary>When the bisection misses the target by more than this share, the whole scale range is scanned in this many steps.</summary>
        private const float ScanTolerance = 0.005f;
        private const int ScanSteps = 160;
        private const float CapStep = 0.92f;
        private const int CapRefinements = 8;
        private const int CapSweeps = 4;

        public readonly List<CampaignLevel> Levels = new List<CampaignLevel>();
        public readonly List<LevelFacts> Facts = new List<LevelFacts>();
        public readonly Dictionary<string, TowerStats> Towers = new Dictionary<string, TowerStats>();
        public readonly Dictionary<string, EnemyStats> Enemies = new Dictionary<string, EnemyStats>();
        public readonly List<UpgradeStats> Upgrades = new List<UpgradeStats>();
        public readonly List<DifficultyStats> Difficulties = new List<DifficultyStats>();
        public readonly List<string> StartingTowers = new List<string>();
        public HeroStats Hero = new HeroStats();
        public int HarvesterCost;
        public BalanceRules Rules = BalanceRules.Default;

        public int IndexOf(string key) => Levels.FindIndex(l => l.Key == key);

        public PlayerState PlayerAt(int levelIndex, int difficulty) =>
            Progression.StateAt(Levels, levelIndex, difficulty, StartingTowers, Upgrades, Rules);

        public LevelInput InputFor(int levelIndex, int difficulty) => InputFor(levelIndex, difficulty, PlayerAt(levelIndex, difficulty));

        /// <summary>
        /// As above for a player without permanent upgrades or perks (a fresh save), owning the towers the campaign has rewarded so far:
        /// what the probe bots play with, so the calibration judges the recorded runs against the player that really played them.
        /// </summary>
        public LevelInput InputForFreshPlayer(int levelIndex, int difficulty) => InputFor(levelIndex, difficulty, PlayerAt(levelIndex, difficulty).Plain());

        public LevelInput InputFor(int levelIndex, int difficulty, PlayerState player)
        {
            LevelFacts facts = Facts[levelIndex];
            DifficultyStats stats = difficulty >= 0 && difficulty < Difficulties.Count ? Difficulties[difficulty] : new DifficultyStats();
            var input = new LevelInput
            {
                Key = facts.Key,
                // Rounded as the game rounds it (Mathf.RoundToInt: to even), so the model's purse is the wallet's.
                StartingMoney = (int)Math.Round(facts.StartingMoney * stats.StartMoney),
                Income = stats.Income,
                HealthScale = facts.HealthScale,
                Shape = facts.Shape,
                MineCount = facts.MineCount,
                MineStock = facts.MineStock,
                Harvest = facts.Harvest,
                HarvesterCost = HarvesterCost,
                Hero = Hero,
                HeroDamage = player.HeroDamage,
                HeroPerks = player.HeroPerks,
                GateHealth = facts.GateHealth * player.GateHealth,
                GateRepairCostPerHealth = facts.GateRepairCostPerHealth,
                GateRebuildCost = facts.GateRebuildCost,
                BaseHealth = facts.BaseHealth + player.BaseHealthBonus,
                StarFractions = facts.StarFractions,
                BaseTurret = facts.BaseTurret,
                BaseTurrets = facts.BaseTurret != null ? Math.Min(facts.BaseTurretSides, player.BaseWeaponLevel) : 0,
                BaseTurretLevel = facts.BaseTurret != null ? Math.Max(0, player.BaseWeaponLevel - facts.BaseTurretSides) : 0,
                Gadgets = facts.Gadgets,
                Drones = facts.Drones,
                Geometry = facts.Geometry,
            };

            foreach (string id in Levels[levelIndex].TowerIds)
            {
                if (player.OwnedTowers.Contains(id) && Towers.TryGetValue(id, out TowerStats tower) && tower.IsGun)
                {
                    input.Towers.Add(tower);
                    input.Boosts[id] = player.Boosts.TryGetValue(id, out TowerBoost boost) ? boost : TowerBoost.None;
                }
            }

            return input;
        }

        /// <summary>
        /// As <see cref="InputFor"/>, but with the level's own starting money and full income: the economy the wave solver
        /// sizes creatures against. A difficulty's money shares (<see cref="DifficultyStats.Income"/>, <see cref="DifficultyStats.StartMoney"/>)
        /// are a lever of their own; solving against them would only make the creatures weaker to make up for the thinner purse.
        /// </summary>
        public LevelInput ReferenceInputFor(int levelIndex, int difficulty)
        {
            LevelInput input = InputFor(levelIndex, difficulty);
            input.StartingMoney = Facts[levelIndex].StartingMoney;
            input.Income = 1f;
            return input;
        }

        /// <summary>Solves a level's budgets on Medium and composes its waves for every difficulty.</summary>
        public List<List<WaveSpec>> BuildWaves(int levelIndex, WaveScriptSpec script, out float[] healths)
        {
            LevelFacts facts = Facts[levelIndex];
            // The script (with the builder's tuning) is the source of the target scale at generation; the asset records it for the checks.
            Levels[levelIndex].TargetScale = script.TargetScale;
            // The raid floor is this solve's to set (the Easy raid rule below), never the script's.
            script.FirstAirWaveFloor = new[] { 0 };
            int medium = Math.Min(WaveSolver.SolveDifficulty, Difficulties.Count - 1);
            healths = WaveSolver.Solve(script, Enemies, ReferenceInputFor(levelIndex, medium), Difficulties[medium], facts.SecondRoad, Rules);
            var sets = new List<List<WaveSpec>>();
            for (int d = 0; d < Difficulties.Count; d++)
            {
                float[] own = SolveDifficulty(levelIndex, script, d, medium, healths);
                if (d == EasyDifficulty)
                {
                    // The Easy raid rule (the curator, round 22f): the first raid waits until the expected player's kills have paid for
                    // a sky gun, plus a wave. Judged on the waves as solved; when it moves the raids, Easy is solved again with them moved.
                    int first = WaveComposer.FirstAirWave(script, d);
                    int floor = EasyRaidFloor(levelIndex, ComposedWaves(script, d, own, facts.SecondRoad));
                    if (first != int.MaxValue && floor > first)
                    {
                        script.FirstAirWaveFloor = FloorPerDifficulty(floor);
                        own = SolveDifficulty(levelIndex, script, d, medium, healths);
                    }
                }

                sets.Add(ComposedWaves(script, d, own, facts.SecondRoad));
            }

            return sets;
        }

        /// <summary>The first difficulty, where the raid rule applies.</summary>
        public const int EasyDifficulty = 0;

        /// <summary>
        /// The Easy raid rule's floor for a level: the first wave (1-based) by which the expected Easy player's kill money (the purse
        /// above the start money, the ledger's walk over <paramref name="easyWaves"/>) covers the cheapest owned sky gun, plus
        /// <see cref="BalanceRules.EasyRaidMarginWaves"/>. A gun made for the air counts first, else any gun that reaches it; with
        /// neither, or when the kills never cover it, the floor lies past the last wave and Easy has no raid.
        /// </summary>
        public int EasyRaidFloor(int levelIndex, IReadOnlyList<WaveSpec> easyWaves)
        {
            LevelInput input = InputFor(levelIndex, EasyDifficulty);
            int cost = CheapestSkyGunCost(input);
            int none = easyWaves.Count + 1;
            if (cost < 0)
            {
                return none;
            }

            List<WaveLine> lines = Ledger.Evaluate(input, easyWaves, Difficulties[EasyDifficulty], Rules.HumanTowersOnly());
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Money - lines[0].Money >= cost)
                {
                    return Math.Min(none, i + 1 + Rules.EasyRaidMarginWaves);
                }
            }

            return none;
        }

        // The cheapest sky gun the player brings: an air-only gun when owned, else the cheapest gun that reaches the air; -1 for none.
        private static int CheapestSkyGunCost(LevelInput input)
        {
            int airOnly = -1;
            int reaches = -1;
            foreach (TowerStats tower in input.Towers)
            {
                if (!tower.IsGun || !Layer.Has(tower.Targets, Layer.Air) || tower.Levels.Length == 0)
                {
                    continue;
                }

                int cost = tower.Levels[0].Cost;
                if (tower.Targets == Layer.Air)
                {
                    airOnly = airOnly < 0 ? cost : Math.Min(airOnly, cost);
                }

                reaches = reaches < 0 ? cost : Math.Min(reaches, cost);
            }

            return airOnly >= 0 ? airOnly : reaches;
        }

        private int[] FloorPerDifficulty(int easyFloor)
        {
            var floors = new int[Math.Max(1, Difficulties.Count)];
            floors[EasyDifficulty] = easyFloor;
            return floors;
        }

        private float[] SolveDifficulty(int levelIndex, WaveScriptSpec script, int d, int medium, float[] healths)
        {
            float[] own = d == medium ? healths : WaveSolver.SolveVariant(script, Enemies, ReferenceInputFor(levelIndex, d), Difficulties[d], healths, Facts[levelIndex].SecondRoad, Rules);
            return Rules.CalibrateToHuman ? CalibrateToHuman(levelIndex, script, d, own, Facts[levelIndex].SecondRoad) : own;
        }

        private List<WaveSpec> ComposedWaves(WaveScriptSpec script, int d, float[] healths, int secondRoad)
        {
            var waves = new List<WaveSpec>(script.Waves);
            for (int i = 0; i < script.Waves; i++)
            {
                waves.Add(Composed(script, d, i, healths[i], secondRoad));
            }

            return waves;
        }

        // A wave as the generator stores it: composed, on the clock the assets carry (the first wave's longer countdown included), so
        // the calibration judges exactly the waves the ledger will read back (round 22b: a 2 s purse shift put C1_04 Hard w3 over its cap).
        private WaveSpec Composed(WaveScriptSpec script, int difficulty, int index, float health, int secondRoad)
        {
            WaveSpec wave = WaveComposer.Compose(script, Enemies, difficulty, index + 1, health, secondRoad, Rules);
            wave.StartDelay = index == 0 ? Rules.FirstWaveDelay : Rules.WaveDelay;
            return wave;
        }

        /// <summary>Index of a level within its continent (0 for the continent's first level).</summary>
        public int ContinentIndexOf(int levelIndex)
        {
            int continent = Levels[levelIndex].Continent;
            int index = 0;
            for (int i = 0; i < levelIndex; i++)
            {
                index += Levels[i].Continent == continent ? 1 : 0;
            }

            return index;
        }

        /// <summary>The plain-wave pressure the human lens should meet on a level and difficulty (one over the target surplus).</summary>
        public float HumanTarget(int levelIndex, int difficulty) => Levels[levelIndex].TargetScaleFor(difficulty) / Rules.HumanSurplusAt(difficulty, ContinentIndexOf(levelIndex));

        /// <summary>
        /// Trims one difficulty's health multipliers by a single factor, so a competent player's towers alone (the human lens,
        /// with the real money of the difficulty) meet <see cref="HumanTarget"/> on the plain waves. The solver gives the shape
        /// (ramp, rhythm, bosses, breathers) against the bot's calibration; this sets the level of the whole curve, so every
        /// level of a continent is a little harder than the one before, net of the upgrades the player is expected to own.
        /// </summary>
        /// <remarks>
        /// Then no wave may ask more of that player with the hero (<see cref="BalanceRules.HumanWithHero"/>) than the
        /// difficulty's <see cref="BalanceRules.HeroCap"/>: a boss wave or a spike above it is softened on its own (bosses keep
        /// their high health but stay killable), and the other waves are trimmed again so the plain mean stays on the curve.
        /// </remarks>
        private float[] CalibrateToHuman(int levelIndex, WaveScriptSpec script, int difficulty, float[] healths, int secondRoad)
        {
            BalanceRules human = Rules.HumanTowersOnly();
            BalanceRules withHero = Rules.HumanWithHero();
            LevelInput input = InputFor(levelIndex, difficulty);
            DifficultyStats stats = Difficulties[difficulty];
            float target = HumanTarget(levelIndex, difficulty);
            float cap = Rules.HeroCapFor(difficulty);
            var trimmed = (float[])healths.Clone();
            var capped = new bool[healths.Length];

            List<WaveLine> Evaluate(float[] health, BalanceRules rules)
            {
                var waves = new List<WaveSpec>(script.Waves);
                for (int i = 0; i < script.Waves; i++)
                {
                    waves.Add(Composed(script, difficulty, i, health[i], secondRoad));
                }

                return Ledger.Evaluate(input, waves, stats, rules);
            }

            for (int pass = 0; pass < CalibrationPasses; pass++)
            {
                // One factor on every wave not capped yet, so the plain mean meets the target.
                float[] start = (float[])trimmed.Clone();
                double factor = Bisect(logScale =>
                {
                    float scale = (float)Math.Exp(logScale);
                    var trial = new float[start.Length];
                    for (int i = 0; i < start.Length; i++)
                    {
                        trial[i] = capped[i] ? start[i] : start[i] * scale;
                    }

                    return BalanceChecks.PlainMean(Evaluate(trial, human));
                }, target, Math.Log(CalibrationMinScale), Math.Log(CalibrationMaxScale));
                for (int i = 0; i < trimmed.Length; i++)
                {
                    trimmed[i] = capped[i] ? start[i] : start[i] * (float)factor;
                }

                // Waves over the cap, in order (the purse of a wave depends on the leaks before it).
                for (int i = 0; i < trimmed.Length; i++)
                {
                    if (Evaluate(trimmed, withHero)[i].Pressure <= cap)
                    {
                        continue;
                    }

                    int wave = i;
                    float full = trimmed[i];
                    trimmed[i] = full * CapScale(scale =>
                    {
                        trimmed[wave] = full * scale;
                        return Evaluate(trimmed, withHero)[wave].Pressure;
                    }, cap);
                    capped[i] = true;
                }
            }

            // The last pass's rescale can lift a capped wave back over the cap through the creatures of the waves before it that
            // arrive with it (KillPurse.Carried): sweep the cap alone until every wave fits (round 22b; C1_04 Hard waves 3 and 6).
            for (int sweep = 0; sweep < CapSweeps; sweep++)
            {
                bool over = false;
                List<WaveLine> lines = Evaluate(trimmed, withHero);
                for (int i = 0; i < trimmed.Length; i++)
                {
                    if (lines[i].Pressure <= cap)
                    {
                        continue;
                    }

                    over = true;
                    int wave = i;
                    float full = trimmed[i];
                    trimmed[i] = full * CapScale(scale =>
                    {
                        trimmed[wave] = full * scale;
                        return Evaluate(trimmed, withHero)[wave].Pressure;
                    }, cap);
                    capped[i] = true;
                    lines = Evaluate(trimmed, withHero);
                }

                if (!over)
                {
                    break;
                }
            }

            for (int i = 0; i < trimmed.Length; i++)
            {
                // Rounded so the editor and the BalanceLab tool land on the same numbers.
                trimmed[i] = (float)Math.Round(trimmed[i], 3);
            }

            return trimmed;
        }

        // The largest scale of one wave's health (at most one) that brings its pressure under the cap. A wave's own health is
        // not the only thing in its pressure (the ledger re-chooses the defence, and slow creatures of earlier waves arrive with
        // it), so the pressure is not monotone near the bottom: the scale steps down until the wave fits, then a bisection
        // between the last two steps finds the edge; when no step fits, the step with the lowest pressure is kept.
        private static float CapScale(Func<float, float> pressure, float cap)
        {
            float above = 1f;
            float best = 1f;
            float lowest = float.MaxValue;
            for (float scale = CapStep; scale >= CalibrationMinScale; scale *= CapStep)
            {
                float value = pressure(scale);
                if (value <= cap)
                {
                    float below = scale;
                    for (int i = 0; i < CapRefinements; i++)
                    {
                        float mid = (above + below) * 0.5f;
                        if (pressure(mid) <= cap)
                        {
                            below = mid;
                        }
                        else
                        {
                            above = mid;
                        }
                    }

                    return below;
                }

                if (value < lowest)
                {
                    lowest = value;
                    best = scale;
                }

                above = scale;
            }

            return best;
        }

        // Pressure grows with health, so a bisection on the logarithm of a scale finds where it meets the target. Where the purse
        // breaks (leaks cost kill money) the pressure jumps; the bisection then ends on the jump, and the side closer to the
        // target is kept.
        private static double Bisect(Func<double, float> pressure, float target, double lo, double hi)
        {
            if (pressure(hi) <= target)
            {
                return Math.Exp(hi);
            }

            double floor = lo;
            double ceiling = hi;
            for (int i = 0; i < CalibrationIterations; i++)
            {
                double mid = (lo + hi) * 0.5;
                if (pressure(mid) < target)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            double chosen = Math.Abs(pressure(lo) - target) <= Math.Abs(pressure(hi) - target) ? lo : hi;
            float miss = Math.Abs(pressure(chosen) - target);
            if (miss > target * ScanTolerance)
            {
                // The mean is not monotone in the scale (the ledger re-chooses its guns per wave and the purse rounds to coins), so
                // the bisection can land on a jump (round 22c: Quartz Flats Easy came out 5% harder than its target). A scan of the
                // whole range finds the scale whose mean comes closest.
                for (int i = 0; i <= ScanSteps; i++)
                {
                    double scale = floor + (ceiling - floor) * i / ScanSteps;
                    float value = Math.Abs(pressure(scale) - target);
                    if (value < miss)
                    {
                        miss = value;
                        chosen = scale;
                    }
                }
            }

            return Math.Exp(chosen);
        }

        /// <summary>
        /// Research and power the existing levels of one continent pay per difficulty, against what the upgrades tagged to
        /// that continent cost. Dormant upgrades (no continent) count nowhere.
        /// </summary>
        public void MetaTotals(int continent, out int researchCost, out int powerCost, out int[] researchPerDifficulty, out int[] powerPerDifficulty)
        {
            researchCost = 0;
            powerCost = 0;
            foreach (UpgradeStats upgrade in Upgrades)
            {
                if (upgrade.Continent != continent)
                {
                    continue;
                }

                if (upgrade.PaidInPower)
                {
                    powerCost += upgrade.TotalCost;
                }
                else
                {
                    researchCost += upgrade.TotalCost;
                }
            }

            researchPerDifficulty = new int[Difficulties.Count];
            powerPerDifficulty = new int[Difficulties.Count];
            for (int d = 0; d < Difficulties.Count; d++)
            {
                foreach (CampaignLevel level in Levels)
                {
                    if (level.Continent != continent)
                    {
                        continue;
                    }

                    researchPerDifficulty[d] += (int)Math.Round(level.Research * Difficulties[d].Research, MidpointRounding.AwayFromZero);
                    powerPerDifficulty[d] += (int)Math.Round(level.Power * Difficulties[d].Power, MidpointRounding.AwayFromZero);
                }
            }
        }
    }
}
