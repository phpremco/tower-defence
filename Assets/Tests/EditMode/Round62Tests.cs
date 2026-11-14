using System.Collections.Generic;
using Game.Balance;
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
    /// Round 62. The player is told what a tower does against ground and sky: the words come from the tower's data (target
    /// layers and the damage share per layer, <see cref="TargetRead"/>), so a retuned share changes the card without code.
    /// Fault Line's Easy raids are no longer pairs. And the calibration was confirmed for the new inputs with its efficiencies
    /// kept, which only holds while every stored wave is still exactly what the solve gives.
    /// </summary>
    public sealed class Round62Tests
    {
        private const string TowerFolder = "Assets/_Project/Data/Towers";
        private const string FaultLine = "C1_04";
        private const int Easy = 0;

        // Two Skyrays were cleared by a row of upgraded Gun Turrets; the first Easy raid brings at least this many now.
        private const int MinFlyersInARaid = 3;

        private static TowerDefinition Tower(string id) => AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{TowerFolder}/{id}.asset");

        [TestCase("PulseRifle", TargetReadKind.AirWeak, "Air: weak")]
        [TestCase("Rocket", TargetReadKind.AirOnly, "Air only")]
        [TestCase("Cannon", TargetReadKind.GroundOnly, "Ground only")]
        [TestCase("Laser", TargetReadKind.GroundOnly, "Ground only")]
        [TestCase("Railgun", TargetReadKind.GroundAndAir, "Ground and air")]
        public void TheTowerSaysWhatItDoesUpThere(string id, TargetReadKind kind, string label)
        {
            TowerDefinition tower = Tower(id);
            Assert.IsNotNull(tower, id + " is not in " + TowerFolder);
            Assert.AreEqual(kind, TargetRead.Of(tower), id);
            Assert.AreEqual(label, TargetRead.Label(tower), id);
        }

        [Test]
        public void TheRead_FollowsTheData_NotTheTowerId()
        {
            TowerDefinition gun = Tower("PulseRifle");
            // The rule the label rests on, stated on the asset's own numbers: aims at both layers, a small share lands up there.
            Assert.AreEqual(TargetLayers.Ground | TargetLayers.Air, gun.Targets);
            Assert.LessOrEqual(gun.AirDamageFactor, TargetRead.WeakShare);
            Assert.Greater(gun.GroundDamageFactor, TargetRead.WeakShare);
            Assert.AreEqual(TargetReadKind.None, TargetRead.Of(null), "no tower, nothing to say");
            Assert.IsEmpty(TargetRead.Label(TargetReadKind.None));
        }

        [Test]
        public void ASupportTower_SaysNothingAboutTargets()
        {
            TowerDefinition support = Tower("Support");
            Assert.IsNotNull(support);
            Assert.AreEqual(TargetReadKind.None, TargetRead.Of(support), "a tower that does not shoot has no ground or air to talk about");
        }

        [Test]
        public void FaultLineEasy_RaidsAreNoLongerPairs()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            WaveScriptSpec script = WaveScripts.ByKey(FaultLine);
            Assert.Greater(script.AirRaidScaleFor(Easy), 1f, "Fault Line's Easy raids are back at the script's plain size");
            List<List<WaveSpec>> sets = loaded.Model.BuildWaves(loaded.Model.IndexOf(FaultLine), script, out _);
            DifficultyStats difficulty = loaded.Model.Difficulties[Easy];
            int raids = 0;
            for (int i = 0; i < sets[Easy].Count; i++)
            {
                if (WaveComposer.KindOf(script, Easy, i + 1) != WaveComposer.Kind.Air)
                {
                    continue;
                }

                raids++;
                int flyers = 0;
                foreach (GroupSpec group in sets[Easy][i].Groups)
                {
                    if (group.Enemy.Flies && !group.Enemy.IsBoss)
                    {
                        flyers += Mathf.Max(1, Mathf.CeilToInt(group.Count * difficulty.Count));
                    }
                }

                Assert.GreaterOrEqual(flyers, MinFlyersInARaid, $"{FaultLine} Easy wave {i + 1}: a raid of {flyers}, which a row of Gun Turrets clears");
            }

            Assert.Greater(raids, 0, "Fault Line Easy has no raid");
        }

        [Test]
        public void EveryStoredWave_IsWhatTheSolveGives_AtTheAppliedCalibration()
        {
            // The calibration's efficiencies were kept when its inputs were re-stamped (BalanceStages.ApplyCalibration, keep): that
            // is only honest while no stored wave differs from the solve. A new fit applied without a solve fails here.
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            int cells = 0;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
                int index = loaded.Model.IndexOf(key);
                WaveScriptSpec script = WaveScripts.ByKey(key);
                if (index < 0 || script == null)
                {
                    continue;
                }

                List<List<WaveSpec>> sets = loaded.Model.BuildWaves(index, script, out _);
                for (int d = 0; d < sets.Count; d++)
                {
                    WaveDefinition[] stored = node.Level.GetWaves(loaded.Catalog.Difficulties[d]);
                    Assert.AreEqual(sets[d].Count, stored.Length, $"{key} difficulty {d}: wave count");
                    for (int i = 0; i < stored.Length; i++)
                    {
                        Assert.AreEqual(sets[d][i].HealthMultiplier, stored[i].HealthMultiplier, 0.0015f, $"{key} difficulty {d} wave {i + 1}: the stored wave is not what the solve gives (run the solve stage)");
                    }

                    cells++;
                }
            }

            Assert.Greater(cells, 0, "no campaign cell with a wave script");
        }

        [Test]
        public void ACalibrationWithKeptFactors_SaysSoInItsReportLine()
        {
            var calibration = ScriptableObject.CreateInstance<BalanceCalibration>();
            try
            {
                var so = new SerializedObject(calibration);
                so.FindProperty("_measuredOn").stringValue = "2026-11-14 00:30";
                so.FindProperty("_inputHash").stringValue = "abc";
                so.ApplyModifiedPropertiesWithoutUndo();
                StringAssert.DoesNotContain("kept", calibration.Describe("abc"), "an applied fit has nothing kept");
                so.FindProperty("_factorsFrom").stringValue = "2026-11-10 03:33";
                so.ApplyModifiedPropertiesWithoutUndo();
                StringAssert.Contains("factors of 2026-11-10 03:33 kept", calibration.Describe("abc"));
                StringAssert.Contains("current", calibration.Describe("abc"));
            }
            finally
            {
                Object.DestroyImmediate(calibration);
            }
        }
    }
}
