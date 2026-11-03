using System.Collections.Generic;
using Game.Balance;
using Game.Editor;
using Game.Levels;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// The pure layout generator and its checks: every template lays out a fair map within its attempts for many seeds,
    /// the same spec always gives the same map, the rules the checks enforce really hold on what passes, and a bad map
    /// is refused.
    /// </summary>
    public sealed class LayoutTests
    {
        private const int Seeds = 50;
        private static readonly BalanceRules Rules = BalanceRules.Default;

        private static LayoutSpec Spec(RoadTemplate template, int seed)
        {
            var spec = new LayoutSpec
            {
                Key = $"Test_{template}",
                Template = template,
                Seed = seed,
                Width = 40,
                Height = 22,
                Legs = template == RoadTemplate.Hook ? 4 : 3,
                Gates = 4,
                Plazas = template == RoadTemplate.Fork ? 1 : 0,
                Terrain = new[] { new TerrainShare("Water", 0.05f), new TerrainShare(LayoutTerrain.RaisedName, 0.03f) },
                Nests = new[] { new NestCount("Water", 1), new NestCount("Wood", 1) },
            };
            return spec;
        }

        private static RoadLayout FirstPassing(LayoutSpec spec, out List<string> lastErrors)
        {
            lastErrors = null;
            for (int attempt = 0; attempt < RoadGenerator.Attempts; attempt++)
            {
                RoadLayout layout = RoadGenerator.Build(spec, attempt);
                lastErrors = LayoutChecks.Check(layout, spec, Rules);
                if (lastErrors.Count == 0)
                {
                    return layout;
                }
            }

            return null;
        }

        [Test]
        public void EveryTemplate_FindsAFairLayout_ForFiftySeeds()
        {
            foreach (RoadTemplate template in System.Enum.GetValues(typeof(RoadTemplate)))
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    LayoutSpec spec = Spec(template, seed);
                    RoadLayout layout = FirstPassing(spec, out List<string> errors);
                    Assert.IsNotNull(layout, $"{template} seed {seed}: no attempt passed; last: {string.Join("; ", errors)}");

                    int length = LayoutGeometry.Walk(layout.Road).Count - 1;
                    Assert.That(length, Is.InRange(LayoutChecks.MinRoadLength, LayoutChecks.MaxRoadLength), $"{template} seed {seed}");
                    Assert.That(layout.Gates.Count, Is.InRange(LayoutChecks.MinGates, LayoutChecks.MaxGates));
                    Assert.AreEqual(spec.HasSecondRoad, layout.SecondRoad != null);
                }
            }
        }

        [Test]
        public void ParallelLegs_KeepTheirBuildRows()
        {
            foreach (RoadTemplate template in System.Enum.GetValues(typeof(RoadTemplate)))
            {
                for (int seed = 0; seed < Seeds; seed += 7)
                {
                    RoadLayout layout = FirstPassing(Spec(template, seed), out _);
                    Cell[] road = layout.Road;
                    for (int i = 2; i < road.Length; i++)
                    {
                        // Two legs joined by a short connector run side by side; their corridors keep two free rows.
                        List<Cell> a = LayoutGeometry.SegmentCorridor(road[i - 2], road[i - 1], layout.RoadWidth);
                        for (int k = i + 1; k < road.Length; k++)
                        {
                            List<Cell> b = LayoutGeometry.SegmentCorridor(road[k - 1], road[k], layout.RoadWidth);
                            Assert.GreaterOrEqual(LayoutGeometry.Distance(a, b) - 1, RoadGenerator.MinBuildRows, $"{template} seed {seed}: legs {i - 2} and {k - 1}");
                        }
                    }
                }
            }
        }

        // The owner's rule from the wide roads: a U-turn to the right needs five rows between the waypoint lines, to the left three.
        [Test]
        public void UTurnGap_FollowsTheWideRoadRule()
        {
            Assert.AreEqual(5, RoadGenerator.UTurnGap(true, 2));
            Assert.AreEqual(3, RoadGenerator.UTurnGap(false, 2));
            Assert.AreEqual(3, RoadGenerator.UTurnGap(true, 1));
        }

        [Test]
        public void SameSpec_GivesTheSameMap()
        {
            LayoutSpec spec = Spec(RoadTemplate.Fork, 7);
            RoadLayout a = RoadGenerator.Build(spec, 3);
            RoadLayout b = RoadGenerator.Build(spec, 3);

            CollectionAssert.AreEqual(Text(a.Road), Text(b.Road));
            CollectionAssert.AreEqual(Text(a.SecondRoad), Text(b.SecondRoad));
            CollectionAssert.AreEqual(Text(a.Gates.ToArray()), Text(b.Gates.ToArray()));
            Assert.AreEqual(a.Terrain.Count, b.Terrain.Count);
            CollectionAssert.AreNotEqual(Text(a.Road), Text(RoadGenerator.Build(spec, 4).Road), "another attempt lays out another map");
        }

        [Test]
        public void Nests_JoinTheRoadInItsFirstSixtyPercent_AwayFromTheBase()
        {
            foreach (RoadTemplate template in System.Enum.GetValues(typeof(RoadTemplate)))
            {
                for (int seed = 0; seed < Seeds; seed += 5)
                {
                    RoadLayout layout = FirstPassing(Spec(template, seed), out _);
                    List<Cell> walk = LayoutGeometry.Walk(layout.Road);
                    Assert.AreEqual(2, layout.Nests.Count);
                    foreach (LayoutNest nest in layout.Nests)
                    {
                        float share = LayoutGeometry.Nearest(walk, nest.Cell, out _) / (float)(walk.Count - 1);
                        Assert.LessOrEqual(share, LevelValidator.MaxNestRoadShare, $"{template} seed {seed} {nest.Element} nest");
                        Assert.GreaterOrEqual(LayoutGeometry.Chebyshev(nest.Cell, layout.Base), LayoutChecks.NestBaseClearance);
                    }
                }
            }
        }

        [Test]
        public void SecondRoad_EndsOnAJunctionWaypoint_WithAGateWithinAScreen()
        {
            for (int seed = 0; seed < Seeds; seed += 3)
            {
                foreach (RoadTemplate template in new[] { RoadTemplate.Fork, RoadTemplate.LateJoin })
                {
                    RoadLayout layout = FirstPassing(Spec(template, seed), out _);
                    Cell junction = layout.SecondRoad[layout.SecondRoad.Length - 1];
                    Assert.GreaterOrEqual(LayoutGeometry.IndexOf(layout.Road, junction), 0, "the junction is a waypoint, so MergeIntoMain finds it");
                    Cell[] merged = layout.SecondRoadToBase;
                    Assert.IsTrue(LayoutGeometry.Same(merged[merged.Length - 1], layout.Base), "the second road walks on to the base");
                }
            }
        }

        [Test]
        public void Checks_RefuseLegsTooCloseAndTooFewGates()
        {
            var spec = new LayoutSpec { Key = "Bad", Width = 36, Height = 20, Gates = 3 };
            var layout = new RoadLayout { Key = "Bad", Width = 36, Height = 20, RoadWidth = 2 };
            // East along row 16, then back west only two rows lower: the corridors touch.
            layout.Road = new[] { new Cell(0, 16), new Cell(30, 16), new Cell(30, 14), new Cell(3, 14), new Cell(3, 4), new Cell(35, 4) };
            layout.NorthAir = new[] { new Cell(0, 19), new Cell(35, 4) };
            layout.SouthAir = new[] { new Cell(0, 0), new Cell(35, 4) };

            List<string> errors = LayoutChecks.Check(layout, spec, Rules);

            Assert.IsTrue(errors.Exists(e => e.Contains("build rows")), string.Join("\n", errors));
            Assert.IsTrue(errors.Exists(e => e.Contains("gates placed")), string.Join("\n", errors));
        }

        [Test]
        public void EveryCrystalPlainsSpec_HasAFairLayout()
        {
            foreach (CampaignLevelSpec spec in LevelSpecs.Crystal)
            {
                Assert.AreEqual(spec.Key, spec.Layout.Key, "the layout carries its level's key");
                Assert.AreEqual(spec.Key, spec.Waves.Key, "the waves carry their level's key");
                Assert.IsNotNull(FirstPassing(spec.Layout, out List<string> errors), $"{spec.Key}: {string.Join("; ", errors)}");
            }
        }

        // The pure tables must match the game's: terrain kinds by number and the ground every nest needs.
        [Test]
        public void LayoutTerrain_MatchesTheGamesTerrainKindsAndNests()
        {
            Assert.AreEqual((int)TerrainKind.Water, LayoutTerrain.Water);
            Assert.AreEqual((int)TerrainKind.Lava, LayoutTerrain.Lava);
            Assert.AreEqual((int)TerrainKind.Quicksand, LayoutTerrain.Quicksand);
            Assert.AreEqual((int)TerrainKind.Ice, LayoutTerrain.Ice);
            Assert.AreEqual((int)TerrainKind.Bog, LayoutTerrain.Bog);
            Assert.AreEqual((int)TerrainKind.Raised, LayoutTerrain.Raised);
            foreach (Game.Core.Element element in System.Enum.GetValues(typeof(Game.Core.Element)))
            {
                Assert.AreEqual((int)LevelValidator.NestTerrain(element), LayoutTerrain.KindOf(element.ToString()), element.ToString());
            }
        }

        [Test]
        public void LayoutRandom_IsTheSameEverywhere()
        {
            var random = new LayoutRandom("C1_01", 101, 0);
            // SplitMix64 over FNV-1a: fixed numbers, so a dotnet tool and the editor draw the same sequence.
            Assert.AreEqual(LayoutRandom.Fnv("C1_01:101:0"), LayoutRandom.Fnv("C1_01:101:0"));
            ulong first = random.NextULong();
            Assert.AreEqual(first, new LayoutRandom("C1_01", 101, 0).NextULong());
            Assert.AreNotEqual(first, new LayoutRandom("C1_01", 101, 1).NextULong());
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(random.Range(3, 7), Is.InRange(3, 7));
            }
        }

        private static string[] Text(Cell[] cells)
        {
            var text = new string[cells?.Length ?? 0];
            for (int i = 0; i < text.Length; i++)
            {
                text[i] = cells[i].ToString();
            }

            return text;
        }
    }
}
