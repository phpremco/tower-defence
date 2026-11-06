using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>A tower the roadmap plans to hand out before a continent finale: owned from a level of a continent on.</summary>
    public readonly struct PlannedTower
    {
        public PlannedTower(string id, int continent, int level)
        {
            Id = id;
            Continent = continent;
            Level = level;
        }

        public string Id { get; }

        /// <summary>Index of the continent (0 = Crystal Plains).</summary>
        public int Continent { get; }

        /// <summary>1-based level of that continent from which the player owns it (the level before rewards it).</summary>
        public int Level { get; }
    }

    /// <summary>How one continent finale with its boss plays for a competent player of one difficulty (Docs/BALANCE.md).</summary>
    public struct FinaleLine
    {
        public int Difficulty;

        /// <summary>The factor on every wave's health that puts the plain waves on the finale's human target.</summary>
        public float PlainScale;

        /// <summary>Plain-wave pressure for the towers alone (on the target) and with the hero.</summary>
        public float PlainTowers;
        public float PlainHero;

        /// <summary>The boss wave's pressure for the towers alone and with the hero, and the difficulty's hero cap.</summary>
        public float BossTowers;
        public float BossHero;
        public float Cap;

        /// <summary>
        /// The boss on its own with the hero (its escorts left out): what its health and mechanic ask. The solver softens a
        /// boss wave over the cap as a whole, so the escorts can always give way; the boss itself cannot.
        /// </summary>
        public float BossAlone;

        /// <summary>Money the player can have spent by the boss wave.</summary>
        public int Money;

        /// <summary>Share of the boss fight its mechanic silences.</summary>
        public float MechanicShare;
        public int ResearchEarned;
        public int Towers;

        /// <summary>True when the boss alone asks more than the cap: no softening of its escorts makes it killable (too hard).</summary>
        public bool TooHard;

        /// <summary>True when the boss alone asks less than a plain wave: an anticlimax (too easy).</summary>
        public bool TooEasy;

        public string Verdict => TooHard ? "too hard" : TooEasy ? "too easy" : "good";
    }

    /// <summary>
    /// The continent finales as they will be played, before their levels exist (only Crystal Plains 1 to 6 do). Every finale is
    /// level 20 of its continent: the player owns what the roadmap hands out by then, has spent the research a player of that
    /// difficulty earned over the continent, and meets the finale's waves trimmed to the level-20 human curve. The stand-in field
    /// is the last real level with its own waves; its last boss wave carries the continent boss. Because every wave of a level
    /// shares the solver's health factor, the boss is judged against its own escorts and plain waves, which holds for any
    /// continent: later continents raise creatures and firepower together.
    /// </summary>
    public static class FinaleModel
    {
        private const int Iterations = 24;
        private const double MinLogScale = -3.0;
        private const double MaxLogScale = 3.0;

        /// <summary>
        /// The campaign extended to the end of continent 0 (twenty levels): the real levels, then planned ones on the stand-in
        /// field that sell the real last level's towers plus every planned tower, pay the continent's research curve and hand
        /// out the planned towers the level before they are owned.
        /// </summary>
        public static CampaignModel Planned(CampaignModel real, IReadOnlyList<PlannedTower> plan, IReadOnlyDictionary<string, TowerStats> plannedStats, int researchBudget)
        {
            var model = new CampaignModel { Rules = real.Rules, Hero = real.Hero, HarvesterCost = real.HarvesterCost };
            model.StartingTowers.AddRange(real.StartingTowers);
            model.Upgrades.AddRange(real.Upgrades);
            model.Difficulties.AddRange(real.Difficulties);
            foreach (KeyValuePair<string, TowerStats> tower in real.Towers)
            {
                model.Towers[tower.Key] = tower.Value;
            }

            foreach (KeyValuePair<string, EnemyStats> enemy in real.Enemies)
            {
                model.Enemies[enemy.Key] = enemy.Value;
            }

            foreach (KeyValuePair<string, TowerStats> tower in plannedStats)
            {
                model.Towers[tower.Key] = tower.Value;
            }

            int levels = real.Rules.LevelsPerContinent;
            CampaignLevel last = real.Levels[real.Levels.Count - 1];
            LevelFacts standIn = real.Facts[real.Facts.Count - 1];
            for (int i = 0; i < levels; i++)
            {
                if (i < real.Levels.Count)
                {
                    model.Levels.Add(real.Levels[i]);
                    model.Facts.Add(real.Facts[i]);
                    continue;
                }

                var level = new CampaignLevel
                {
                    Key = $"C1_{i + 1:00} (planned)",
                    Continent = last.Continent,
                    Research = ContinentEconomy.Research(researchBudget, i, real.Rules),
                };
                level.TowerIds.AddRange(last.TowerIds);
                model.Levels.Add(level);
                model.Facts.Add(standIn);
            }

            foreach (PlannedTower tower in plan)
            {
                if (tower.Continent != last.Continent)
                {
                    continue;
                }

                int owned = Math.Max(1, Math.Min(levels - 1, tower.Level - 1));
                for (int i = Math.Max(real.Levels.Count, owned); i < levels; i++)
                {
                    if (!model.Levels[i].TowerIds.Contains(tower.Id))
                    {
                        model.Levels[i].TowerIds.Add(tower.Id);
                    }
                }

                if (owned - 1 >= real.Levels.Count && string.IsNullOrEmpty(model.Levels[owned - 1].RewardTowerId))
                {
                    model.Levels[owned - 1].RewardTowerId = tower.Id;
                }
            }

            return model;
        }

        /// <summary>
        /// Judges the finale (the planned model's last level) on one difficulty. <paramref name="waves"/> are the stand-in's
        /// waves with the boss in wave <paramref name="finale"/>; <paramref name="extraTowers"/> are guns of later continents the
        /// player owns by this finale, counted without permanent upgrades (their continents' research is not budgeted yet).
        /// </summary>
        public static FinaleLine Judge(CampaignModel planned, IReadOnlyList<WaveSpec> waves, int finale, int difficulty, IEnumerable<TowerStats> extraTowers, List<WaveLoad> loads = null)
        {
            int levelIndex = planned.Levels.Count - 1;
            BalanceRules rules = planned.Rules;
            LevelInput input = planned.InputFor(levelIndex, difficulty);
            // The finale is a stand-in field, not the map whose waves it borrows: the measured field ease of that map stays out.
            input.FieldEase = 1f;
            foreach (TowerStats tower in extraTowers)
            {
                if (!input.Towers.Exists(t => t.Id == tower.Id))
                {
                    input.Towers.Add(tower);
                    input.Boosts[tower.Id] = TowerBoost.None;
                }
            }

            DifficultyStats stats = planned.Difficulties[difficulty];
            BalanceRules human = rules.HumanTowersOnly();
            BalanceRules withHero = rules.HumanWithHero();
            float target = planned.HumanTarget(levelIndex, difficulty);
            double scale = Bisect(log => BalanceChecks.PlainMean(Ledger.Evaluate(input, Scaled(waves, Math.Exp(log)), stats, human)), target);
            List<WaveSpec> trimmed = Scaled(waves, scale);
            List<WaveLine> towers = Ledger.Evaluate(input, trimmed, stats, human);
            var heroLoads = loads ?? new List<WaveLoad>();
            heroLoads.Clear();
            List<WaveLine> hero = Ledger.Evaluate(input, trimmed, stats, withHero, heroLoads);
            PlayerState player = planned.PlayerAt(levelIndex, difficulty);
            var line = new FinaleLine
            {
                Difficulty = difficulty,
                PlainScale = (float)scale,
                PlainTowers = BalanceChecks.PlainMean(towers),
                PlainHero = BalanceChecks.PlainMean(hero),
                BossTowers = towers[finale].Pressure,
                BossHero = hero[finale].Pressure,
                Cap = rules.HeroCapFor(difficulty),
                Money = hero[finale].Money,
                MechanicShare = heroLoads[finale].BossMechanicShare,
                ResearchEarned = player.ResearchEarned,
                Towers = input.Towers.Count,
            };
            line.BossAlone = Ledger.Evaluate(input, Alone(trimmed, finale), stats, withHero)[finale].Pressure;
            line.TooHard = line.BossAlone > line.Cap * (1f + rules.BeatableTolerance);
            line.TooEasy = line.BossAlone < line.PlainHero;
            return line;
        }

        // The same waves with the boss wave reduced to its boss.
        private static List<WaveSpec> Alone(List<WaveSpec> waves, int finale)
        {
            var alone = new List<WaveSpec>(waves);
            WaveSpec boss = waves[finale];
            var only = new WaveSpec { StartDelay = boss.StartDelay, HealthMultiplier = boss.HealthMultiplier };
            only.Groups.AddRange(boss.Groups.FindAll(g => g.Enemy.IsBoss));
            alone[finale] = only;
            return alone;
        }

        private static List<WaveSpec> Scaled(IReadOnlyList<WaveSpec> waves, double scale)
        {
            var scaled = new List<WaveSpec>(waves.Count);
            foreach (WaveSpec wave in waves)
            {
                var copy = new WaveSpec { StartDelay = wave.StartDelay, HealthMultiplier = (float)(wave.HealthMultiplier * scale) };
                copy.Groups.AddRange(wave.Groups);
                scaled.Add(copy);
            }

            return scaled;
        }

        // The plain mean grows with health; a bisection on the log of the scale finds where it meets the target.
        private static double Bisect(Func<double, float> pressure, float target)
        {
            double lo = MinLogScale;
            double hi = MaxLogScale;
            for (int i = 0; i < Iterations; i++)
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

            return Math.Exp(Math.Abs(pressure(lo) - target) <= Math.Abs(pressure(hi) - target) ? lo : hi);
        }
    }
}
