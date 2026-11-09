using Game.Balance;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Round 29: a continent boss's timed powers in the ledger. A ward costs the field a share of the boss's walk; an escort
    /// call adds a stream that joins mid-road (WaveLoad.SummonStartShare) and keeps coming for the walk.
    /// </summary>
    public sealed class BossPowerLedgerTests
    {
        private const float MainLength = 100f;
        private const float BossSpeed = 0.5f;

        [Test]
        public void Ward_AddsItsShareOfTheWalk_ToTheMechanicLoss()
        {
            WaveLoad plain = Load(Boss(ward: 0f, summonsPerSecond: 0f));
            WaveLoad warded = Load(Boss(ward: 0.25f, summonsPerSecond: 0f));
            float walk = MainLength / BossSpeed;
            Assert.AreEqual(plain.BossMechanicLoss + 0.25f * walk, warded.BossMechanicLoss, 0.01f);
            Assert.AreEqual(plain.BossHealth, warded.BossHealth, 0.01f, "a ward changes no health");
        }

        [Test]
        public void EscortCall_AddsAMidRoadStream_ForTheWalk()
        {
            WaveLoad plain = Load(Boss(ward: 0f, summonsPerSecond: 0f));
            WaveLoad escorted = Load(Boss(ward: 0f, summonsPerSecond: 0.1f));
            float walk = MainLength / BossSpeed;
            int expected = (int)System.Math.Ceiling(0.1f * walk);
            Assert.AreEqual(0, plain.GroundCount);
            Assert.AreEqual(expected, escorted.GroundCount);
            Assert.AreEqual(expected * 30f, escorted.StreamHealth, 0.01f);
            Assert.AreEqual(plain.BossHealth, escorted.BossHealth, 0.01f, "escorts never count as the boss");
            WaveGroup escorts = escorted.Groups.Find(g => !g.IsBoss);
            Assert.IsNotNull(escorts);
            Assert.AreEqual(WaveLoad.SummonStartShare, escorts.StartShare, 0.001f);
        }

        private static EnemyStats Boss(float ward, float summonsPerSecond) => new EnemyStats
        {
            Id = "Titan",
            Health = 2000f,
            Speed = BossSpeed,
            IsBoss = true,
            IsContinentBoss = true,
            BlockadeDamage = 100f,
            AttackInterval = 1f,
            WardShare = ward,
            SummonsPerSecond = summonsPerSecond,
            SummonHealth = 30f,
            SummonSpeed = 1.5f,
            SummonBlockadeDamage = 4f,
            SummonAttackInterval = 1f,
        };

        private static WaveLoad Load(EnemyStats boss)
        {
            var level = new LevelInput();
            level.Shape.Lanes.Add(new LevelShape.Lane { PathIndex = 0, Length = MainLength, TrunkShare = 1f });
            var wave = new WaveSpec();
            wave.Groups.Add(new GroupSpec { Enemy = boss, Count = 1 });
            return WaveLoad.Of(level, wave, new DifficultyStats(), BalanceRules.Default);
        }
    }
}
