using System.Collections.Generic;
using Game.Bosses;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The exact grid walk behind the burrow route checks (round 20 GravemawArena flake).</summary>
    public sealed class GridLineTests
    {
        // The arena grid: 40 x 22 cells of one metre, centred on the origin.
        private static readonly GridMap Grid = new GridMap(new Vector2Int(40, 22), 1f);

        [Test]
        public void Touches_TheCellWhoseCornerTheLineOnlyClips()
        {
            // The burrow leg of the failing run: it passes cell (33, 15) only between x 13.95 and 14.0, which a 0.25 m
            // sampling never lands in.
            var a = new Vector3(8.16f, 0f, 8.44f);
            var b = new Vector3(16.5f, 0f, 3.5f);
            var tower = new Vector2Int(33, 15);
            Assert.IsTrue(GridLine.Touches(Grid, a, b, c => c == tower));
        }

        [Test]
        public void Visits_EveryCellASampledWalkFinds_AndStaysOnTheLine()
        {
            var a = new Vector3(-7.3f, 0f, 2.2f);
            var b = new Vector3(5.9f, 0f, -4.6f);
            var visited = new HashSet<Vector2Int>();
            GridLine.Touches(Grid, a, b, c =>
            {
                visited.Add(c);
                return false;
            });

            for (int i = 0; i <= 2000; i++)
            {
                Assert.IsTrue(visited.Contains(Grid.WorldToCell(Vector3.Lerp(a, b, i / 2000f))), $"missed a cell at sample {i}");
            }

            // A cell walk of a line crosses at most one boundary per cell along each axis.
            Vector2Int from = Grid.WorldToCell(a);
            Vector2Int to = Grid.WorldToCell(b);
            Assert.LessOrEqual(visited.Count, Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y) + 1);
        }

        [Test]
        public void ThroughACorner_CountsBothCellsBesideIt()
        {
            // From the centre of (10, 10) to the centre of (12, 12): exactly through the corners (10.5, 10.5) and (11.5, 11.5).
            Vector3 a = Grid.CellToWorld(new Vector2Int(10, 10));
            Vector3 b = Grid.CellToWorld(new Vector2Int(12, 12));
            Assert.IsTrue(GridLine.Touches(Grid, a, b, c => c == new Vector2Int(11, 10)));
            Assert.IsTrue(GridLine.Touches(Grid, a, b, c => c == new Vector2Int(10, 11)));
            Assert.IsFalse(GridLine.Touches(Grid, a, b, c => c == new Vector2Int(12, 10)));
        }
    }
}
