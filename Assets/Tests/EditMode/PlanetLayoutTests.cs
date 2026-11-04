using Game.Balance;
using Game.Editor;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// The campaign follows the planet as it was painted (layout.json 22b): Crystal Plains' level sites sit where the
    /// painted route put them, the Core is the painted crater's size, and the map's spacing rule matches the route's.
    /// </summary>
    public sealed class PlanetLayoutTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const float Tolerance = 0.01f;
        private const float CoreRadius = 14f;
        private const float PaintedSpacing = 6f;

        [Test]
        public void CrystalPlainsSites_AreThePaintedRoute_InLevelOrder()
        {
            CampaignCatalog catalog = Catalog();
            GlobePoint[] painted = PlanetLayout.Sites("C1");
            Assert.IsNotNull(painted);
            int first = catalog.FirstNode(0);
            Assert.AreEqual(painted.Length, catalog.NodeCount(0));
            for (int i = 0; i < painted.Length; i++)
            {
                MapNode node = catalog.Nodes[first + i];
                Assert.AreEqual(painted[i].Longitude, node.Longitude, Tolerance, $"site {i + 1} longitude");
                Assert.AreEqual(painted[i].Latitude, node.Latitude, Tolerance, $"site {i + 1} latitude");
            }

            Assert.AreEqual("Landing Site", catalog.Nodes[first].DisplayName);
        }

        [Test]
        public void TheCore_IsThePaintedCratersSize()
        {
            CampaignCatalog catalog = Catalog();
            bool found = false;
            foreach (MapRegion region in catalog.Regions)
            {
                if (region.Layer == MapLayer.Core)
                {
                    Assert.AreEqual(CoreRadius, region.RadiusDegrees, Tolerance);
                    found = true;
                }
            }

            Assert.IsTrue(found, "no Core region");
        }

        [Test]
        public void SpacingRule_MatchesThePaintedRoute()
        {
            Assert.AreEqual(PaintedSpacing, PlanetCoordinates.MinimumSpacingDegrees);
            Assert.AreEqual(PaintedSpacing, NodePlacer.MinimumSpacingDegrees);
            GlobePoint[] painted = PlanetLayout.Sites("C1");
            for (int i = 0; i < painted.Length; i++)
            {
                for (int k = 0; k < i; k++)
                {
                    Assert.GreaterOrEqual(GlobePoint.AngleBetween(painted[i], painted[k]), PaintedSpacing, $"sites {k + 1} and {i + 1}");
                }
            }
        }

        private static CampaignCatalog Catalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            return catalog;
        }
    }
}
