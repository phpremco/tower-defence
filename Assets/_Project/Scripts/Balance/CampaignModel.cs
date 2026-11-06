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
        private const float CapStep = 0.92f;
        private const int CapRefinements = 8;

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

        public LevelInput InputFor(int levelIndex, int difficulty)
        {
            LevelFacts facts = Facts[levelIndex];
            PlayerState player = PlayerAt(levelIndex, difficulty);
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
            int medium = Math.Min(WaveSolver.SolveDifficulty, Difficulties.Count - 1);
            healths = WaveSolver.Solve(script, Enemies, ReferenceInputFor(levelIndex, medium), Difficulties[medium], facts.SecondRoad, Rules);
            var sets = new List<List<WaveSpec>>();
            for (int d = 0; d < Difficulties.Count; d++)
            {
                float[] own = d == medium ? healths : WaveSolver.SolveVariant(script, Enemies, ReferenceInputFor(levelIndex, d), Difficulties[d], healths, facts.SecondRoad, Rules);
                if (Rules.CalibrateToHuman)
                {
                    own = CalibrateToHuman(levelIndex, script, d, own, facts.SecondRoad);
                }

                var waves = new List<WaveSpec>();
                for (int i = 0; i < script.Waves; i++)
                {
                    waves.Add(WaveComposer.Compose(script, Enemies, d, i + 1, own[i], facts.SecondRoad, Rules));
                }

                sets.Add(waves);
            }

            return sets;
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
                    waves.Add(WaveComposer.Compose(script, Enemies, difficulty, i + 1, health[i], secondRoad, Rules));
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

            return Math.Abs(pressure(lo) - target) <= Math.Abs(pressure(hi) - target) ? Math.Exp(lo) : Math.Exp(hi);
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
