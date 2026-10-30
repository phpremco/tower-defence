using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the waves. Every level has a script of its own: how many waves, which creatures join and
    /// from which wave, the rhythm of wave kinds (plain, rush, heavy, ambush), which waves turn into air raids per
    /// difficulty, and on which waves a boss walks in. A threat budget that grows wave by wave sets the size; the difficulty profile scales the
    /// counts and health on top, and adds air waves and escorts.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const float FirstWaveDelay = 10f;
        private const float WaveDelay = 8f;
        private const float BossDelaySeconds = 14f;
        private const float EscortDelaySeconds = 5f;
        private const float AmbushDelaySeconds = 12f;
        private const float TroopStagger = 2.5f;
        private const float ThreatPerHealth = 1f / 20f;
        private const float AirThreatFactor = 1.2f;
        private const float ArmorThreatShare = 0.8f;
        private const float BaseDamageThreat = 0.04f;
        private const float AttackDamageThreat = 0.03f;
        private const float RushBudget = 1.2f;
        // Late waves lean on the tougher creatures instead of ever more fodder: the weight of a troop is raised by its
        // threat to a power that grows from zero in wave 1 to this in the last wave.
        private const float HeavyBias = 0.6f;
        // A troop with a share worth at least this much of one creature still sends one, so a new kind shows up on time.
        private const float MinShare = 0.35f;
        private const int MaxGroupCount = 64;
        private const float RushTempo = 0.6f;
        private const float HeavyBudget = 1.1f;
        private const float HeavyShare = 0.55f;
        private const float HeavyTempo = 1.4f;
        private const float AirShare = 0.35f;
        private const float AirGroundShare = 0.4f;
        // Share of an ambush wave that surfaces from pools and nests, per difficulty: easy keeps most of it at the gate.
        private static readonly float[] AmbushSharePerDifficulty = { 0.3f, 0.45f, 0.55f, 0.65f };
        private const float BossWaveShare = 0.6f;
        private const float SprinkleShare = 0.15f;
        private const float FlyerInterval = 1.6f;

        private const int Road = 0;
        private const int NorthAirRoute = 1;
        private const int SouthAirRoute = 2;

        // Flyers that sweep in behind a boss, per difficulty: the harder the level, the busier the sky while it walks.
        private static readonly int[] BossEscortFlyers = { 0, 1, 2, 4 };

        private enum WaveKind
        {
            /// <summary>A mix of everything unlocked, by weight.</summary>
            Plain = 0,

            /// <summary>Many of the cheapest creatures, close together.</summary>
            Rush = 1,

            /// <summary>The toughest creature takes most of the budget, spaced out, with a little fodder.</summary>
            Heavy = 2,

            /// <summary>Flyers on both flight routes over a thinned ground wave.</summary>
            Air = 3,

            /// <summary>Pool creatures surface as one group in the middle of the wave.</summary>
            Ambush = 4,
        }

        /// <summary>One creature of a level's roster: when it joins, how much of the budget it takes and how it arrives.</summary>
        private sealed class Troop
        {
            public EnemyDefinition Enemy;
            public int FirstWave;
            public float Weight;
            public float Interval;
            public SpawnSource Source = SpawnSource.Gate;
            public float Delay;

            /// <summary>Index of the easiest difficulty this troop joins on; easy may be spared a species.</summary>
            public int MinDifficulty;

            /// <summary>
            /// Creatures that never come alone: the stream is cut into groups of about this many, and a wave that
            /// sends the species at all sends at least a whole group. Zero keeps the mixed singles-and-threes rhythm.
            /// </summary>
            public int Pack;
        }

        /// <summary>A boss and the wave it walks in on.</summary>
        private struct BossWave
        {
            public int Wave;
            public EnemyDefinition Enemy;
        }

        private sealed class WaveScript
        {
            /// <summary>How many waves the level has; levels differ on purpose.</summary>
            public int Waves;

            /// <summary>Threat budget of the first and of the last wave; one point is twenty health. The curve between is geometric.</summary>
            public float Budget;
            public float FinalBudget;

            /// <summary>
            /// Extra health per wave, as a share of the base (0.03 means wave 11 has 1.3 times the health of wave 1).
            /// The budget buys the numbers, this keeps every creature growing while the player's towers do.
            /// </summary>
            public float HealthGrowth;

            public Troop[] Troops;

            /// <summary>The creature of the air raids, and of the escorts behind the last boss.</summary>
            public EnemyDefinition Flyer;

            /// <summary>Wave numbers (from 1) that become air raids; a difficulty takes the first few of them.</summary>
            public int[] AirWaves;

            /// <summary>How many of the air waves each difficulty gets.</summary>
            public int[] AirWavesPerDifficulty;

            /// <summary>From this difficulty index on, plain waves after the first air raid also carry a few flyers.</summary>
            public int AirSprinkleFrom;

            /// <summary>One letter per wave: P plain, R rush, H heavy, M ambush. A shorter string repeats.</summary>
            public string Rhythm;

            /// <summary>The bosses in wave order; the last one gets the escorts.</summary>
            public BossWave[] Bosses;

            public float Growth => Mathf.Pow(FinalBudget / Budget, 1f / Mathf.Max(1, Waves - 1));
        }

        private static BossWave Boss(int wave, EnemyDefinition enemy) => new BossWave { Wave = wave, Enemy = enemy };

        private static Troop Join(EnemyDefinition enemy, int firstWave, float weight, float interval, SpawnSource source = SpawnSource.Gate, float delay = 0f, int minDifficulty = 0, int pack = 0) =>
            new Troop { Enemy = enemy, FirstWave = firstWave, Weight = weight, Interval = interval, Source = source, Delay = delay, MinDifficulty = minDifficulty, Pack = pack };

        // The landing zone teaches the game: ant crowds first, then the pack hunters, scorpions from wave 6, and no sky
        // at all on easy; from medium on tide bats rise out of the lake halfway through.
        private static WaveScript LandingScript(Bestiary b) => new WaveScript
        {
            Waves = 24,
            Budget = 7f,
            FinalBudget = 170f,
            HealthGrowth = 0.03f,
            Troops = new[] { Join(b.Skitter, 1, 3f, 0.45f, pack: 6), Join(b.Prowler, 3, 2.2f, 1.2f, pack: 3), Join(b.Stinger, 6, 1.2f, 3f), Join(b.Tidebat, 8, 1.4f, 1.6f, SpawnSource.Nest, 6f, 1) },
            Flyer = b.Skyray,
            AirWaves = new[] { 9, 15, 20, 23 },
            AirWavesPerDifficulty = new[] { 0, 1, 2, 3 },
            AirSprinkleFrom = 3,
            Rhythm = "PPPRPPHPMPPRHPMPHPRPMRHP",
            Bosses = new[] { Boss(12, b.BroodMother), Boss(24, b.PackAlpha) },
        };

        // The woods are about speed and the sky: rollers rush the roads, packs hunt, moss and tide bats pour out of
        // the trees and ponds, and the slugs grease the way for everyone else.
        private static WaveScript ForestScript(Bestiary b) => new WaveScript
        {
            Waves = 30,
            Budget = 9f,
            FinalBudget = 230f,
            HealthGrowth = 0.025f,
            Troops = new[]
            {
                Join(b.Skitter, 1, 2f, 0.45f, pack: 6), Join(b.Prowler, 1, 2.5f, 1.1f, pack: 3), Join(b.Roller, 2, 2.5f, 1.6f),
                Join(b.Mossbat, 3, 2f, 1.5f, SpawnSource.Nest, 8f), Join(b.Tidebat, 5, 1.2f, 1.5f, SpawnSource.Nest, 10f),
                Join(b.Oozer, 7, 1.5f, 2.4f), Join(b.Stinger, 10, 1.2f, 3f),
            },
            Flyer = b.Skyray,
            AirWaves = new[] { 7, 13, 18, 23, 27 },
            AirWavesPerDifficulty = new[] { 1, 2, 3, 5 },
            AirSprinkleFrom = 2,
            Rhythm = "PRPMRPMRPPRMRPMRHRMPRMHRPMRHPR",
            Bosses = new[] { Boss(15, b.RollerKing), Boss(30, b.StormMatriarch) },
        };

        // The lava fields erupt: ember bats burst from the vents from the first wave, wisps drift down the roads,
        // scorpions and shield bearers walk the heat.
        private static WaveScript LavaScript(Bestiary b) => new WaveScript
        {
            Waves = 36,
            Budget = 7f,
            FinalBudget = 300f,
            HealthGrowth = 0.02f,
            Troops = new[]
            {
                Join(b.Prowler, 1, 3f, 1.1f, pack: 3), Join(b.Emberbat, 1, 2.5f, 1.5f, SpawnSource.Nest, 6f), Join(b.Stinger, 4, 1.5f, 2.8f),
                Join(b.Skitter, 3, 1.5f, 0.45f, pack: 6), Join(b.Wisp, 4, 2f, 1.3f), Join(b.Roller, 5, 2f, 1.6f), Join(b.Bulwark, 10, 1.2f, 3.2f),
            },
            Flyer = b.Skyray,
            AirWaves = new[] { 5, 9, 14, 19, 26, 32 },
            AirWavesPerDifficulty = new[] { 2, 3, 5, 6 },
            AirSprinkleFrom = 1,
            Rhythm = "PMPHMPMHPPMHMPMHMPHPMHPMHMPHMPMHPMHP",
            Bosses = new[] { Boss(12, b.CoreWisp), Boss(24, b.AcidEmpress), Boss(36, b.CrystalWarlord) },
        };

        // The ruins are a siege: shield bearers in convoys, slugs that grease the roads, dust bats out of the sand
        // and tide bats from the cistern, and rays over the walls.
        private static WaveScript RuinsScript(Bestiary b) => new WaveScript
        {
            Waves = 40,
            Budget = 10f,
            FinalBudget = 350f,
            HealthGrowth = 0.018f,
            Troops = new[]
            {
                Join(b.Bulwark, 9, 2f, 2.8f), Join(b.Dustbat, 1, 2.5f, 1.4f, SpawnSource.Nest, 6f), Join(b.Oozer, 1, 1.5f, 2.2f),
                Join(b.Stinger, 6, 1.5f, 2.6f), Join(b.Skitter, 4, 1.5f, 0.45f, pack: 6), Join(b.Prowler, 5, 2f, 1.1f, pack: 3),
                Join(b.Tidebat, 8, 1.2f, 1.4f, SpawnSource.Nest, 9f), Join(b.Wisp, 9, 1.5f, 1.3f),
            },
            Flyer = b.Skyray,
            AirWaves = new[] { 6, 10, 16, 21, 26, 31, 37 },
            AirWavesPerDifficulty = new[] { 2, 3, 5, 7 },
            AirSprinkleFrom = 2,
            Rhythm = "PPMHPHMHPHMPHMHPHMHPHPMHPHMHPHMPHMHPPMHP",
            Bosses = new[] { Boss(14, b.SlimeMonarch), Boss(27, b.SkySovereign), Boss(40, b.CrystalWarlord) },
        };

        // The nest throws everything, in a steady rotation of rush, ambush and heavy, and ends with the queen.
        private static WaveScript NestScript(Bestiary b) => new WaveScript
        {
            Waves = 48,
            Budget = 10f,
            FinalBudget = 450f,
            HealthGrowth = 0.015f,
            Troops = new[]
            {
                Join(b.Skitter, 1, 2f, 0.4f, pack: 6), Join(b.Prowler, 1, 3f, 1f, pack: 3), Join(b.Roller, 1, 2.5f, 1.5f), Join(b.Oozer, 2, 1.5f, 2f),
                Join(b.Tidebat, 2, 2f, 1.3f, SpawnSource.Nest, 7f), Join(b.Dustbat, 3, 1.5f, 1.4f, SpawnSource.Nest, 10f),
                Join(b.Stinger, 3, 1.5f, 2.4f), Join(b.Wisp, 4, 2f, 1.2f), Join(b.Bulwark, 7, 1.5f, 2.6f),
            },
            Flyer = b.Skyray,
            AirWaves = new[] { 4, 7, 10, 14, 17, 21, 26, 31, 37, 43 },
            AirWavesPerDifficulty = new[] { 3, 5, 7, 10 },
            AirSprinkleFrom = 0,
            Rhythm = "RMHRMHRMHP",
            Bosses = new[] { Boss(12, b.PackAlpha), Boss(24, b.AcidEmpress), Boss(36, b.CoreWisp), Boss(48, b.Queen) },
        };

        /// <summary>Writes the waves of a level for every difficulty and returns them in order.</summary>
        private static List<DifficultyWaves> BuildWaves(string levelKey, DifficultyProfile[] difficulties, WaveScript script, int secondRoad)
        {
            var sets = new List<DifficultyWaves>();
            for (int d = 0; d < difficulties.Length; d++)
            {
                var waves = new WaveDefinition[script.Waves];
                for (int i = 0; i < script.Waves; i++)
                {
                    int number = i + 1;
                    var random = new System.Random(levelKey.GetHashCode() * 31 + d * 131 + i);
                    List<SpawnGroup> groups = ComposeWave(script, d, number, secondRoad, random);

                    var wave = LoadOrCreate<WaveDefinition>($"{DataRoot}/Waves/{levelKey}_{DifficultyNames[d]}_{number}.asset");
                    var so = new SerializedObject(wave);
                    so.FindProperty("_startDelay").floatValue = i == 0 ? FirstWaveDelay : WaveDelay;
                    so.FindProperty("_healthMultiplier").floatValue = 1f + script.HealthGrowth * i;
                    WriteGroups(so.FindProperty("_groups"), groups);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    waves[i] = wave;
                }

                sets.Add(new DifficultyWaves { Difficulty = difficulties[d], Waves = waves });
            }

            return sets;
        }

        private static void WriteGroups(SerializedProperty array, List<SpawnGroup> groups)
        {
            array.arraySize = groups.Count;
            for (int g = 0; g < groups.Count; g++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(g);
                element.FindPropertyRelative("Enemy").objectReferenceValue = groups[g].Enemy;
                element.FindPropertyRelative("Count").intValue = groups[g].Count;
                element.FindPropertyRelative("Interval").floatValue = groups[g].Interval;
                element.FindPropertyRelative("Delay").floatValue = groups[g].Delay;
                element.FindPropertyRelative("Stagger").floatValue = groups[g].Stagger;
                element.FindPropertyRelative("PathIndex").intValue = groups[g].PathIndex;
                element.FindPropertyRelative("Source").enumValueIndex = (int)groups[g].Source;
            }
        }

        // One wave: its kind decides how the budget is spent; a boss wave keeps the ground thin so the boss is the event.
        private static List<SpawnGroup> ComposeWave(WaveScript script, int difficulty, int number, int secondRoad, System.Random random)
        {
            var result = new List<SpawnGroup>();
            float budget = script.Budget * Mathf.Pow(script.Growth, number - 1);
            int bossIndex = BossIndex(script, number);
            bool boss = bossIndex >= 0;
            WaveKind kind = IsAirWave(script, difficulty, number) ? WaveKind.Air : KindOf(script.Rhythm[(number - 1) % script.Rhythm.Length]);
            if (boss)
            {
                budget *= BossWaveShare;
            }

            List<Troop> open = Unlocked(script.Troops, number, difficulty, false);
            List<Troop> pools = Unlocked(script.Troops, number, difficulty, true);
            switch (kind)
            {
                case WaveKind.Rush:
                    Fill(result, budget * RushBudget, Cheapest(open, 2), RushTempo, secondRoad, random, script, number);
                    break;
                case WaveKind.Heavy:
                    Troop heavy = Toughest(open);
                    Fill(result, budget * HeavyShare, new List<Troop> { heavy }, HeavyTempo, secondRoad, random, script, number);
                    Fill(result, budget * (1f - HeavyShare), Without(open, heavy), 1f, secondRoad, random, script, number);
                    break;
                case WaveKind.Air:
                    AddFlyers(result, script.Flyer, budget * AirShare, 0f);
                    Fill(result, budget * AirGroundShare, open, 1f, secondRoad, random, script, number);
                    break;
                case WaveKind.Ambush when pools.Count > 0:
                    float ambush = AmbushSharePerDifficulty[Mathf.Min(difficulty, AmbushSharePerDifficulty.Length - 1)];
                    Fill(result, budget * (1f - ambush), open, 1f, secondRoad, random, script, number);
                    Fill(result, budget * ambush, pools, 1f, secondRoad, random, script, number, AmbushDelaySeconds);
                    break;
                default:
                    Fill(result, budget, open, 1f, secondRoad, random, script, number);
                    break;
            }

            // On the harder difficulties the sky stays contested between the air raids.
            if (kind == WaveKind.Plain && difficulty >= script.AirSprinkleFrom && number > FirstAirWave(script, difficulty))
            {
                AddFlyers(result, script.Flyer, budget * SprinkleShare, TroopStagger * 2f);
            }

            if (boss)
            {
                result.Add(new SpawnGroup { Enemy = script.Bosses[bossIndex].Enemy, Count = 1, Interval = 1f, Delay = BossDelaySeconds, PathIndex = Road });
                if (bossIndex == script.Bosses.Length - 1 && BossEscortFlyers[difficulty] > 0)
                {
                    result.Add(new SpawnGroup { Enemy = script.Flyer, Count = BossEscortFlyers[difficulty], Interval = 1.5f, Delay = BossDelaySeconds + EscortDelaySeconds, PathIndex = NorthAirRoute });
                }
            }

            return result;
        }

        private static int BossIndex(WaveScript script, int number)
        {
            for (int i = 0; i < script.Bosses.Length; i++)
            {
                if (script.Bosses[i].Wave == number)
                {
                    return i;
                }
            }

            return -1;
        }

        private static WaveKind KindOf(char letter)
        {
            switch (letter)
            {
                case 'R':
                    return WaveKind.Rush;
                case 'H':
                    return WaveKind.Heavy;
                case 'M':
                    return WaveKind.Ambush;
                default:
                    return WaveKind.Plain;
            }
        }

        private static bool IsAirWave(WaveScript script, int difficulty, int number)
        {
            int allowed = Mathf.Min(script.AirWavesPerDifficulty[difficulty], script.AirWaves.Length);
            for (int i = 0; i < allowed; i++)
            {
                if (script.AirWaves[i] == number)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FirstAirWave(WaveScript script, int difficulty) =>
            script.AirWavesPerDifficulty[difficulty] > 0 && script.AirWaves.Length > 0 ? script.AirWaves[0] : int.MaxValue;

        private static List<Troop> Unlocked(Troop[] troops, int number, int difficulty, bool pools)
        {
            var list = new List<Troop>();
            foreach (Troop troop in troops)
            {
                if (troop.FirstWave <= number && troop.MinDifficulty <= difficulty && (troop.Source != SpawnSource.Gate) == pools)
                {
                    list.Add(troop);
                }
            }

            return list;
        }

        // Armour counts for most of its hit points, a flyer for a little more, and a creature that hurts structures
        // or the base for more still, so a glass cannon is not priced as fodder.
        private static float Threat(EnemyDefinition enemy) =>
            (enemy.MaxHealth + enemy.Armor * ArmorThreatShare) * ThreatPerHealth
            * (enemy.Flies ? AirThreatFactor : 1f)
            * (1f + enemy.BaseDamage * BaseDamageThreat + enemy.AttackDamage * AttackDamageThreat);

        private static List<Troop> Cheapest(List<Troop> troops, int count)
        {
            var sorted = new List<Troop>(troops);
            sorted.Sort((a, b) => Threat(a.Enemy).CompareTo(Threat(b.Enemy)));
            return sorted.GetRange(0, Mathf.Min(count, sorted.Count));
        }

        private static Troop Toughest(List<Troop> troops)
        {
            Troop best = troops[0];
            foreach (Troop troop in troops)
            {
                if (Threat(troop.Enemy) > Threat(best.Enemy))
                {
                    best = troop;
                }
            }

            return best;
        }

        private static List<Troop> Without(List<Troop> troops, Troop excluded)
        {
            var list = new List<Troop>(troops);
            list.Remove(excluded);
            return list.Count > 0 ? list : troops;
        }

        // Spends a budget over troops by weight, the weight leaning towards tougher creatures as the level goes on: each
        // troop gets as many creatures as its share buys, capped so no wave turns into a flood. Gate troops are cut into
        // clusters over the lanes; pool troops surface as one group.
        private static void Fill(List<SpawnGroup> output, float budget, List<Troop> troops, float tempo, int secondRoad, System.Random random, WaveScript script, int number, float extraDelay = 0f)
        {
            if (troops.Count == 0 || budget <= 0f)
            {
                return;
            }

            float bias = HeavyBias * (number - 1) / Mathf.Max(1, script.Waves - 1);
            float totalWeight = 0f;
            foreach (Troop troop in troops)
            {
                totalWeight += Weight(troop, bias);
            }

            int order = 0;
            foreach (Troop troop in troops)
            {
                float threat = Threat(troop.Enemy);
                float share = budget * Weight(troop, bias) / totalWeight;
                int count = Mathf.Min(MaxGroupCount, Mathf.Max(share >= threat * MinShare ? 1 : 0, Mathf.RoundToInt(share / threat)));
                // A pack species waits until the budget buys at least half a pack, then always sends a whole one.
                if (count > 0 && troop.Pack > 0 && count < troop.Pack)
                {
                    count = count * 2 >= troop.Pack ? troop.Pack : 0;
                }

                if (count <= 0)
                {
                    continue;
                }

                var group = new SpawnGroup
                {
                    Enemy = troop.Enemy,
                    Count = count,
                    Interval = troop.Interval * tempo,
                    Delay = troop.Delay + extraDelay + order * TroopStagger,
                    Source = troop.Source,
                    PathIndex = Road,
                };
                if (troop.Source == SpawnSource.Gate)
                {
                    Scatter(group, random.Next(0, 1000), LanesFor(group, secondRoad), output, troop.Pack);
                }
                else
                {
                    output.Add(group);
                }

                order++;
            }
        }

        private static float Weight(Troop troop, float bias) => troop.Weight * Mathf.Pow(Threat(troop.Enemy), bias);

        // Flyers split over both flight routes, the second route a little behind the first.
        private static void AddFlyers(List<SpawnGroup> output, EnemyDefinition flyer, float budget, float delay)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(budget / Threat(flyer)));
            int north = (count + 1) / 2;
            output.Add(new SpawnGroup { Enemy = flyer, Count = north, Interval = FlyerInterval, Delay = delay, PathIndex = NorthAirRoute });
            if (count - north > 0)
            {
                output.Add(new SpawnGroup { Enemy = flyer, Count = count - north, Interval = FlyerInterval, Delay = delay + TroopStagger, PathIndex = SouthAirRoute });
            }
        }

        // Cluster sizes a stream is cut into. Mixing singles with groups of three or four breaks up the
        // one-after-another line; several patterns keep neighbouring streams from marching in step.
        private static readonly int[][] ClusterPatterns =
        {
            new[] { 1, 2, 1, 3 },
            new[] { 2, 1, 3 },
            new[] { 1, 1, 2, 4 },
            new[] { 3, 1, 2 },
        };

        private const float ClusterInnerIntervalShare = 0.5f;
        private const float ClusterMinInterval = 0.2f;
        private const float PackInnerIntervalShare = 0.35f;

        // The routes a stream may alternate between: both ground entrances, or both flight routes.
        private static int[] LanesFor(SpawnGroup group, int secondRoad)
        {
            if (group.PathIndex == NorthAirRoute || group.PathIndex == SouthAirRoute)
            {
                return new[] { NorthAirRoute, SouthAirRoute };
            }

            bool ground = !group.Enemy.Flies;
            return ground && secondRoad >= 0 ? new[] { Road, secondRoad } : new[] { group.PathIndex };
        }

        // Cuts one stream into clusters that take turns on the lanes. The average rate stays the same:
        // a cluster of n enemies claims n times the stream interval before the next one starts.
        private static void Scatter(SpawnGroup group, int seed, int[] lanes, List<SpawnGroup> output, int pack = 0)
        {
            // A pack species comes in groups of its pack size (give or take), close on each other's heels.
            int[] pattern = pack > 0 ? new[] { pack, pack + 1, pack, pack + 2 } : ClusterPatterns[seed % ClusterPatterns.Length];
            float inner = Mathf.Max(ClusterMinInterval, group.Interval * (pack > 0 ? PackInnerIntervalShare : ClusterInnerIntervalShare));
            int remaining = group.Count;
            int step = 0;
            float offset = 0f;

            while (remaining > 0)
            {
                int size = Mathf.Min(pattern[step % pattern.Length], remaining);
                output.Add(new SpawnGroup
                {
                    Enemy = group.Enemy,
                    Count = size,
                    Interval = inner,
                    Delay = group.Delay,
                    Source = group.Source,
                    Stagger = offset,
                    PathIndex = lanes[(seed + step) % lanes.Length],
                });

                offset += size * group.Interval;
                remaining -= size;
                step++;
            }
        }
    }
}
