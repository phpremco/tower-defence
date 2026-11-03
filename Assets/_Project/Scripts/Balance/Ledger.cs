using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>Permanent upgrades of one tower type, as multipliers.</summary>
    public struct TowerBoost
    {
        public float Damage;
        public float Rate;
        public float Range;

        public static TowerBoost None => new TowerBoost { Damage = 1f, Rate = 1f, Range = 1f };
    }

    /// <summary>Everything the ledger needs to know about one level as the player meets it.</summary>
    public sealed class LevelInput
    {
        public string Key = string.Empty;
        public int StartingMoney;
        public float HealthScale = 1f;
        public LevelShape Shape = new LevelShape();
        public int MineCount;
        public int MineStock;
        public int HarvesterCost;

        /// <summary>The guns the player owns at this point of the campaign and this level sells.</summary>
        public List<TowerStats> Towers = new List<TowerStats>();
        public Dictionary<string, TowerBoost> Boosts = new Dictionary<string, TowerBoost>();
        public HeroStats Hero;
        public float HeroDamage = 1f;

        /// <summary>Health of one gate with the permanent gate upgrades.</summary>
        public float GateHealth;

        public TowerBoost BoostOf(string towerId) => Boosts.TryGetValue(towerId, out TowerBoost boost) ? boost : TowerBoost.None;
    }

    /// <summary>One wave as the ledger sees it.</summary>
    public struct WaveLine
    {
        public int Wave;
        public bool Boss;
        public int Money;
        public int Towers;
        public float GroundHealth;
        public float AirHealth;
        public float Window;
        public float GroundPressure;
        public float AirPressure;
        public int KillMoney;

        /// <summary>The worse of ground and air: what the player must survive.</summary>
        public float Pressure => Math.Max(GroundPressure, AirPressure);

        public override string ToString() =>
            $"w{Wave,-2}{(Boss ? "B" : " ")} money {Money,5} towers {Towers,2} hp {GroundHealth,6:0}/{AirHealth,5:0} window {Window,4:0}s pressure {GroundPressure:0.00}/{AirPressure:0.00}";
    }

    /// <summary>
    /// A paper model of a level's balance, wave by wave. For every wave it works out the money the player can have had
    /// (start, the kills of earlier waves, the mines), the best defence that money buys from the towers the player owns
    /// (building and upgrading tower by tower), and how much of the wave's effective health that defence can deal with in
    /// the time it gets: the spawn span plus each tower's own time in range, the hold of the gates on the way, the hero.
    /// Splash counts by how dense the stream is, overkill wastes big hits on small creatures, a boss is a single target,
    /// and a second front or a nest that joins late only meets part of the defence. Pressure is health over capacity:
    /// below one the towers alone hold. A model, not the game: no placement, drones, path objects or boss powers.
    /// </summary>
    public static class Ledger
    {
        /// <summary>Every wave of a level on one difficulty, in order, with money carried from wave to wave.</summary>
        public static List<WaveLine> Evaluate(LevelInput level, IReadOnlyList<WaveSpec> waves, DifficultyStats difficulty, BalanceRules rules)
        {
            var lines = new List<WaveLine>(waves.Count);
            float kills = 0f;
            for (int i = 0; i < waves.Count; i++)
            {
                int money = MoneyAt(level, i, waves.Count, kills, rules);
                WaveLine line = EvaluateWave(level, waves[i], difficulty, money, rules);
                line.Wave = i + 1;
                lines.Add(line);
                // Creatures that get through pay nothing; a wave over pressure one leaks in proportion.
                kills += line.KillMoney * Math.Min(1f, line.Pressure > 0f ? 1f / line.Pressure : 1f);
            }

            return lines;
        }

        /// <summary>Money available before a wave: start, the kills so far and the mines, less one harvester.</summary>
        public static int MoneyAt(LevelInput level, int waveIndex, int waveCount, float kills, BalanceRules rules) =>
            level.StartingMoney + (int)kills + MineIncome(level, waveIndex, waveCount, rules);

        /// <summary>One wave against the defence a purse of money buys.</summary>
        public static WaveLine EvaluateWave(LevelInput level, WaveSpec wave, DifficultyStats difficulty, int money, BalanceRules rules)
        {
            WaveLoad load = WaveLoad.Of(level, wave, difficulty, rules);
            List<Option> options = Options(level, load, rules);
            Defence defence = Buy(options, load, money, Cap(level, rules));
            return new WaveLine
            {
                Boss = load.HasBoss,
                Money = money,
                Towers = defence.Towers,
                GroundHealth = load.StreamHealth + load.BossHealth,
                AirHealth = load.AirHealth,
                Window = load.Window,
                GroundPressure = load.GroundPressure(defence.Stream + HeroStream(level, load, rules), defence.Single + HeroSingle(level, load, rules)),
                AirPressure = load.AirPressure(defence.Air),
                KillMoney = load.KillMoney,
            };
        }

        private static int Cap(LevelInput level, BalanceRules rules) => rules.TowersPerFront * Math.Max(1, level.Shape.GroundFronts(rules.TrunkReference));

        private static int MineIncome(LevelInput level, int waveIndex, int waveCount, BalanceRules rules)
        {
            if (level.MineCount == 0 || waveIndex < rules.MinesOpenFromWave)
            {
                return 0;
            }

            float progress = Clamp01((waveIndex - rules.MinesOpenFromWave) / Math.Max(1f, waveCount * rules.MineDepletionShare));
            return Math.Max(0, (int)Math.Round(level.MineCount * level.MineStock * progress) - level.HarvesterCost);
        }

        // The blade at a gate and half the blaster for the lulls, over half the window.
        private static float HeroDps(LevelInput level)
        {
            if (level.Hero == null)
            {
                return 0f;
            }

            float blade = level.Hero.BladeInterval > 0f ? level.Hero.BladeDamage * level.HeroDamage / level.Hero.BladeInterval : 0f;
            float gun = level.Hero.GunInterval > 0f ? level.Hero.GunDamage / level.Hero.GunInterval * 0.5f : 0f;
            return blade + gun;
        }

        private static float HeroStream(LevelInput level, WaveLoad load, BalanceRules rules) => HeroDps(level) * load.Window * rules.HeroShare;

        private static float HeroSingle(LevelInput level, WaveLoad load, BalanceRules rules) => HeroDps(level) * load.BossWindow * rules.HeroShare;

        // ---- Buying ------------------------------------------------------------------------------------------------

        // One tower type at one level: what a single instance contributes against this wave.
        private sealed class Option
        {
            public int Tower;
            public int Level;
            public int Cost;
            public float Stream;
            public float Single;
            public float Air;
        }

        private struct Defence
        {
            public float Stream;
            public float Single;
            public float Air;
            public int Towers;
        }

        private static List<Option> Options(LevelInput level, WaveLoad load, BalanceRules rules)
        {
            var options = new List<Option>();
            for (int t = 0; t < level.Towers.Count; t++)
            {
                TowerStats tower = level.Towers[t];
                if (!tower.IsGun)
                {
                    continue;
                }

                TowerBoost boost = level.BoostOf(tower.Id);
                bool ground = Layer.Has(tower.Targets, Layer.Ground);
                bool air = Layer.Has(tower.Targets, Layer.Air);
                for (int l = 0; l < tower.Levels.Length; l++)
                {
                    TowerLevelStats stats = tower.Levels[l];
                    float damage = stats.Damage * boost.Damage;
                    float dps = stats.Interval > 0f ? damage * boost.Rate / stats.Interval : 0f;
                    float range = stats.Range * boost.Range;
                    options.Add(new Option
                    {
                        Tower = t,
                        Level = l,
                        Cost = stats.Cost,
                        Stream = ground ? dps * Overkill(damage, load.MeanHealth) * Splash(tower.Splash, load.GroundDensity, rules) * (load.Window + 2f * range / load.GroundSpeed) * rules.Utilisation : 0f,
                        Single = ground ? dps * (load.BossWindow + 2f * range / load.BossSpeed) * rules.Utilisation : 0f,
                        Air = air ? dps * Overkill(damage, load.MeanAirHealth) * Splash(tower.Splash, load.AirDensity, rules) * (load.AirWindow + 2f * range / load.AirSpeed) * rules.Utilisation : 0f,
                    });
                }
            }

            return options;
        }

        // A hit bigger than the creature is partly wasted.
        private static float Overkill(float damage, float meanHealth) => meanHealth <= 0f || damage <= meanHealth ? 1f : meanHealth / damage;

        // A splash hit catches more creatures the closer they walk: one plus those inside its diameter, capped.
        private static float Splash(float radius, float perUnit, BalanceRules rules) =>
            radius <= 0f ? 1f : Math.Min(rules.MaxSplashTargets, 1f + 2f * radius * perUnit);

        // Greedy purchase, step by step: build a new tower or raise one already built by a level, whichever lowers the
        // wave's worse pressure most per coin. Raising needs a tower at the level below, so steps are never bought alone.
        private static Defence Buy(List<Option> options, WaveLoad load, int money, int cap)
        {
            var built = new Dictionary<(int, int), int>();
            var defence = new Defence();
            while (true)
            {
                float current = Worst(load, defence);
                Option best = null;
                float bestScore = 0f;
                foreach (Option option in options)
                {
                    int cost = option.Cost;
                    if (cost <= 0 || cost > money)
                    {
                        continue;
                    }

                    bool build = option.Level == 0;
                    if (build ? defence.Towers >= cap : !built.TryGetValue((option.Tower, option.Level - 1), out int below) || below <= 0)
                    {
                        continue;
                    }

                    Defence next = Add(defence, options, option);
                    float score = Score(load, defence, next, current) / cost;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = option;
                    }
                }

                if (best == null)
                {
                    return defence;
                }

                money -= best.Cost;
                defence = Add(defence, options, best);
                built[(best.Tower, best.Level)] = (built.TryGetValue((best.Tower, best.Level), out int count) ? count : 0) + 1;
                if (best.Level > 0)
                {
                    built[(best.Tower, best.Level - 1)]--;
                }
                else
                {
                    defence.Towers++;
                }
            }
        }

        private static Defence Add(Defence defence, List<Option> options, Option option)
        {
            Option below = option.Level > 0 ? Find(options, option.Tower, option.Level - 1) : null;
            defence.Stream += option.Stream - (below?.Stream ?? 0f);
            defence.Single += option.Single - (below?.Single ?? 0f);
            defence.Air += option.Air - (below?.Air ?? 0f);
            return defence;
        }

        private static Option Find(List<Option> options, int tower, int level)
        {
            foreach (Option option in options)
            {
                if (option.Tower == tower && option.Level == level)
                {
                    return option;
                }
            }

            return null;
        }

        private static float Worst(WaveLoad load, Defence defence) =>
            Math.Max(load.GroundPressure(defence.Stream, defence.Single), load.AirPressure(defence.Air));

        // How much a step helps: the drop of the worse pressure; while a side has no defence at all, its new capacity.
        private static float Score(WaveLoad load, Defence before, Defence after, float current)
        {
            if (float.IsInfinity(current))
            {
                bool groundOpen = float.IsInfinity(load.GroundPressure(before.Stream, before.Single));
                bool airOpen = float.IsInfinity(load.AirPressure(before.Air));
                float gain = 0f;
                if (groundOpen)
                {
                    gain += (after.Stream - before.Stream) + (after.Single - before.Single);
                }

                if (airOpen)
                {
                    gain += after.Air - before.Air;
                }

                return gain;
            }

            float next = Worst(load, after);
            if (next < current)
            {
                return current - next;
            }

            // The bottleneck did not move (the other side is worse): still prefer steps that help some side.
            float ground = load.GroundPressure(before.Stream, before.Single) - load.GroundPressure(after.Stream, after.Single);
            float air = load.AirPressure(before.Air) - load.AirPressure(after.Air);
            return Math.Max(0f, Math.Max(ground, air)) * 0.01f;
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
