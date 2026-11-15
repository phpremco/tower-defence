using System.Collections.Generic;
using Game.Balance;
using Game.Combat;
using Game.Core;
using Game.Editor;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 65, Remco's decisions after his second playtest. Raised ground slopes down over a ring of ground no tower stands
    /// on (so a gun on a plateau never fires through the ground: LineOfFireTests walks every line), a tower on high ground
    /// reaches further by the one number in the terrain rules, Dr. Voss says so once, the loading screen's tips stand on
    /// their own, and a flyer rounds the corners of its route on the arc its speed and turn rate allow.
    /// </summary>
    public sealed class Round65Tests
    {
        private const string DataRoot = "Assets/_Project/Data";
        private const string LevelFolder = DataRoot + "/Levels";
        private const float Tolerance = 0.0001f;

        // The drawn landscape against the cells: the middle of a raised cell is this close to the height a tower stands at,
        // and ground beyond the ring is this flat.
        private const float TopTolerance = 0.006f;
        private const float BeyondRingTolerance = 0.004f;
        // The steepest step between two points of the landscape (a quarter cell apart) on a plateau's bank: the old cliff
        // dropped the whole height in one step.
        private const float SteepestBankStep = 0.5f;

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"{DataRoot}/{path}.asset");

        private static IEnumerable<LevelDefinition> Levels()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        // ---- the plateau rules, on cells -------------------------------------------------------------------------

        [Test]
        public void RaisedGround_BesideTheRoadOrALiquid_IsTakenDown_AndTheRestKeepsItsRing()
        {
            // A plateau of three by two cells; the road runs along y = 3, right above its top row, and water lies left of it.
            var patches = new List<TerrainPatch>();
            for (int x = 2; x <= 4; x++)
            {
                for (int y = 1; y <= 2; y++)
                {
                    patches.Add(new TerrainPatch { Cell = new Vector2Int(x, y), Kind = TerrainKind.Raised });
                }
            }

            patches.Add(new TerrainPatch { Cell = new Vector2Int(1, 1), Kind = TerrainKind.Water });
            var size = new Vector2Int(8, 6);
            TerrainPatch[] kept = PlateauSkirt.KeepClear(patches, size, cell => cell.y == 3);
            var terrain = new TerrainMap(kept);
            Assert.AreEqual(TerrainKind.Ground, terrain.KindAt(new Vector2Int(3, 2)), "the row beside the road is taken down");
            Assert.AreEqual(TerrainKind.Ground, terrain.KindAt(new Vector2Int(2, 1)), "the cell beside the water is taken down");
            Assert.AreEqual(TerrainKind.Raised, terrain.KindAt(new Vector2Int(3, 1)), "a cell clear of both stays raised");
            Assert.AreEqual(TerrainKind.Water, terrain.KindAt(new Vector2Int(1, 1)), "the liquid stays");

            Vector2Int[] ring = PlateauSkirt.Ring(terrain, size);
            CollectionAssert.Contains(ring, new Vector2Int(3, 2), "the cell taken down is now part of the ring");
            CollectionAssert.Contains(ring, new Vector2Int(5, 0), "the ring goes round the corners");
            CollectionAssert.DoesNotContain(ring, new Vector2Int(1, 1), "a liquid is never a slope cell");
            foreach (Vector2Int cell in ring)
            {
                Assert.AreEqual(TerrainKind.Ground, terrain.KindAt(cell), $"{cell}: the ring is plain ground");
            }
        }

        [Test]
        public void ARaisedCell_TwoRowsBehindTheRim_IsShadowed_AndTheRimAndTheRowBehindItAreNot()
        {
            // Three rows of raised ground under a road along y = 6: the ring is y = 5, the rim y = 4, then y = 3 and y = 2.
            var patches = new List<TerrainPatch>();
            for (int x = 2; x <= 6; x++)
            {
                for (int y = 2; y <= 4; y++)
                {
                    patches.Add(new TerrainPatch { Cell = new Vector2Int(x, y), Kind = TerrainKind.Raised });
                }
            }

            var road = new List<Vector2Int>();
            for (int x = 0; x < 9; x++)
            {
                road.Add(new Vector2Int(x, 6));
            }

            var terrain = new TerrainMap(patches);
            var grid = new GridMap(new Vector2Int(9, 8), 1f);
            List<Vector2Int> shadowed = PlateauSkirt.Shadowed(terrain, grid, road, 0f, 6f, ProjectGenerator.PlateauSightRatio);
            CollectionAssert.DoesNotContain(shadowed, new Vector2Int(4, 4), "the rim sees the road: half a cell of plateau on two cells");
            CollectionAssert.DoesNotContain(shadowed, new Vector2Int(4, 3), "the row behind the rim still sees over it: 1.5 of 3 cells");
            CollectionAssert.Contains(shadowed, new Vector2Int(4, 2), "the third row fires through the lip: 2.5 of 4 cells");
            Assert.AreEqual(0.25f, PlateauSkirt.PlateauShare(new Vector2Int(4, 4), new Vector2(0f, 2f), 2f, terrain), 0.02f, "the rim's line leaves the plateau a quarter of the way");
        }

        // ---- the plateau rules, on every level --------------------------------------------------------------------

        [Test]
        public void EveryLevel_HasItsRingAsSlopeCells_NoTowerOnIt_AndPassesTheValidator()
        {
            int raised = 0;
            foreach (LevelDefinition level in Levels())
            {
                var errors = new List<string>();
                Assert.IsTrue(LevelValidator.ValidateLayout(level, errors), $"{level.name}: {string.Join("; ", errors)}");
                CollectionAssert.AreEquivalent(PlateauSkirt.Ring(level.Terrain, level.GridSize), level.SlopeCells, $"{level.name}: the slope cells are not the ring round its raised ground (run ApplyRound65)");
                Assert.IsNotNull(level.TerrainRules, $"{level.name}: no terrain rules");
                var build = new BuildGrid(level);
                foreach (Vector2Int cell in level.SlopeCells)
                {
                    Assert.IsFalse(build.CanBuild(cell), $"{level.name}: a tower can be built on the slope at {cell}");
                    Assert.IsFalse(level.Road.IsRoad(cell), $"{level.name}: the slope at {cell} lies on the road");
                }

                foreach (KeyValuePair<Vector2Int, TerrainKind> pair in level.Terrain.Cells)
                {
                    raised += pair.Value == TerrainKind.Raised ? 1 : 0;
                }
            }

            Assert.Greater(raised, 0, "no level has raised ground: the round's rules were not exercised");
        }

        [Test]
        public void TheValidator_NamesAPlateauWithoutItsRing_AndATowerBehindTheLip()
        {
            var level = Object.Instantiate(Load<LevelDefinition>("Levels/Level_C1_01"));
            try
            {
                var so = new SerializedObject(level);
                so.FindProperty("_slopeCells").arraySize = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                var errors = new List<string>();
                Assert.IsFalse(LevelValidator.ValidateLayout(level, errors), "a plateau without slope cells passes");
                Assert.IsTrue(errors.Exists(error => error.Contains("has no slope cell")), string.Join("; ", errors));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        // ---- the landscape that is drawn --------------------------------------------------------------------------

        [Test]
        public void TheLandscape_IsLevelUnderATowerOnHighGround_SlopesOverTheRingOnly_AndHasNoCliff()
        {
            foreach (LevelDefinition level in Levels())
            {
                var field = new TerrainField(level, new RoadShape(level));
                var slope = new HashSet<Vector2Int>(level.SlopeCells);
                GridMap grid = level.Grid;
                float quarter = level.CellSize * 0.25f;
                for (int x = 0; x < level.GridSize.x; x++)
                {
                    for (int y = 0; y < level.GridSize.y; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        Vector3 middle = grid.CellToWorld(cell);
                        TerrainKind kind = level.Terrain.KindAt(cell);
                        if (kind == TerrainKind.Raised)
                        {
                            Assert.AreEqual(TerrainMap.PlateauHeight, field.HeightAt(middle.x, middle.z), TopTolerance, $"{level.name} {cell}: the ground under a tower on high ground is not at its height");
                            continue;
                        }

                        if (slope.Contains(cell) || kind != TerrainKind.Ground)
                        {
                            continue;
                        }

                        // Plain ground beyond the ring: the plateau's slope has run out before it, all over the cell.
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                float plateau = field.PlateauAt(middle.x + dx * quarter * 1.9f, middle.z + dz * quarter * 1.9f);
                                Assert.LessOrEqual(plateau * TerrainMap.PlateauHeight, BeyondRingTolerance, $"{level.name} {cell}: the slope of raised ground reaches a cell beyond its ring");
                            }
                        }
                    }
                }

                // No cliff anywhere on a bank: from one point of the landscape to the next the plateau never drops more than
                // a share of its height (the old plateau fell its whole height in one step).
                for (int iz = 0; iz < field.Nz; iz++)
                {
                    for (int ix = 0; ix < field.Nx - 1; ix++)
                    {
                        float here = field.PlateauAt(field.WorldX(ix), field.WorldZ(iz));
                        float next = field.PlateauAt(field.WorldX(ix + 1), field.WorldZ(iz));
                        Assert.LessOrEqual(Mathf.Abs(here - next), SteepestBankStep, $"{level.name}: a cliff in the plateau's bank near ({field.WorldX(ix):0.0}, {field.WorldZ(iz):0.0})");
                    }
                }
            }
        }

        // ---- high ground reaches further --------------------------------------------------------------------------

        [Test]
        public void TheTerrainRules_CarryTheGeneratorsNumbers_AndEveryLevelPointsAtThem()
        {
            var rules = AssetDatabase.LoadAssetAtPath<TerrainRules>(TerrainRules.AssetPath);
            Assert.IsNotNull(rules, "no terrain rules asset (run ApplyRound65)");
            Assert.AreEqual(ProjectGenerator.HighGroundRange, rules.HighGroundRange, Tolerance);
            Assert.AreEqual(ProjectGenerator.PlateauSightRatio, rules.SightRatio, Tolerance);
            Assert.AreEqual("+10%", rules.HighGroundLabel, "Remco's decision: ten percent");
            Assert.AreEqual(rules.HighGroundRange, rules.RangeMultiplier(TerrainKind.Raised), Tolerance);
            Assert.AreEqual(1f, rules.RangeMultiplier(TerrainKind.Ground), Tolerance);
            foreach (LevelDefinition level in Levels())
            {
                Assert.AreSame(rules, level.TerrainRules, $"{level.name}: its own terrain rules");
            }
        }

        [Test]
        public void ATowerOnHighGround_ReachesFurther_ByTheRulesNumber_AndThePlanModelSeesTheSameSpot()
        {
            var level = Load<LevelDefinition>("Levels/Level_C1_01");
            var rules = level.TerrainRules;
            var build = new BuildGrid(level);
            Vector2Int raised = default;
            bool found = false;
            foreach (KeyValuePair<Vector2Int, TerrainKind> pair in level.Terrain.Cells)
            {
                if (pair.Value == TerrainKind.Raised && build.CanBuild(pair.Key))
                {
                    raised = pair.Key;
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found, "Landing Site has no raised build cell");
            Assert.IsTrue(PlateauSkirt.HasBuildSpot(level), "the high-ground hint would never play on Landing Site");
            Assert.AreEqual(rules.HighGroundRange, TerrainBalance.RangeMultiplier(level, raised), Tolerance);
            Assert.AreEqual(1f, TerrainBalance.RangeMultiplier(level, level.SlopeCells[0]), Tolerance, "the ring is not high ground");

            var definition = Load<TowerDefinition>("Towers/Laser");
            var go = new GameObject("Tower");
            try
            {
                var tower = go.AddComponent<Tower>();
                tower.enabled = false;
                var targets = new TargetRegistry();
                tower.Initialize(definition, targets, new ProjectilePool(targets, go.transform), PlayerModifiers.None);
                float plain = tower.Range;
                tower.GroundRange = TerrainBalance.RangeMultiplier(level, raised);
                Assert.AreEqual(plain * rules.HighGroundRange, tower.Range, Tolerance, "the tower's own range does not grow on high ground");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            // The plan model's spots: every raised spot carries the multiplier, no other spot does.
            int raisedSpots = 0;
            foreach (LevelDefinition any in Levels())
            {
                LevelGeometry geometry = BalanceLedger.Geometry(any);
                foreach (Cell spot in geometry.Spots)
                {
                    bool high = any.Terrain.KindAt(new Vector2Int(spot.X, spot.Y)) == TerrainKind.Raised;
                    raisedSpots += high ? 1 : 0;
                    Assert.AreEqual(high ? rules.HighGroundRange : 1f, geometry.SpotRange(spot), Tolerance, $"{any.name} spot {spot.X},{spot.Y}");
                }
            }

            Assert.Greater(raisedSpots, 0, "no level has a raised spot beside its road: the plan model never meets high ground");
        }

        // ---- Voss's hint and the loading tips ---------------------------------------------------------------------

        [Test]
        public void TheStory_HasVosssHighGroundHint_WithTheRulesNumberInIt()
        {
            var catalog = Load<CampaignCatalog>("Levels/CampaignCatalog");
            DialogueDefinition tip = catalog.Story.HighGroundTip;
            Assert.IsNotNull(tip, "the story has no high-ground hint (run ApplyRound65)");
            Assert.AreEqual(ProjectGenerator.HighGroundTipId, tip.Id);
            var errors = new List<string>();
            Assert.IsTrue(tip.Validate(errors), string.Join("; ", errors));
            var rules = AssetDatabase.LoadAssetAtPath<TerrainRules>(TerrainRules.AssetPath);
            string percent = rules.HighGroundLabel.TrimStart('+');
            bool named = false;
            foreach (DialogueLine line in tip.Lines)
            {
                Assert.AreEqual("Speaker_Voss", line.Speaker.name, "the hint is Voss's");
                foreach (DialogueSegment segment in line.Segments)
                {
                    named |= segment.Text.Contains(percent);
                }
            }

            Assert.IsTrue(named, $"the hint does not say {percent}");
        }

        [Test]
        public void EveryLoadingTip_StandsOnItsOwn_NamesWhatItIsAbout_AndFitsItsBox()
        {
            var catalog = Load<CampaignCatalog>("Levels/CampaignCatalog");
            StoryDefinition story = catalog.Story;
            foreach (ScanHint hint in story.ScanHints)
            {
                string tip = hint.Dialogue.LoadingTip;
                Assert.IsNotEmpty(tip, $"{hint.Dialogue.Id}: no loading tip");
                Assert.IsTrue(tip.Contains(hint.Enemy.DisplayName), $"{hint.Dialogue.Id}: the tip does not name the {hint.Enemy.DisplayName}: \"{tip}\"");
            }

            var level = Load<LevelDefinition>("Levels/Level_C1_03");
            var easy = Load<DifficultyProfile>("Difficulty/Easy");
            List<LoadingTip> tips = LoadingTips.For(story, level, easy);
            Assert.AreEqual(ProjectGenerator.LoadingTipTexts.Length, tips.Count, "a hint's tip is missing from the loading screen");
            foreach (LoadingTip tip in tips)
            {
                Assert.LessOrEqual(tip.Text.Length, LoadingTips.MaxLength, $"too long for its box: \"{tip.Text}\"");
                // A tip never opens on a word that points at something the screen has not shown.
                foreach (string opening in new[] { "It ", "They ", "Them ", "Those ", "That ", "This ", "Ground guns", "Air only" })
                {
                    Assert.IsFalse(tip.Text.StartsWith(opening, System.StringComparison.Ordinal), $"the tip opens on \"{opening}\": \"{tip.Text}\"");
                }
            }

            Assert.IsTrue(tips.Exists(tip => tip.Text.Contains("Skyray")), "the Skyray's tip names it (Remco: \"Ground guns can't touch it\" read without context)");
            Assert.IsTrue(tips.Exists(tip => tip.Text.Contains("high ground")), "the high-ground hint is among the tips");
        }

        // ---- flyers round their corners ---------------------------------------------------------------------------

        [Test]
        public void ARightAngleCorner_IsFlownOnAnArc_OfTheRadius_ShorterThanTheCorner()
        {
            var route = new[] { new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f) };
            const float radius = 2f;
            Vector3[] round = FlightCurve.Round(route, radius);
            Assert.AreEqual(route[0], round[0], "the route starts where it did");
            Assert.AreEqual(route[2], round[round.Length - 1], "and ends where it did");
            Assert.LessOrEqual(FlightCurve.SharpestTurn(round), 13f, "the heading changes in small steps");
            Assert.AreEqual(90f, FlightCurve.SharpestTurn(route), 0.01f);
            // Two tangents of a radius each become a quarter circle.
            Assert.AreEqual(20f - 2f * radius + Mathf.PI * radius * 0.5f, FlightCurve.Length(round), 0.03f);
            var centre = new Vector3(8f, 0f, 2f);
            for (int i = 1; i < round.Length - 1; i++)
            {
                Assert.AreEqual(radius, Vector3.Distance(centre, round[i]), 0.001f, $"point {i} is not on the arc");
            }
        }

        [Test]
        public void AShortLeg_DrawsTheArcIn_AndAStraightRouteStaysAsItIs()
        {
            var route = new[] { new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 1f), new Vector3(20f, 0f, 1f) };
            Vector3[] round = FlightCurve.Round(route, 3f);
            for (int i = 0; i < round.Length; i++)
            {
                Assert.GreaterOrEqual(round[i].z, -0.001f, "the arcs of two close corners overlap");
                Assert.LessOrEqual(round[i].z, 1.001f, "the arcs of two close corners overlap");
            }

            for (int i = 1; i < round.Length; i++)
            {
                Assert.GreaterOrEqual(round[i].x, round[i - 1].x - 0.001f, "the route doubles back on itself");
            }

            var straight = new[] { Vector3.zero, new Vector3(5f, 0f, 0f), new Vector3(9f, 0f, 0f) };
            Assert.AreEqual(3, FlightCurve.Round(straight, 2f).Length, "a straight route gains no points");
            Assert.AreEqual(2f, FlightCurve.Radius(2f, 180f / Mathf.PI, 0.5f), 0.0001f, "speed over turn rate");
            Assert.AreEqual(0.5f, FlightCurve.Radius(0.1f, 90f, 0.5f), 0.0001f, "never tighter than the floor");
        }

        [Test]
        public void EveryFlyer_TurnsAtARate_AndEveryFlightOfEveryLevel_StaysSmoothAndOnTheMap()
        {
            var flyers = new List<EnemyDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { DataRoot + "/Enemies" }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemy.Flies)
                {
                    flyers.Add(enemy);
                    Assert.Greater(enemy.TurnRate, 0f, $"{enemy.name} turns on the spot");
                    Assert.Greater(enemy.BankDegrees, 0f, $"{enemy.name} does not bank");
                }
            }

            Assert.IsNotEmpty(flyers);
            int routes = 0;
            foreach (LevelDefinition level in Levels())
            {
                var flights = new List<Vector3[]>();
                for (int p = 0; p < level.Paths.Length; p++)
                {
                    if (level.Paths[p].Flying)
                    {
                        flights.Add(level.Road.Route(p));
                    }
                }

                // A flyer out of a nest flies to the road and along it: the road's right angles.
                foreach (ElementNest nest in level.Nests)
                {
                    flights.Add(OffRoadRoute.Build(level, level.Grid.CellToWorld(nest.Cell), out _));
                }

                foreach (Vector3[] route in flights)
                {
                    foreach (EnemyDefinition flyer in flyers)
                    {
                        float radius = EnemySpawner.TurnRadius(flyer, level.CellSize);
                        Vector3[] round = FlightCurve.Round(route, radius);
                        routes++;
                        Assert.LessOrEqual(FlightCurve.SharpestTurn(round), 13f, $"{level.name}: {flyer.name} still turns sharply");
                        Assert.LessOrEqual(FlightCurve.Length(round), FlightCurve.Length(route) + 0.001f, $"{level.name}: rounding made a flight longer");
                        Assert.GreaterOrEqual(FlightCurve.Length(round), FlightCurve.Length(route) * 0.9f, $"{level.name}: rounding cut more than a tenth off a flight");
                        foreach (Vector3 point in round)
                        {
                            // An arc lies inside its corner, so it never leaves the box its waypoints span.
                            Assert.IsTrue(Inside(point, route, 0.01f), $"{level.name}: {flyer.name}'s arc leaves the route's own bounds at {point}");
                        }
                    }
                }
            }

            Assert.Greater(routes, 0);
        }

        private static bool Inside(Vector3 point, Vector3[] route, float slack)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (Vector3 waypoint in route)
            {
                minX = Mathf.Min(minX, waypoint.x);
                maxX = Mathf.Max(maxX, waypoint.x);
                minZ = Mathf.Min(minZ, waypoint.z);
                maxZ = Mathf.Max(maxZ, waypoint.z);
            }

            return point.x >= minX - slack && point.x <= maxX + slack && point.z >= minZ - slack && point.z <= maxZ + slack;
        }
    }
}
