using System;
using Game.Balance;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Every element and terrain kind is handled by the tables that turn data into ground, nests and colour, so a newly
    /// appended element (Stone in S4) cannot be forgotten in one of them.
    /// </summary>
    public sealed class ElementCoverageTests
    {
        // Elements whose creatures nest in plain ground rather than a liquid of their own.
        private static readonly Element[] DryElements = { Element.None, Element.Wood };

        [Test]
        public void EveryElementWithATerrain_HasItsNestTerrainInBothTables()
        {
            foreach (Element element in Enum.GetValues(typeof(Element)))
            {
                TerrainKind nest = LevelValidator.NestTerrain(element);
                bool dry = Array.IndexOf(DryElements, element) >= 0;
                Assert.AreEqual(!dry, TerrainMap.IsLiquid(nest), $"{element} nests in {nest}");
                if (element != Element.None)
                {
                    Assert.AreEqual((int)nest, LayoutTerrain.KindOf(element.ToString()), $"the layout generator maps {element} to another kind than the validator");
                }
            }
        }

        [Test]
        public void EveryElement_HasANestColour()
        {
            foreach (Element element in Enum.GetValues(typeof(Element)))
            {
                if (element != Element.None)
                {
                    Assert.AreNotEqual(UnityEngine.Color.white, LevelVisualBuilder.NestColor(element), $"{element} has no nest colour");
                }
            }
        }

        [Test]
        public void EveryTerrainKind_MatchesTheLayoutTable_AndEveryLiquidHasASurface()
        {
            float sand = TerrainLiquidBuilder.SurfaceLevel(TerrainKind.Quicksand);
            int highest = 0;
            foreach (TerrainKind kind in Enum.GetValues(typeof(TerrainKind)))
            {
                highest = Math.Max(highest, (int)kind);
                Assert.AreEqual(TerrainMap.IsLiquid(kind), LayoutTerrain.IsLiquid((int)kind), $"{kind}: the layout and the map disagree on whether it holds a tower");
                if (TerrainMap.IsLiquid(kind) && kind != TerrainKind.Quicksand)
                {
                    // A kind without a case of its own falls back to the quicksand surface.
                    Assert.AreNotEqual(sand, TerrainLiquidBuilder.SurfaceLevel(kind), $"{kind} has no liquid surface of its own");
                }
            }

            Assert.AreEqual(highest, LayoutTerrain.Last, "the layout generator's kind table is too short");
            Assert.AreEqual((int)TerrainKind.Rift, LayoutTerrain.Rift);
        }

        [Test]
        public void StoneNest_SendsItsCreaturesOutOfTheRift()
        {
            Assert.AreEqual(EnemyOrigin.Rift, EnemySpawner.NestOrigin(Element.Stone), "a stone nest sits in a rift");
            foreach (Element element in Enum.GetValues(typeof(Element)))
            {
                if (LevelValidator.NestTerrain(element) != TerrainKind.Rift)
                {
                    Assert.AreEqual(EnemyOrigin.Nest, EnemySpawner.NestOrigin(element), $"{element} nest");
                }
            }
        }
    }
}
