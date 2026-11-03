using System.Collections.Generic;
using Game.Balance;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Level sites on the globe: twenty per surface continent, inside the land, apart enough to click, none in the Core.</summary>
    public sealed class NodePlacementTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const int LevelsPerContinent = 20;

        [Test]
        public void Route_LaysTwentySitesInsideTheCap_AtLeastEightDegreesApart()
        {
            // The continents of the story, the Crowns near their poles included.
            float[][] continents = { new[] { -40f, 6f }, new[] { 60f, -15f }, new[] { 60f, 62f }, new[] { -140f, 15f }, new[] { 150f, -60f } };
            foreach (float[] centre in continents)
            {
                GlobePoint middle = new GlobePoint(centre[0], centre[1]);
                GlobePoint[] sites = NodePlacer.Route(centre[0], centre[1], 30f);
                Assert.AreEqual(LevelsPerContinent, sites.Length);
                for (int i = 0; i < sites.Length; i++)
                {
                    Assert.LessOrEqual(GlobePoint.AngleBetween(sites[i], middle), 30f - NodePlacer.CoastInsetDegrees + 0.01f, $"site {i} of {middle}");
                    for (int k = 0; k < i; k++)
                    {
                        Assert.GreaterOrEqual(GlobePoint.AngleBetween(sites[i], sites[k]), PlanetCoordinates.MinimumSpacingDegrees, $"sites {k} and {i} of {middle}");
                    }
                }
            }
        }

        [Test]
        public void Route_StartsInTheSouthWest_AndWindsInEvenSteps()
        {
            GlobePoint[] sites = NodePlacer.Route(-40f, 6f, 30f);
            Assert.Less(sites[0].Longitude, -40f, "the first site lies west of the middle");
            Assert.Less(sites[0].Latitude, 6f, "and south of it");
            Assert.Greater(sites[sites.Length - 1].Latitude, 6f, "the route ends in the north");
            Assert.Greater(sites[4].Longitude, sites[0].Longitude, "the first leg runs east");

            double shortest = double.MaxValue;
            double longest = 0.0;
            for (int i = 1; i < sites.Length; i++)
            {
                double step = GlobePoint.AngleBetween(sites[i - 1], sites[i]);
                shortest = System.Math.Min(shortest, step);
                longest = System.Math.Max(longest, step);
            }

            Assert.Less(longest / shortest, 1.3, "consecutive levels sit at about equal steps along the route");

            // Not a grid: the sites do not line up in five columns.
            var columns = new HashSet<int>();
            foreach (GlobePoint site in sites)
            {
                columns.Add(Mathf.RoundToInt(site.Longitude));
            }

            Assert.Greater(columns.Count, NodePlacer.Columns * 2, "the route winds instead of forming columns");
        }

        [Test]
        public void Route_IsDeterministic()
        {
            CollectionAssert.AreEqual(NodePlacer.Route(-40f, 6f, 30f, 20, 0f, Ridge), NodePlacer.Route(-40f, 6f, 30f, 20, 0f, Ridge));
        }

        [Test]
        public void Route_KeepsOffRuggedGround()
        {
            GlobePoint[] plain = NodePlacer.Route(-40f, 6f, 30f);
            GlobePoint[] avoiding = NodePlacer.Route(-40f, 6f, 30f, 20, 0f, Ridge);

            Assert.Less(Cost(avoiding), Cost(plain), "with a ridge on the map the route moves off it");
            for (int i = 0; i < avoiding.Length; i++)
            {
                for (int k = 0; k < i; k++)
                {
                    Assert.GreaterOrEqual(GlobePoint.AngleBetween(avoiding[i], avoiding[k]), PlanetCoordinates.MinimumSpacingDegrees, $"sites {k} and {i}");
                }
            }
        }

        // A mountain chain running north-north-east through the east half of the continent.
        private static float Ridge(GlobePoint place)
        {
            float across = (place.Longitude + 32f) - (place.Latitude - 6f) * 0.3f;
            return Mathf.Exp(-across * across / 8f);
        }

        private static float Cost(GlobePoint[] sites)
        {
            float sum = 0f;
            foreach (GlobePoint site in sites)
            {
                sum += Ridge(site);
            }

            return sum;
        }

        [Test]
        public void GeneratedCampaign_HasZeroOrTwentySitesPerSurfaceRegion_AndNoneInTheCore()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            Assert.AreEqual(6, catalog.Regions.Count, "five surface continents and the Core");
            int core = 0;
            for (int r = 0; r < catalog.Regions.Count; r++)
            {
                MapRegion region = catalog.Regions[r];
                Assert.IsFalse(string.IsNullOrEmpty(region.Key), $"region {r} has a key");
                int count = catalog.NodeCount(r);
                if (region.Layer == MapLayer.Core)
                {
                    core++;
                    Assert.AreEqual(0, count, "the Core holds no sites on the surface map yet");
                    continue;
                }

                Assert.IsTrue(count == 0 || count == LevelsPerContinent, $"{region.DisplayName} holds {count} sites");
            }

            Assert.AreEqual(1, core);
            Assert.AreEqual(LevelsPerContinent, catalog.NodeCount(0), "Crystal Plains is laid out in full");
            MapRegion plains = catalog.Regions[0];
            MapNode first = catalog.Nodes[catalog.FirstNode(0)];
            Assert.Less(first.Longitude, plains.Longitude, "level 1 lies west of the middle of Crystal Plains");
            Assert.Less(first.Latitude, plains.Latitude, "and south of it");
        }
    }
}
