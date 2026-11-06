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
        /// <summary>Starting money on this difficulty (the level's own times the difficulty's share).</summary>
        public int StartingMoney;

        /// <summary>What a bounty or a load of ore really pays on this difficulty, as a share (<see cref="DifficultyStats.Income"/>).</summary>
        public float Income = 1f;
        public float HealthScale = 1f;
        public LevelShape Shape = new LevelShape();
        public int MineCount;
        public int MineStock;

        /// <summary>False when this level offers no harvest drone yet: the ore stays in the ground.</summary>
        public bool Harvest = true;
        public int HarvesterCost;

        /// <summary>The guns the player owns at this point of the campaign and this level sells.</summary>
        public List<TowerStats> Towers = new List<TowerStats>();
        public Dictionary<string, TowerBoost> Boosts = new Dictionary<string, TowerBoost>();
        public HeroStats Hero;
        public float HeroDamage = 1f;

        /// <summary>The hero perks the player has unlocked by this level.</summary>
        public HashSet<string> HeroPerks = new HashSet<string>();

        /// <summary>Health of one gate with the permanent gate upgrades.</summary>
        public float GateHealth;

        /// <summary>The gadgets this level sells (mines, slow fields) and the drones it offers, for the plan model.</summary>
        public List<GadgetStats> Gadgets = new List<GadgetStats>();
        public List<DroneStats> Drones = new List<DroneStats>();

        /// <summary>The map's routes and build spots, for placement; null makes every tower count as ideally placed.</summary>
        public LevelGeometry Geometry;

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
        public static List<WaveLine> Evaluate(LevelInput level, IReadOnlyList<WaveSpec> waves, DifficultyStats difficulty, BalanceRules rules) =>
            Evaluate(level, waves, difficulty, rules, null);

        /// <summary>
        /// As <see cref="Evaluate(LevelInput, IReadOnlyList{WaveSpec}, DifficultyStats, BalanceRules)"/>; <paramref name="loads"/>,
        /// when given, receives each wave's load with the carried health of earlier waves folded in, for
        /// <see cref="PressureOf"/>.
        /// </summary>
        public static List<WaveLine> Evaluate(LevelInput level, IReadOnlyList<WaveSpec> waves, DifficultyStats difficulty, BalanceRules rules, List<WaveLoad> loads)
        {
            var lines = new List<WaveLine>(waves.Count);
            var purse = new KillPurse(level, difficulty, rules);
            var held = new AirHoldings();
            for (int i = 0; i < waves.Count; i++)
            {
                purse.Begin(waves[i]);
                var next = new AirHoldings();
                WaveLoad load = LoadOf(level, waves[i], difficulty, rules, purse.Carried(waves[i]));
                loads?.Add(load);
                WaveLine line = EvaluateLoad(level, load, purse.Money(i, waves.Count), rules, held, next);
                held = next;
                line.Wave = i + 1;
                lines.Add(line);
                purse.Add(waves[i], line);
            }

            return lines;
        }

        /// <summary>Money available before a wave: start, the kills paid so far (see <see cref="KillPurse"/>) and the mines, less one harvester.</summary>
        public static int MoneyAt(LevelInput level, int waveIndex, int waveCount, float kills, BalanceRules rules) =>
            level.StartingMoney + (int)kills + MineIncome(level, waveIndex, waveCount, rules);

        /// <summary>One wave against the defence a purse of money buys.</summary>
        public static WaveLine EvaluateWave(LevelInput level, WaveSpec wave, DifficultyStats difficulty, int money, BalanceRules rules) =>
            EvaluateWave(level, wave, difficulty, money, rules, null, null);

        /// <summary>
        /// One wave against the defence a purse buys, on top of the air guns bought for earlier waves (<paramref name="held"/>:
        /// they stay on the map and their price is gone from the purse). <paramref name="next"/>, when given, receives the air
        /// guns standing after this wave. <paramref name="carried"/> is the ground health of earlier waves that reaches the
        /// defence at the same time (<see cref="KillPurse.Carried"/>); the towers share their window with it.
        /// </summary>
        public static WaveLine EvaluateWave(LevelInput level, WaveSpec wave, DifficultyStats difficulty, int money, BalanceRules rules, AirHoldings held, AirHoldings next, float carried = 0f) =>
            EvaluateLoad(level, LoadOf(level, wave, difficulty, rules, carried), money, rules, held, next);

        /// <summary>
        /// Ground and air pressure of one wave against towers that already stand (tower id and level index), with the hero,
        /// on the same scale as <see cref="WaveLine"/>: below one they hold. The PlayMode probe bot judges its own defence
        /// with it the way the ledger judges the expected player's. Towers the level input does not sell count for nothing.
        /// </summary>
        public static (float Ground, float Air) PressureOf(LevelInput level, WaveLoad load, BalanceRules rules, IEnumerable<(string TowerId, int Level)> standing)
        {
            List<Option> options = Options(level, load, rules);
            var defence = new Defence();
            foreach ((string towerId, int towerLevel) in standing)
            {
                int index = level.Towers.FindIndex(t => t.Id == towerId);
                Option option = index >= 0 ? Find(options, index, towerLevel) : null;
                if (option != null)
                {
                    defence.Stream += option.Stream;
                    defence.Single += option.Single;
                    defence.Air += option.Air;
                }
            }

            return (load.GroundPressure(defence.Stream + HeroStream(level, load, rules), defence.Single + HeroSingle(level, load, rules)), load.AirPressure(defence.Air));
        }

        internal static WaveLoad LoadOf(LevelInput level, WaveSpec wave, DifficultyStats difficulty, BalanceRules rules, float carried)
        {
            WaveLoad load = WaveLoad.Of(level, wave, difficulty, rules);
            load.StreamHealth += carried * rules.CarriedShare;
            return load;
        }

        private static WaveLine EvaluateLoad(LevelInput level, WaveLoad load, int money, BalanceRules rules, AirHoldings held, AirHoldings next)
        {
            List<Option> options = Options(level, load, rules);
            Defence defence = Buy(options, load, money - (held?.Cost ?? 0), Cap(level, rules), held, next);
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

        // Only a level that offers a harvest drone mines its nodes (drones arrive one kind a level since 2026-11-05).
        private static int MineIncome(LevelInput level, int waveIndex, int waveCount, BalanceRules rules)
        {
            if (!level.Harvest || level.MineCount == 0 || waveIndex < rules.MinesOpenFromWave)
            {
                return 0;
            }

            float progress = Clamp01((waveIndex - rules.MinesOpenFromWave) / Math.Max(1f, waveCount * rules.MineDepletionShare));
            return Math.Max(0, (int)Math.Round(level.MineCount * level.MineStock * progress * level.Income) - level.HarvesterCost);
        }

        // The blade at a gate and half the blaster for the lulls, over half the window.
        private static float HeroDps(LevelInput level)
        {
            if (level.Hero == null)
            {
                return 0f;
            }

            float blade = level.Hero.BladeInterval > 0f ? level.Hero.BladeDamage * level.HeroDamage / level.Hero.BladeInterval : 0f;
            float interval = level.Hero.EffectiveGunInterval(level.HeroPerks);
            float gun = interval > 0f ? level.Hero.GunDamage / interval * 0.5f : 0f;
            return blade + gun;
        }

        internal static float HeroStream(LevelInput level, WaveLoad load, BalanceRules rules) => HeroDps(level) * load.Window * rules.HeroShare;

        internal static float HeroSingle(LevelInput level, WaveLoad load, BalanceRules rules) => HeroDps(level) * load.BossSeconds(load.BossWindow, rules) * rules.HeroShare;

        // ---- Buying ------------------------------------------------------------------------------------------------

        /// <summary>
        /// The air guns standing from earlier waves. A gun that only aims at the sky does nothing for a ground wave, and the
        /// player cannot sell it back at full price, so its cost stays out of the purse for the rest of the level. Without
        /// this the ledger re-spent the Flak money on the ground every wave after a raid (Crysium Vein: about a quarter of
        /// the purse at wave 9). Ground guns are still re-chosen every wave: they all serve the ground.
        /// </summary>
        public sealed class AirHoldings
        {
            internal readonly Dictionary<(int Tower, int Level), int> Steps = new Dictionary<(int, int), int>();

            /// <summary>What the standing air guns cost, every step up to their level.</summary>
            public int Cost { get; private set; }

            internal void Keep(Dictionary<(int, int), int> built, List<Option> options)
            {
                foreach (KeyValuePair<(int Tower, int Level), int> step in built)
                {
                    Option option = Find(options, step.Key.Tower, step.Key.Level);
                    if (option == null || !option.AirOnly || step.Value <= 0)
                    {
                        continue;
                    }

                    Steps[step.Key] = step.Value;
                    for (int level = 0; level <= step.Key.Level; level++)
                    {
                        Cost += (Find(options, step.Key.Tower, level)?.Cost ?? 0) * step.Value;
                    }
                }
            }
        }

        /// <summary>One tower type at one level: what a single instance contributes against this wave (hit points it can take).</summary>
        public sealed class Option
        {
            public int Tower;
            public int Level;
            public int Cost;
            public bool AirOnly;
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

        /// <summary>
        /// What one tower at one level contributes against a wave from a spot that covers <paramref name="cover"/>: hit points
        /// of the stream, of the bosses and of the flyers it can take in the time it gets. The time is the spawn span plus its
        /// own pass (route in reach over the creatures' speed) and, when the gate's queue is in reach, the gate's hold, where a
        /// splash gun's blast meets its cap. <paramref name="efficiency"/> below zero takes the tower's calibrated value.
        /// </summary>
        public static Option Capacity(LevelInput level, WaveLoad load, BalanceRules rules, int towerIndex, int towerLevel, SpotCover cover, float efficiency = -1f)
        {
            TowerStats tower = level.Towers[towerIndex];
            TowerLevelStats stats = tower.Levels[towerLevel];
            TowerBoost boost = level.BoostOf(tower.Id);
            bool ground = Layer.Has(tower.Targets, Layer.Ground);
            bool air = Layer.Has(tower.Targets, Layer.Air);
            float damage = stats.Damage * boost.Damage;
            float dps = stats.Interval > 0f ? damage * boost.Rate / stats.Interval : 0f;
            // A gun's layer share and the creatures' plating both work per hit, so they scale its damage per layer; the tower's
            // calibrated in-play efficiency scales everything it deals.
            float groundHit = damage * tower.GroundFactor;
            float airHit = damage * tower.AirFactor;
            float field = dps * (efficiency >= 0f ? efficiency : tower.Efficiency) * rules.Utilisation;
            float stream = field * tower.GroundFactor * load.StreamPlates.Share(groundHit);
            float single = field * tower.GroundFactor * load.BossPlates.Share(groundHit);
            float sky = field * tower.AirFactor * load.AirPlates.Share(airHit);
            float cap = SplashCap(tower, rules);
            float pass = cover.GroundPass / Math.Max(0.01f, load.GroundSpeed);
            float queue = cover.Gate ? load.GateSeconds : 0f;
            float bossQueue = cover.Gate ? load.BossWindow : 0f;
            return new Option
            {
                Tower = towerIndex,
                Level = towerLevel,
                Cost = stats.Cost,
                AirOnly = air && !ground,
                Stream = ground ? stream * Overkill(groundHit, load.MeanHealth) * ((load.Span + pass) * Splash(tower.Splash, load.GroundDensity, cap) + queue * (tower.Splash > 0f ? cap : 1f)) : 0f,
                Single = ground ? single * load.BossSeconds(bossQueue + cover.GroundPass / Math.Max(0.01f, load.BossSpeed), rules) : 0f,
                Air = air ? sky * Overkill(airHit, load.MeanAirHealth) * Splash(tower.Splash, load.AirDensity, cap) * (load.AirWindow + cover.AirPass / Math.Max(0.01f, load.AirSpeed)) : 0f,
            };
        }

        /// <summary>Spots a competent player has for one tower type: the ledger's typical cover is their mean (LevelGeometry.TypicalCover).</summary>
        public const int TypicalSpots = 6;

        /// <summary>Every tower and level the player owns, each from a typical good spot of its map (ideal without a map): the purse model buys from these.</summary>
        internal static List<Option> Options(LevelInput level, WaveLoad load, BalanceRules rules)
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
                for (int l = 0; l < tower.Levels.Length; l++)
                {
                    float range = tower.Levels[l].Range * boost.Range;
                    bool airOnly = Layer.Has(tower.Targets, Layer.Air) && !Layer.Has(tower.Targets, Layer.Ground);
                    SpotCover cover = level.Geometry != null ? level.Geometry.TypicalCover(range, airOnly, tower.Splash > 0f, TypicalSpots) : SpotCover.Ideal(range);
                    options.Add(Capacity(level, load, rules, t, l, cover));
                }
            }

            return options;
        }

        /// <summary>Most creatures one of this tower's blasts hurts: its own cap, else the rules'.</summary>
        public static float SplashCap(TowerStats tower, BalanceRules rules) => tower.SplashTargets > 0 ? tower.SplashTargets : rules.MaxSplashTargets;

        // A hit bigger than the creature is partly wasted.
        public static float Overkill(float damage, float meanHealth) => meanHealth <= 0f || damage <= meanHealth ? 1f : meanHealth / damage;

        // A splash hit catches more creatures the closer they walk: one plus those inside its diameter, capped.
        public static float Splash(float radius, float perUnit, float cap) =>
            radius <= 0f ? 1f : Math.Min(cap, 1f + 2f * radius * perUnit);

        // Greedy purchase, step by step: build a new tower or raise one already built by a level, whichever lowers the
        // wave's worse pressure most per coin. Raising needs a tower at the level below, so steps are never bought alone.
        private static Defence Buy(List<Option> options, WaveLoad load, int money, int cap, AirHoldings held, AirHoldings after)
        {
            var built = new Dictionary<(int, int), int>();
            var defence = new Defence();
            if (held != null)
            {
                foreach (KeyValuePair<(int Tower, int Level), int> step in held.Steps)
                {
                    Option owned = Find(options, step.Key.Tower, step.Key.Level);
                    if (owned == null)
                    {
                        continue;
                    }

                    built[step.Key] = step.Value;
                    defence.Stream += owned.Stream * step.Value;
                    defence.Single += owned.Single * step.Value;
                    defence.Air += owned.Air * step.Value;
                    defence.Towers += step.Value;
                }
            }

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
                    after?.Keep(built, options);
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
