using Game.Balance;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Round 21a, roles by enemy: flat per-hit plating (every hit loses the plating's points, at least the floor share gets
    /// through) in the game's Health and in the balance model, and the per-layer damage share of a tower (the Gun Turret does
    /// half damage to air-only creatures). Docs/BALANCE.md, "Roles by enemy".
    /// </summary>
    public sealed class PlatingTests
    {
        private const float Floor = 0.2f;

        [Test]
        public void Plating_TakesItsPointsOffEveryHit_ButLetsTheFloorThrough()
        {
            var health = new Health(100f, 0f, 0f, 4f, Floor);
            health.TakeDamage(25f);
            Assert.AreEqual(79f, health.Current, 0.001f, "a shell loses 4 of 25");
            health.TakeDamage(5f);
            Assert.AreEqual(78f, health.Current, 0.001f, "a light pulse loses 4 of 5, and 1 is the floor share");
            health.TakeDamage(3f);
            Assert.AreEqual(77.4f, health.Current, 0.001f, "below the plating only the floor share lands");
        }

        [Test]
        public void Plating_NeverWearsOff_UnlikeThePoolArmour()
        {
            var health = new Health(1000f, 0f, 0f, 8f, Floor);
            for (int i = 0; i < 50; i++)
            {
                health.TakeDamage(10f);
            }

            Assert.AreEqual(900f, health.Current, 0.001f, "fifty hits of 10 through 8 plating land 2 each, the last as the first");
        }

        [Test]
        public void ContinuousDamage_PassesThePlating()
        {
            var health = new Health(100f, 0f, 0f, 8f, Floor);
            health.TakeDamage(0.5f, true);
            Assert.AreEqual(99.5f, health.Current, 0.001f, "a lava ball's burn a frame has no hit for the plates to stop");
        }

        [Test]
        public void PoolArmour_StillWorksBehindThePlating()
        {
            var health = new Health(100f, 30f, 0.5f, 4f, Floor);
            health.TakeDamage(24f);
            Assert.AreEqual(20f, health.Armor, 0.001f, "plating first (24 -> 20), then the pool soaks half of that");
            Assert.AreEqual(90f, health.Current, 0.001f);
        }

        [Test]
        public void TheBalanceModel_UsesTheGamesFormula()
        {
            float[] hits = { 0.5f, 1f, 3f, 5f, 8f, 9f, 12f, 15f, 25f, 48f, 85f, 120f };
            float[] platings = { 0f, 2f, 4f, 8f, 12f, 20f };
            foreach (float hit in hits)
            {
                foreach (float plating in platings)
                {
                    Assert.AreEqual(Health.ThroughPlating(hit, plating, Floor), Plating.Through(hit, plating, Floor), 0.0001f, $"hit {hit} plating {plating}");
                }
            }
        }

        [Test]
        public void PlateMix_WeighsThePlatingByHealth()
        {
            var mix = new PlateMix();
            mix.Add(0f, Floor, 300f);
            mix.Add(4f, Floor, 100f);
            Assert.AreEqual(1f - 0.25f * 0.8f, mix.Share(5f), 0.0001f, "a quarter of the health is plated; a 5 hit keeps 1/5 of it there");
            Assert.AreEqual(1f - 0.25f * 0.16f, mix.Share(25f), 0.0001f, "a 25 hit loses 4 of 25 on the plated quarter");
            Assert.AreEqual(1f, new PlateMix().Share(5f), 0.0001f, "a soft wave lets every hit through");
        }

        [Test]
        public void Ledger_RanksTheCannonAboveTheGun_AgainstPlatedCreatures()
        {
            var gun = new TowerStats { Id = "Gun", Targets = Layer.Ground, Levels = new[] { new TowerLevelStats(50, 5f, 3.5f, 0.35f) } };
            var cannon = new TowerStats { Id = "Cannon", Targets = Layer.Ground, Levels = new[] { new TowerLevelStats(100, 25f, 4f, 1.4f) } };
            float soft = PerCoin(cannon, 0f) / PerCoin(gun, 0f);
            float plated = PerCoin(cannon, 4f) / PerCoin(gun, 4f);
            Assert.Greater(plated, soft * 3f, $"cannon over gun per coin: soft {soft:0.00}, plated {plated:0.00}");
        }

        [Test]
        public void GunTurret_DoesHalfDamageToAirOnlyCreatures()
        {
            var gun = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/PulseRifle.asset");
            var flak = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Rocket.asset");
            Assert.IsNotNull(gun, "Run Tools/Generate/Scenes and Starter Data first.");
            Assert.AreEqual(0.5f, gun.DamageFactorAgainst(TargetLayers.Air), 0.0001f);
            Assert.AreEqual(1f, gun.DamageFactorAgainst(TargetLayers.Ground), 0.0001f);
            Assert.AreEqual(1f, gun.DamageFactorAgainst(TargetLayers.Ground | TargetLayers.Air), 0.0001f, "a wisp walks the road: the ground share");
            Assert.AreEqual(1f, flak.DamageFactorAgainst(TargetLayers.Air), 0.0001f, "the Flak is the air gun");
        }

        [Test]
        public void Cragback_WearsALightHide_ThatTheCannonPasses()
        {
            var crag = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Cragback.asset");
            Assert.IsNotNull(crag, "Run Tools/Generate/Scenes and Starter Data first.");
            Assert.Greater(crag.Plating, 0f);
            Assert.Greater(crag.PlatingFloor, 0f, "light guns still chip it");
            Assert.GreaterOrEqual(Health.ThroughPlating(25f, crag.Plating, crag.PlatingFloor) / 25f, 0.8f, "a shell passes the hide");
            Assert.Less(Health.ThroughPlating(5f, crag.Plating, crag.PlatingFloor) / 5f, 0.5f, "a light pulse does not");
        }

        // Damage per coin one level-0 tower deals to a wave of a single creature with this plating, from the ledger's own load.
        private static float PerCoin(TowerStats tower, float plating)
        {
            var load = new WaveLoad();
            load.StreamPlates.Add(plating, Floor, 100f);
            TowerLevelStats level = tower.Levels[0];
            return level.Dps * load.StreamPlates.Share(level.Damage) / level.Cost;
        }
    }
}
