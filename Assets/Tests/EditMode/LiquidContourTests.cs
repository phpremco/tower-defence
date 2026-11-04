using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The round pool outlines (README round 24b): smooth, close to the cells, and the sheet and sweep follow them.</summary>
    public sealed class LiquidContourTests
    {
        private const float CellSize = 1f;

        // No piece of the shore may run straight along a grid axis for longer than this: a staircase of cell edges would.
        private const float MaxAxisRun = 0.3f;

        // Between two points 0.05 apart the shore may turn this much at most: a cell corner turns 90 degrees at once.
        private const float MaxTurnDegrees = 12f;
        private const float AxisToleranceDegrees = 1f;
        private const float MaxDriftFromCells = 0.32f;

        private static readonly GridMap Grid = new GridMap(new Vector2Int(20, 20), CellSize);

        // An L of cells with a notch: convex and concave corners, long straight cell runs.
        private static readonly Vector2Int[] LShape =
        {
            new Vector2Int(5, 5), new Vector2Int(6, 5), new Vector2Int(7, 5), new Vector2Int(8, 5), new Vector2Int(9, 5),
            new Vector2Int(5, 6), new Vector2Int(6, 6), new Vector2Int(7, 6), new Vector2Int(8, 6), new Vector2Int(9, 6),
            new Vector2Int(5, 7), new Vector2Int(6, 7), new Vector2Int(5, 8), new Vector2Int(6, 8), new Vector2Int(5, 9), new Vector2Int(6, 9),
        };

        [Test]
        public void Smoothed_DiagonalPool_HasNoAxisAlignedStaircase()
        {
            // A band of cells along the diagonal: its cell outline is a staircase of one-cell steps on both sides.
            var band = new List<Vector2Int>();
            for (int i = 2; i <= 16; i++)
            {
                for (int j = i - 1; j <= i + 1; j++)
                {
                    band.Add(new Vector2Int(i, j));
                }
            }

            List<Vector2[]> loops = LiquidContour.Build(band, Grid, 11);

            Assert.AreEqual(1, loops.Count);
            Vector2[] loop = loops[0];
            float run = 0f;
            float longest = 0f;
            for (int i = 0; i < loop.Length; i++)
            {
                Vector2 a = loop[(i + 1) % loop.Length] - loop[i];
                // Only the middle of the band: its two ends are short straight caps, not steps.
                bool middle = Mathf.Abs(loop[i].x) < 3.5f;
                run = middle && IsAxisAligned(a) ? run + a.magnitude : 0f;
                longest = Mathf.Max(longest, run);
            }

            Assert.Less(longest, MaxAxisRun, $"the shore runs {longest:0.00} straight along a grid axis: the steps survived");
            Assert.Less(SharpestTurn(loop), MaxTurnDegrees);
        }

        [Test]
        public void Smoothed_LShapedPool_HasNoSquareCorner()
        {
            List<Vector2[]> loops = LiquidContour.Build(LShape, Grid, 11);

            Assert.AreEqual(1, loops.Count);
            float sharpest = SharpestTurn(loops[0]);
            Assert.Less(sharpest, MaxTurnDegrees, $"the shore turns {sharpest:0.0} degrees between two points");
        }

        private static float SharpestTurn(Vector2[] loop)
        {
            float sharpest = 0f;
            for (int i = 0; i < loop.Length; i++)
            {
                Vector2 a = loop[(i + 1) % loop.Length] - loop[i];
                Vector2 b = loop[(i + 2) % loop.Length] - loop[(i + 1) % loop.Length];
                sharpest = Mathf.Max(sharpest, Vector2.Angle(a, b));
            }

            return sharpest;
        }

        [Test]
        public void Smoothed_Outline_StaysCloseToTheCells()
        {
            Vector2[] raw = LiquidContour.CellOutlines(LShape, Grid)[0];
            Vector2[] smooth = LiquidContour.Build(LShape, Grid, 3)[0];

            foreach (Vector2 point in smooth)
            {
                Assert.Less(DistanceToLoop(raw, point), MaxDriftFromCells, $"shore point {point} strays from the cells");
            }

            Assert.Greater(LiquidContour.SignedArea(smooth), 0f, "the outer shore walks counter-clockwise (liquid on the left)");
        }

        [Test]
        public void CellOutlines_KeepDiagonalNeighboursApart_AndFindIslands()
        {
            var diagonal = new[] { new Vector2Int(2, 2), new Vector2Int(3, 3) };
            Assert.AreEqual(2, LiquidContour.CellOutlines(diagonal, Grid).Count);

            var ring = new List<Vector2Int>();
            for (int x = 2; x <= 6; x++)
            {
                for (int y = 2; y <= 6; y++)
                {
                    if (x != 4 || y != 4)
                    {
                        ring.Add(new Vector2Int(x, y));
                    }
                }
            }

            List<Vector2[]> loops = LiquidContour.CellOutlines(ring, Grid);
            Assert.AreEqual(2, loops.Count);
            Assert.AreEqual(1, CountNegative(loops), "the dry island in the middle is a clockwise loop");
        }

        [Test]
        public void SignedDistance_IsPositiveInTheLiquid_AndNegativeOnDryGround()
        {
            var shape = new LiquidShape(TerrainKind.Water, LShape, Grid, 5);

            Assert.Greater(shape.SignedDistance(Centre(5, 5).x, Centre(5, 5).y), 0.2f);
            Assert.Greater(shape.SignedDistance(Centre(7, 6).x, Centre(7, 6).y), 0.2f);
            Assert.Less(shape.SignedDistance(Centre(8, 8).x, Centre(8, 8).y), -0.2f, "the notch of the L is dry");
            Assert.AreEqual(-LiquidShape.Reach, shape.SignedDistance(Centre(15, 15).x, Centre(15, 15).y), 1e-4f);
        }

        [Test]
        public void SurfaceMesh_EndsJustPastTheShore_AndCarriesTheShoreDistance()
        {
            var shape = new LiquidShape(TerrainKind.Water, LShape, Grid, 5);
            Mesh mesh = LiquidSurfaceMesh.Build(shape);
            var shore = new List<Vector2>();
            mesh.GetUVs(1, shore);

            Assert.Greater(mesh.triangles.Length, 0);
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = shape.SignedDistance(vertices[i].x, vertices[i].z);
                Assert.GreaterOrEqual(distance, -LiquidSurfaceMesh.OuterReach - 0.02f, $"sheet vertex {vertices[i]} lies far outside the shore");
                Assert.AreEqual(distance - LiquidSurfaceMesh.ShelfInset, shore[i].x, 1e-4f);
            }

            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void LoopFrame_LeftSide_PointsIntoACounterClockwiseLoop()
        {
            Vector2[] loop = LiquidContour.Build(LShape, Grid, 5)[0];
            var frame = new LoopFrame(loop);
            var shape = new LiquidShape(TerrainKind.Water, LShape, Grid, 5);

            for (float u = 0f; u < frame.Perimeter; u += 0.37f)
            {
                frame.Sample(u, out Vector2 point, out Vector2 _, out Vector2 left);
                Vector2 probe = point + left * 0.1f;
                Assert.Greater(shape.SignedDistance(probe.x, probe.y), 0f, $"left of the walk at {u:0.00} is not the liquid");
            }
        }

        private static bool IsAxisAligned(Vector2 step)
        {
            float angle = Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg;
            float fromAxis = Mathf.Abs(Mathf.DeltaAngle(angle, Mathf.Round(angle / 90f) * 90f));
            return fromAxis < AxisToleranceDegrees;
        }

        private static float DistanceToLoop(Vector2[] loop, Vector2 point)
        {
            float best = float.MaxValue;
            for (int i = 0; i < loop.Length; i++)
            {
                Vector2 a = loop[i];
                Vector2 ab = loop[(i + 1) % loop.Length] - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(point, a + ab * t));
            }

            return best;
        }

        private static int CountNegative(List<Vector2[]> loops)
        {
            int count = 0;
            foreach (Vector2[] loop in loops)
            {
                count += LiquidContour.SignedArea(loop) < 0f ? 1 : 0;
            }

            return count;
        }

        private static Vector2 Centre(int x, int y)
        {
            Vector3 world = Grid.CellToWorld(new Vector2Int(x, y));
            return new Vector2(world.x, world.z);
        }
    }
}
