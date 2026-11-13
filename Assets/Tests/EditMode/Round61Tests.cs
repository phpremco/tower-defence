using System.Collections.Generic;
using Game.Balance;
using Game.Combat;
using Game.Core;
using Game.Editor;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 61, Remco after his first playthrough. "You don't need air defence on Easy": the Gun Turret does a fifth of its
    /// damage to flyers, the ledger stands a sky gun for the first flyers (so a raid is sized against it and not against a row
    /// of Gun Turrets), and an Easy raid's flyers keep a floor of their book health. "The shockwave looks boring": the hero's
    /// shockwave is an effect of its own in the library, every part of it fades in and out, and the Low profile plays less of it.
    /// </summary>
    public sealed class Round61Tests
    {
        private const string TowerFolder = "Assets/_Project/Data/Towers";
        private const string LibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";
        private const string SkyGun = "Rocket";
        private const string AllRounder = "PulseRifle";
        private const int Easy = 0;
        private const float Radius = 3.2f;

        // Shard Ridge and Fault Line: the first two levels that sell the sky gun.
        private static readonly string[] FirstAirLevels = { "C1_03", "C1_04" };

        private static TowerDefinition Tower(string id) => AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{TowerFolder}/{id}.asset");

        [Test]
        public void TheGunTurret_BarelyScratchesFlyers_AndTheFlakIsMadeForThem()
        {
            TowerDefinition gun = Tower(AllRounder);
            TowerDefinition flak = Tower(SkyGun);
            Assert.AreEqual(ProjectGenerator.GunAirShare, gun.AirDamageFactor, 1e-4f, "the Gun Turret asset and the generator disagree on its air share");
            Assert.LessOrEqual(gun.AirDamageFactor, 0.25f, "a row of Gun Turrets must not be an air defence");
            Assert.Greater(gun.AirDamageFactor, 0f, "the Gun Turret still finishes a straggler over the base");
            Assert.AreEqual(TargetLayers.Air, flak.Targets, "the Flak is the gun made for the sky");
            float gunPerCoin = gun.GetLevel(0).Damage * gun.AirDamageFactor / gun.GetLevel(0).FireInterval / gun.BuildCost;
            float flakPerCoin = flak.GetLevel(0).Damage * flak.AirDamageFactor / flak.GetLevel(0).FireInterval / flak.BuildCost;
            Assert.Greater(flakPerCoin, gunPerCoin * 3f, "against one flyer a coin of Flak must be worth several coins of Gun Turret (its blast comes on top)");
        }

        [Test]
        public void TheLedger_StandsASkyGun_ForEveryWaveWithFlyers_OnTheFirstAirLevels()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            foreach (string key in FirstAirLevels)
            {
                int index = loaded.Model.IndexOf(key);
                int node = NodeOf(loaded.Catalog, key);
                Assert.GreaterOrEqual(index, 0, key + " is not in the campaign model");
                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    LevelInput input = loaded.Model.InputFor(index, d);
                    int sky = input.Towers.FindIndex(t => t.Id == SkyGun);
                    Assert.GreaterOrEqual(sky, 0, $"{key} difficulty {d}: the expected player does not own the sky gun");
                    List<WaveLine> lines = BalanceLedger.Evaluate(loaded.Model, index, loaded.Catalog.Nodes[node].Level, loaded.Catalog.Difficulties[d], d, new List<WaveLoad>());
                    foreach (WaveLine line in lines)
                    {
                        if (line.AirHealth > 0f)
                        {
                            StringAssert.Contains(sky + ":", line.Purchase, $"{key} difficulty {d} wave {line.Wave}: flyers come and the ledger bought no sky gun ({line.Purchase})");
                        }
                    }
                }
            }
        }

        [Test]
        public void WithoutTheRule_TheLedgerHoldsASmallRaidWithARowOfAllRounders()
        {
            // The round-61 finding as a regression guard: Shard Ridge's first Easy raid as the script composes it (wave 12), at
            // the multiplier it was stored with before the round (x0.138) and that wave's purse (656), with the Gun Turret's
            // old half share up there, with and without BalanceRules.SkyGunForFlyers.
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            int index = loaded.Model.IndexOf(FirstAirLevels[0]);
            loaded.Model.Towers[AllRounder].AirFactor = 0.5f;
            LevelInput input = loaded.Model.InputFor(index, Easy);
            int sky = input.Towers.FindIndex(t => t.Id == SkyGun);
            BalanceRules with = BalanceRules.Default;
            BalanceRules without = BalanceRules.Default;
            without.SkyGunForFlyers = false;
            WaveSpec raid = WaveComposer.Compose(WaveScripts.ByKey(FirstAirLevels[0]), loaded.Model.Enemies, Easy, 12, 0.138f, loaded.Model.Facts[index].SecondRoad, with);
            Assert.IsTrue(raid.Groups.Exists(g => g.Enemy.Flies), "wave 12 of Shard Ridge Easy is no raid any more: pick the level's first raid");
            WaveLine stood = Ledger.EvaluateWave(input, raid, loaded.Model.Difficulties[Easy], 656, with);
            WaveLine row = Ledger.EvaluateWave(input, raid, loaded.Model.Difficulties[Easy], 656, without);
            StringAssert.Contains(sky + ":", stood.Purchase, "with the rule a sky gun stands for the raid");
            StringAssert.DoesNotContain(sky + ":", row.Purchase, "without the rule the small raid is held by the guns that merely reach the air (the bug)");
            Assert.Less(stood.AirPressure, row.AirPressure, "the sky gun holds the raid better than the row");
        }

        [Test]
        public void EasyRaids_KeepTheFloor_UnlessTheHeroCapSoftenedThem()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            BalanceRules rules = loaded.Model.Rules;
            float floor = rules.RaidHealthFloorFor(Easy);
            Assert.Greater(floor, 0f, "Easy has no raid floor");
            BalanceRules withHero = rules.HumanWithHero();
            int raids = 0;
            foreach (string key in FirstAirLevels)
            {
                int index = loaded.Model.IndexOf(key);
                WaveScriptSpec script = WaveScripts.ByKey(key);
                // The solve sets the script's Easy raid floor, so the wave kinds below are the ones the stored waves were built with.
                List<List<WaveSpec>> sets = loaded.Model.BuildWaves(index, script, out _);
                WaveDefinition[] stored = loaded.Catalog.Nodes[NodeOf(loaded.Catalog, key)].Level.GetWaves(loaded.Catalog.Difficulties[Easy]);
                List<WaveLine> lines = Ledger.Evaluate(loaded.Model.InputFor(index, Easy), sets[Easy], loaded.Model.Difficulties[Easy], withHero);
                for (int i = 0; i < stored.Length; i++)
                {
                    Assert.AreEqual(sets[Easy][i].HealthMultiplier, stored[i].HealthMultiplier, 0.0015f, $"{key} Easy wave {i + 1}: the stored wave is not what the solve gives (run the solve stage)");
                    if (WaveComposer.KindOf(script, Easy, i + 1) != WaveComposer.Kind.Air)
                    {
                        continue;
                    }

                    raids++;
                    bool capped = lines[i].Pressure >= rules.HeroCapFor(Easy) * 0.97f;
                    Assert.IsTrue(stored[i].HealthMultiplier >= floor - 0.0015f || capped,
                        $"{key} Easy wave {i + 1}: a raid at x{stored[i].HealthMultiplier:0.000}, below the floor {floor:0.00} and not at the hero cap ({lines[i].Pressure:0.00})");
                }
            }

            Assert.Greater(raids, 0, "no Easy raid found on the first air levels");
        }

        [Test]
        public void TheHeroShockwave_IsInTheLibrary_WithEveryPaintedPart()
        {
            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
            ShockwaveVfxDefinition vfx = library.Shockwave;
            Assert.IsNotNull(vfx, "the effect library has no shockwave (run Tools/PlayTest/Scripts/ApplyRound61.cs)");
            Assert.IsNotNull(vfx.RingSheet, "no ring sheet");
            Assert.IsNotNull(vfx.Distortion, "no refraction sheet");
            Assert.IsNotNull(vfx.Flash, "no flash sheet");
            Assert.IsNotNull(vfx.SparkSheet, "no spark sheet");
            Assert.IsNotNull(vfx.DebrisSheet, "no debris sheet");
            Assert.IsTrue(vfx.HasEcho, "the second ring is missing");
            Assert.Less(vfx.EchoDelay, vfx.RingSeconds, "the echo must leave while the first ring still runs");
            Assert.Greater(vfx.DustPuffs, 0, "no dust");
            Assert.Greater(vfx.Sparks, 0, "no sparks");
            Assert.Greater(vfx.LightIntensity, 0f, "no light flash");
            // A player effect stays in the cool palette: the dust is blue-grey, never warm.
            Assert.GreaterOrEqual(vfx.DustColor.b, vfx.DustColor.r, "the dust leans warm");
        }

        [Test]
        public void TheShockwave_FadesInAndOut_AndReachesItsRadius()
        {
            Assert.AreEqual(0f, ShockwaveView.Envelope(0f, 0.08f, 0.4f), 1e-5f, "a ring must start from nothing");
            Assert.AreEqual(0f, ShockwaveView.Envelope(1f, 0.08f, 0.4f), 1e-5f, "a ring must end on nothing");
            Assert.AreEqual(1f, ShockwaveView.Envelope(0.2f, 0.08f, 0.4f), 1e-4f, "a ring is at full strength between its fades");
            float last = 0f;
            for (int i = 0; i <= 100; i++)
            {
                float value = ShockwaveView.Envelope(i / 100f, 0.08f, 0.4f);
                Assert.LessOrEqual(Mathf.Abs(value - last), 0.2f, $"the ring's strength jumps at {i}% of its run");
                last = value;
            }

            Assert.AreEqual(Radius * 2f, ShockwaveView.Diameter(1f, Radius), 1e-4f, "the front must end on the ability's radius");
            Assert.Less(ShockwaveView.Diameter(0f, Radius), 0.5f, "the front must start at the fist");
            for (int i = 1; i <= 100; i++)
            {
                Assert.GreaterOrEqual(ShockwaveView.Diameter(i / 100f, Radius), ShockwaveView.Diameter((i - 1) / 100f, Radius), "the front never runs backwards");
            }
        }

        [Test]
        public void TheLowProfile_PlaysFewerParticles_NoRefraction_NoLight()
        {
            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
            Assert.IsNotNull(library.Shockwave, "the effect library has no shockwave");
            bool had = Services.TryGet(out MarkerArt before);
            Services.Register(library.Markers);
            var root = new GameObject("Round61Shockwave");
            try
            {
                ShockwaveView high = ShockwaveView.Create(root.transform, VfxQuality.High);
                ShockwaveView low = ShockwaveView.Create(root.transform, new VfxQuality(0.5f, true, false));
                high.Play(Vector3.zero, Radius, Color.cyan, library.Shockwave);
                low.Play(Vector3.zero, Radius, Color.cyan, library.Shockwave);
                Assert.IsTrue(high.IsPlaying && low.IsPlaying, "the wave did not start");
                ShockwaveVfxDefinition vfx = library.Shockwave;
                int highCount = ShockwaveView.BudgetFor(vfx.Sparks, 1f, 100) + ShockwaveView.BudgetFor(vfx.Shards, 1f, 100) + ShockwaveView.BudgetFor(vfx.DustPuffs, 1f, 100);
                int lowCount = ShockwaveView.BudgetFor(vfx.Sparks, 0.5f, 100) + ShockwaveView.BudgetFor(vfx.Shards, 0.5f, 100) + ShockwaveView.BudgetFor(vfx.DustPuffs, 0.5f, 100);
                Assert.Greater(highCount, 0, "the high profile throws no particles");
                Assert.Less(lowCount, highCount * 0.7f, $"the low profile throws {lowCount} particles against {highCount}");
                Assert.AreEqual(1, ShockwaveView.BudgetFor(3, 0.1f, 100), "a part never drops to nothing on a low budget");
                Assert.IsTrue(Part(high, "Refraction").sharedMaterial != null, "the high profile has no refraction ring");
                Assert.IsTrue(Part(low, "Refraction").sharedMaterial == null, "the low profile renders no opaque texture, so no refraction");
                Assert.IsTrue(high.GetComponentInChildren<Light>(true).enabled, "the high profile flashes a light");
                Assert.IsFalse(low.GetComponentInChildren<Light>(true).enabled, "the low profile goes without the light");
                foreach (ParticleSystemRenderer renderer in high.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    Assert.IsNotNull(renderer.sharedMaterial, renderer.name + " has no material");
                    Assert.AreNotSame(ParticleKit.FallbackMaterial, renderer.sharedMaterial, renderer.name + " wears the URP default particle material (null in a build)");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (had)
                {
                    Services.Register(before);
                }
                else
                {
                    Services.Unregister<MarkerArt>();
                }
            }
        }

        private static MeshRenderer Part(ShockwaveView view, string partName)
        {
            foreach (MeshRenderer card in view.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (card.name == partName)
                {
                    return card;
                }
            }

            Assert.Fail("the shockwave has no part called " + partName);
            return null;
        }

        private static int NodeOf(CampaignCatalog catalog, string key)
        {
            for (int i = 0; i < catalog.Nodes.Count; i++)
            {
                if (catalog.Nodes[i].Level != null && catalog.Nodes[i].Level.name == "Level_" + key)
                {
                    return i;
                }
            }

            Assert.Fail("no level " + key);
            return -1;
        }
    }
}
