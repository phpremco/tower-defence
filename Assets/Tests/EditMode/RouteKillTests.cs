using System.Collections.Generic;
using Game.Balance;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// The route-sampled kill model (round 27): a gun is credited for the creatures' time in its reach, not for the whole spawn
    /// span; a gun behind the defence gate kills nothing on the walk and meets only what gets through; a dense stream shares a
    /// gun's fire; kills accumulate along the road so two half-kills make a kill; the siege resolves the walk from the guards'
    /// gun list and the per-type account adds up to what was killed.
    /// </summary>
    public sealed class RouteKillTests
    {
        private const int RoadLength = 40;
        private const int GateAt = 30;

        private static readonly TowerStats Gun = new TowerStats
        {
            Id = "Gun",
            Targets = Layer.Ground,
            Levels = new[] { new TowerLevelStats(50, 10f, 2.5f, 0.5f) },
        };

        private static LevelInput Level()
        {
            var waypoints = new[] { new Cell(0, 5), new Cell(RoadLength, 5) };
            var roads = new List<RoadInput> { new RoadInput { PathIndex = 0, Waypoints = waypoints } };
            var gate = new Cell(GateAt, 5);
            var spots = new List<Cell>();
            for (int x = 0; x <= RoadLength; x++)
            {
                spots.Add(new Cell(x, 7));
            }

            var level = new LevelInput { BaseHealth = 600f, GateHealth = 500f, Towers = new List<TowerStats> { Gun } };
            level.Geometry = LevelGeometry.Build(roads, new List<RoadInput>(), new List<Cell> { gate }, gate, spots, 1f);
            level.Shape.Gates.Add(new LevelShape.Gate { OnTrunk = true, Progress = GateAt / (float)RoadLength });
            level.Shape.Lanes.Add(new LevelShape.Lane { PathIndex = 0, Length = RoadLength, TrunkShare = 1f });
            return level;
        }

        // A wave of identical walkers: count, body health, speed (cells a second) and the seconds they take to spawn.
        private static WaveLoad Wave(int count, float health, float speed, float span)
        {
            var load = new WaveLoad
            {
                StreamHealth = count * health,
                GroundCount = count,
                MeanHealth = health,
                GroundSpeed = speed,
                Span = span,
                Window = span,
                GroundStrike = 2f,
            };
            load.Groups.Add(new WaveGroup { Count = count, Health = health, Speed = speed, Path = 0 });
            return load;
        }

        private static BalanceRules Rules()
        {
            BalanceRules rules = BalanceRules.Default.HumanWithHero();
            rules.Utilisation = 1f;
            return rules;
        }

        private static List<RouteGun> Guns(params int[] xs)
        {
            var guns = new List<RouteGun>();
            foreach (int x in xs)
            {
                guns.Add(new RouteGun { Tower = Gun, Level = 0, Spot = new Cell(x, 7), Efficiency = 1f, Slow = 1f });
            }

            return guns;
        }

        [Test]
        public void SparseStream_IsCreditedForExposure_NotForTheSpawnSpan()
        {
            // 10 creatures of 100 health, one every 10 s, at 1 cell/s: a gun with 3 road cells in reach sees each creature 3 s and
            // deals 20 dps x 3 s = 60 per creature, 600 in all; the spawn-span credit would have been 20 x (90 + 3) = 1860.
            LevelInput level = Level();
            RouteResult result = RouteKill.Walk(level, Wave(10, 100f, 1f, 90f), Rules(), Guns(10));
            Assert.AreEqual(600f, result.WalkStream, 1f);
            Assert.AreEqual(600f, result.Dealt["Gun"], 1f, "the account is what was killed");
            Assert.AreEqual(0f, result.BehindStream);
        }

        [Test]
        public void DenseStream_SharesTheGun()
        {
            // The same 10 creatures in 10 s: one arrives a second, each spends 3 s in reach, so three share the gun at any time and
            // each gets 20 dps x 1 s = 20; the gun deals 200 in all (its 10 s of firing).
            LevelInput level = Level();
            RouteResult result = RouteKill.Walk(level, Wave(10, 100f, 1f, 10f), Rules(), Guns(10));
            Assert.AreEqual(200f, result.WalkStream, 1f);
        }

        [Test]
        public void KillsAccumulateAlongTheRoad_AndNeverExceedTheHealth()
        {
            // Two guns of 60 per creature each on a 100-health creature: the first takes 60, the second the 40 that is left.
            LevelInput level = Level();
            RouteResult result = RouteKill.Walk(level, Wave(10, 100f, 1f, 90f), Rules(), Guns(10, 20));
            Assert.AreEqual(1000f, result.WalkStream, 1f, "the whole wave dies on the walk");
            Assert.AreEqual(1000f, result.Dealt["Gun"], 1f);
        }

        [Test]
        public void GunBehindTheGate_KillsNothingOnTheWalk_ButMeetsWhatGetsThrough()
        {
            LevelInput level = Level();
            RouteResult result = RouteKill.Walk(level, Wave(10, 100f, 1f, 90f), Rules(), Guns(36));
            Assert.AreEqual(0f, result.WalkStream, "the gate holds the walkers before its reach");
            Assert.AreEqual(600f, result.BehindStream, 1f, "behind the gate it meets what got past");
        }

        // Round 31: a gun guards the queue only when it reaches where the held walkers stand (1.4 cells before the gate and the cell
        // behind that), not when the gate cell is within reach plus three cells. Glass Meadows Easy stood its towers past the gate.
        [Test]
        public void QueueGuard_ReachesWhereTheHeldWalkersStand_NotTheGateCell()
        {
            LevelInput level = Level();
            float reach = Gun.Levels[0].Range;
            Assert.IsTrue(level.Geometry.CoverAt(new Cell(GateAt - 2, 7), reach).Gate, "a gun beside the queue guards it");
            Assert.IsFalse(level.Geometry.CoverAt(new Cell(GateAt + 4, 7), reach).Gate, "a gun past the gate within reach plus three cells of the gate cell does not reach the queue");
            Assert.IsFalse(level.Geometry.CoverAt(new Cell(GateAt + 1, 7), reach).Gate, "a gun just downstream of the gate does not reach the stand point either");
            IReadOnlyList<(float X, float Y)> points = level.Geometry.QueuePoints();
            Assert.AreEqual(2, points.Count);
            Assert.AreEqual(GateAt - LevelGeometry.StandDistance, points[0].X, 0.01f, "the stop point lies 1.4 cells before the gate");
            Assert.AreEqual(GateAt - 2, points[1].X, 0.01f, "the queue bunches on the cell behind it");
        }

        [Test]
        public void Rank_PrefersTheApproachAndTheQueue_OverTheRoadBehindTheGate()
        {
            LevelInput level = Level();
            List<Cell> ranked = level.Geometry.Rank(Gun.Levels[0].Range, false, false);
            Assert.That(ranked[0].X, Is.InRange(GateAt - 4, GateAt - 1), "the best spot stands at the queue");
            int firstBehind = ranked.FindIndex(cell => cell.X > GateAt + 2);
            int lastApproach = ranked.FindLastIndex(cell => cell.X < GateAt - 2);
            Assert.Less(lastApproach, firstBehind, "every approach spot ranks above every spot whose cover lies behind the gate");
        }

        // Round 31: a nest that joins the road behind the defence gate never meets its queue (Crystal Plains' rift nest at 17 % behind
        // the gate at 13 % walked a creature to the base past the plans' towers at the gate). The game draws a nest per creature, so a
        // nest group walks from every nest in equal shares.
        [Test]
        public void NestBehindTheGate_PassesTheQueue_AndMeetsTheOtherGates()
        {
            LevelInput level = Level();
            level.Shape.NestRemaining.Add(0.2f);
            WaveLoad load = Wave(10, 100f, 1f, 90f);
            load.Groups[0].Source = 4;
            RouteResult alone = RouteKill.Walk(level, load, Rules(), Guns(36));
            Assert.Greater(alone.WalkStream, 0f, $"a gun past the join point fires on the walk (walk {alone.WalkStream} passed {alone.PassedStream} behind {alone.BehindStream} single {alone.WalkSingle} air {alone.WalkAir})");
            Assert.AreEqual(1000f, alone.WalkStream + alone.PassedStream, 1f, "what the gun did not kill passes the gate alive");
            Assert.AreEqual(0f, alone.BehindStream, "nothing is walked twice");

            var guards = new SiegeGuards();
            Leak.AddTower(ref guards, level, load, Rules(), 0, 0, level.Geometry.CoverAt(new Cell(28, 7), Gun.Levels[0].Range), 1f, 1f);
            SiegeState state = SiegeState.Start(level, 2);
            SiegeLine line = Leak.Resolve(level, load, Rules(), ref guards, state, 100f);
            Assert.AreEqual(500f, state.DefenceGate, "the queue guard at the defence gate never sees the nest's creatures");
            Assert.AreEqual(1000f, line.GroundLeak, 1f, "the passed health is leak all the same");
            Assert.Less(state.RestGates + state.BaseLeft, 500f + 600f, "it hammers the other gate and the base instead");

            level.Shape.NestRemaining.Add(0.9f);
            RouteResult split = RouteKill.Walk(level, load, Rules(), new List<RouteGun>());
            Assert.AreEqual(500f, split.PassedStream, 1f, "half the nest group joins at the nest behind the gate, half before it");
        }

        [Test]
        public void Siege_WalksTheGunsFromTheGuardList()
        {
            LevelInput level = Level();
            WaveLoad load = Wave(10, 100f, 1f, 90f);
            BalanceRules rules = Rules();
            var guards = new SiegeGuards();
            Leak.AddTower(ref guards, level, load, rules, 0, 0, level.Geometry.CoverAt(new Cell(10, 7), Gun.Levels[0].Range), 1f, 1f);
            Assert.IsNotNull(guards.Guns, "a level with routes defers the walk to the siege");
            Assert.AreEqual(0f, guards.WalkStream, "nothing is credited before the walk");

            SiegeState state = SiegeState.Start(level, 1);
            SiegeLine line = Leak.Resolve(level, load, rules, ref guards, state, 100f);
            Assert.AreEqual(600f, guards.WalkStream, 1f, "the route model's kills land in the guards");
            Assert.AreEqual(400f, line.GroundLeak, 1f, "what is alive at the gate is the leak");
            Assert.AreEqual(600f, guards.Dealt["Gun"], 1f);
        }

        [Test]
        public void LoadOf_CarriesTheGroups()
        {
            var level = new LevelInput { Income = 1f };
            var wave = new WaveSpec();
            wave.Groups.Add(new GroupSpec { Enemy = new EnemyStats { Id = "Ant", Health = 40f, Armor = 10f, Speed = 1.5f, Reward = 2, BlockadeDamage = 2f }, Count = 6, Interval = 1f });
            WaveLoad load = WaveLoad.Of(level, wave, new DifficultyStats(), BalanceRules.Default);
            Assert.AreEqual(1, load.Groups.Count);
            Assert.AreEqual(6, load.Groups[0].Count);
            Assert.AreEqual(40f, load.Groups[0].Health, 0.001f);
            Assert.AreEqual(10f, load.Groups[0].Armour, 0.001f);
            Assert.AreEqual(50f / (40f + 10f / 0.2f), load.Groups[0].Share(0.2f), 0.001f, "light fire pays for the armour five times over");
        }
    }
}
