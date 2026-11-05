using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Proof of the slime rule (Docs bestiary; Remco 2026-11-05: "bewijs dat de boost werkt") in a real level with the
    /// real creature data: a creature of another species walking a slime trail goes the Oozer's trail haste times its
    /// own pace, and its own pace again once it is off the trail and the haste has run out; an Oozer gains nothing from
    /// its own slime; slime burns a gate and the hero but never a tower. The measured numbers go to the log.
    /// </summary>
    public sealed class SlimeBoostTests
    {
        private const string EnemyFolder = "Assets/_Project/Data/Enemies/";
        private const float Tolerance = 0.04f;
        private const float MeasureSeconds = 1.5f;
        private const float TrailAhead = 1.5f;
        private const float TrailLength = 9f;
        private const float PatchStep = 0.5f;
        private const float HasteRunsOut = 0.6f;

        private LevelContext _context;
        private EnemySpawner _spawner;

        [UnityTest]
        public IEnumerator SlimeHurriesOtherSpecies_NotTheOozer_AndBurnsGateAndHeroButNoTower()
        {
            yield return LoadLevel();
            var oozerDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyFolder + "Oozer.asset");
            float haste = oozerDefinition.TrailHaste;
            Assert.Greater(haste, 1f, "the Oozer's slime has no haste in the data");
            _context.Hero.MoveTo(_context.Hero.PostCount - 1);

            // An Oozer as the layer of the hand-laid slime; it leaves the field at once, and its first real patches dry
            // up before anything is measured, so the clean runs are clean.
            Enemy oozer = Spawn(oozerDefinition);
            oozer.Deactivate();
            yield return Walk(oozerDefinition.TrailSeconds + 1f);
            var report = new List<string> { $"Oozer trail haste in data: x{haste:0.00}" };
            foreach (string species in new[] { "Skitter", "Prowler", "Cragback" })
            {
                var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyFolder + species + ".asset");
                Enemy walker = Spawn(definition);
                yield return Walk(MeasureSeconds * 0.5f);
                float[] clean = new float[1];
                yield return Measure(walker, MeasureSeconds, false, clean);
                LayTrail(oozer, walker);
                float[] slimed = new float[1];
                yield return WaitHasted(walker);
                yield return Measure(walker, MeasureSeconds, true, slimed);
                float[] after = new float[1];
                yield return WaitOffTrail(walker);
                yield return Measure(walker, MeasureSeconds, false, after);
                report.Add($"{species}: clean x{clean[0]:0.000}, on slime x{slimed[0]:0.000}, after x{after[0]:0.000}");
                Assert.AreEqual(1f, clean[0], Tolerance, $"{species} off the trail");
                Assert.AreEqual(haste, slimed[0], haste * Tolerance, $"{species} on the trail");
                Assert.AreEqual(1f, after[0], Tolerance, $"{species} after the trail");
                walker.TakeDamageFrom(walker.Health.Current + 1f, null, false);
            }

            // A live Oozer crawls over its own slime the whole time it moves.
            oozer = Spawn(oozerDefinition);
            yield return Walk(MeasureSeconds);
            LayTrail(oozer, oozer);
            float[] own = new float[1];
            yield return Measure(oozer, MeasureSeconds, false, own, true);
            report.Add($"Oozer on its own slime: x{own[0]:0.000} (never hasted)");
            Assert.AreEqual(1f, own[0], Tolerance, "the Oozer gains nothing from its own slime");

            yield return CheckBurns(oozer, report);
            Debug.Log("Slime boost proof:\n" + string.Join("\n", report));
        }

        private IEnumerator LoadLevel()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;
            var catalog = Services.Get<CampaignCatalog>();
            Services.Register(new LevelSession(catalog.Nodes[0].Level, catalog.Difficulties[1]));
            SceneManager.LoadScene(SceneNames.Level);
            yield return null;
            yield return null;
            while (!Services.TryGet(out _context))
            {
                yield return null;
            }

            var controller = Object.FindAnyObjectByType<LevelController>();
            _spawner = (EnemySpawner)typeof(LevelController).GetField("_spawner", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);
            // No waves: the test spawns by hand and steps the slime the way the controller does.
            controller.enabled = false;
            // The level's own gates would hold the walkers (and their haste) mid-measurement.
            foreach (Tower tower in Object.FindObjectsByType<Tower>())
            {
                if (tower.Definition.BlocksGroundEnemies)
                {
                    _context.Structures.Remove(tower);
                    Object.Destroy(tower.gameObject);
                }
            }
        }

        private Enemy Spawn(EnemyDefinition definition)
        {
            _spawner.Spawn(definition, 0);
            Enemy found = null;
            foreach (Enemy enemy in Object.FindObjectsByType<Enemy>())
            {
                if (enemy.IsAlive && enemy.Definition == definition)
                {
                    found = enemy;
                }
            }

            Assert.IsNotNull(found, $"no {definition.name} came out");
            return found;
        }

        // Patches the Oozer would leave, laid along the walker's road ahead of it.
        private void LayTrail(Enemy layer, Enemy walker)
        {
            Vector3[] route = walker.RemainingRoute();
            float along = 0f;
            float next = layer == walker ? 0f : TrailAhead;
            for (int i = 1; i < route.Length && next < TrailAhead + TrailLength; i++)
            {
                float leg = Vector3.Distance(route[i - 1], route[i]);
                while (next <= along + leg && next < TrailAhead + TrailLength)
                {
                    _spawner.Slime.Add(layer, Vector3.Lerp(route[i - 1], route[i], (next - along) / Mathf.Max(0.001f, leg)));
                    next += PatchStep;
                }

                along += leg;
            }
        }

        private IEnumerator Walk(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                _spawner.Step(Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator WaitHasted(Enemy walker)
        {
            for (float t = 0f; t < 10f && !walker.IsHasted; t += Time.deltaTime)
            {
                _spawner.Step(Time.deltaTime);
                yield return null;
            }

            Assert.IsTrue(walker.IsHasted, $"{walker.Definition.name} never got the slime haste");
        }

        private IEnumerator WaitOffTrail(Enemy walker)
        {
            float clear = 0f;
            for (float t = 0f; t < 20f && clear < HasteRunsOut; t += Time.deltaTime)
            {
                clear = walker.IsHasted ? 0f : clear + Time.deltaTime;
                _spawner.Step(Time.deltaTime);
                yield return null;
            }
        }

        // Ground covered over what its own pace would cover, in frames where the haste state held throughout.
        private IEnumerator Measure(Enemy walker, float seconds, bool hasted, float[] ratio, bool neverHasted = false)
        {
            float covered = 0f;
            float expected = 0f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float before = walker.PathProgress;
                bool wasHasted = walker.IsHasted;
                _spawner.Step(Time.deltaTime);
                yield return null;
                if (neverHasted)
                {
                    Assert.IsFalse(walker.IsHasted, $"{walker.Definition.name} is hurried by its own slime");
                }

                if (wasHasted == hasted && walker.IsHasted == hasted && walker.IsAlive)
                {
                    covered += walker.PathProgress - before;
                    expected += walker.GroundSpeed * Time.deltaTime;
                }
            }

            Assert.Greater(expected, 0f, $"{walker.Definition.name} was not measured"); 
            ratio[0] = covered / expected;
        }

        // A tower and a gate stand in slime, the hero stands in slime: only the gate and the hero burn.
        private IEnumerator CheckBurns(Enemy oozer, List<string> report)
        {
            var placement = Object.FindAnyObjectByType<PlacementController>();
            MethodInfo create = typeof(PlacementController).GetMethod("CreateTower", BindingFlags.NonPublic | BindingFlags.Instance);
            var rifle = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/PulseRifle.asset");
            var blockade = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Blockade.asset");
            LevelDefinition level = _context.Level;
            var grid = new BuildGrid(level);
            Vector2Int towerCell = Vector2Int.zero;
            bool found = false;
            for (int x = 0; x < level.GridSize.x && !found; x++)
            {
                for (int y = 0; y < level.GridSize.y && !found; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (grid.CanBuild(cell))
                    {
                        towerCell = cell;
                        found = true;
                    }
                }
            }

            Assert.IsTrue(found, "no free pad");
            var tower = (Tower)create.Invoke(placement, new object[] { rifle, towerCell, false });
            var gate = (Tower)create.Invoke(placement, new object[] { blockade, level.MainRoad[level.MainRoad.Length / 2], false });
            Vector3 heroAt = _context.Hero.transform.position;
            float towerBefore = tower.Health.Current;
            float gateBefore = gate.Health.Current;
            float heroBefore = _context.Hero.Health.Current;
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                _spawner.Slime.Add(oozer, tower.Position);
                _spawner.Slime.Add(oozer, gate.Position);
                _spawner.Slime.Add(oozer, heroAt);
                _spawner.Step(Time.deltaTime);
                yield return null;
            }

            report.Add($"2 s in slime: tower -{towerBefore - tower.Health.Current:0.0}, gate -{gateBefore - gate.Health.Current:0.0}, hero -{heroBefore - _context.Hero.Health.Current:0.0}");
            Assert.AreEqual(towerBefore, tower.Health.Current, 0.001f, "a tower took slime damage");
            Assert.Less(gate.Health.Current, gateBefore, "the gate in the slime did not burn");
            Assert.Less(_context.Hero.Health.Current, heroBefore, "the hero in the slime did not burn");
        }
    }
}
