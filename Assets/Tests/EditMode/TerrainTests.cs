using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class TerrainTests
    {
        [Test]
        public void KindAt_ReturnsPatchKind_AndGroundElsewhere()
        {
            var map = new TerrainMap(new[]
            {
                new TerrainPatch { Cell = new Vector2Int(2, 3), Kind = TerrainKind.Water },
                new TerrainPatch { Cell = new Vector2Int(4, 4), Kind = TerrainKind.Raised },
            });

            Assert.AreEqual(TerrainKind.Water, map.KindAt(new Vector2Int(2, 3)));
            Assert.AreEqual(TerrainKind.Ground, map.KindAt(new Vector2Int(0, 0)));
            Assert.IsTrue(map.IsLiquid(new Vector2Int(2, 3)));
            Assert.IsFalse(map.IsLiquid(new Vector2Int(4, 4)));
        }

        [Test]
        public void HeightAt_IsRaisedOnlyOnRaisedGround()
        {
            var map = new TerrainMap(new[] { new TerrainPatch { Cell = new Vector2Int(1, 1), Kind = TerrainKind.Raised } });

            Assert.AreEqual(TerrainMap.PlateauHeight, map.HeightAt(new Vector2Int(1, 1)));
            Assert.AreEqual(0f, map.HeightAt(new Vector2Int(0, 0)));
        }

        [TestCase(TerrainKind.Water, true)]
        [TestCase(TerrainKind.Lava, true)]
        [TestCase(TerrainKind.Quicksand, true)]
        [TestCase(TerrainKind.Raised, false)]
        [TestCase(TerrainKind.Ground, false)]
        public void IsLiquid_MatchesKind(TerrainKind kind, bool expected)
        {
            Assert.AreEqual(expected, TerrainMap.IsLiquid(kind));
        }
    }
}
