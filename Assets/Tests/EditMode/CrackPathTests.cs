using System.Collections.Generic;
using Game.Bosses;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The shape of a burrow crack (Remco 2026-11-05: cracks overlapped): a grid staircase is pulled straight, a path that
    /// turns back on itself loses the fold, corners are rounded with the ends kept, and the distance checks see two
    /// cracks that touch.
    /// </summary>
    public sealed class CrackPathTests
    {
        private static List<Vector3> Staircase(int steps)
        {
            var points = new List<Vector3>();
            for (int i = 0; i < steps; i++)
            {
                points.Add(new Vector3(i, 0f, i));
                points.Add(new Vector3(i + 1, 0f, i));
            }

            points.Add(new Vector3(steps, 0f, steps));
            return points;
        }

        [Test]
        public void Straighten_OpenGround_LeavesTheEndsOnly()
        {
            List<Vector3> points = Staircase(6);
            CrackPath.Straighten(points, (a, b) => true);
            Assert.AreEqual(2, points.Count);
            Assert.AreEqual(Vector3.zero, points[0]);
            Assert.AreEqual(new Vector3(6f, 0f, 6f), points[1]);
        }

        [Test]
        public void Straighten_KeepsTheCornerRoundAnObstacle()
        {
            var points = new List<Vector3> { Vector3.zero, new Vector3(5f, 0f, 0f), new Vector3(5f, 0f, 5f) };
            // Nothing may cut the corner (a tower stands inside it).
            CrackPath.Straighten(points, (a, b) => Mathf.Approximately(a.x, b.x) || Mathf.Approximately(a.z, b.z));
            Assert.AreEqual(3, points.Count);
        }

        [Test]
        public void RemoveBacktracks_DropsTheFold()
        {
            var points = new List<Vector3> { Vector3.zero, new Vector3(4f, 0f, 0f), new Vector3(3f, 0f, 0.2f), new Vector3(3f, 0f, 5f) };
            CrackPath.RemoveBacktracks(points, 0.3f);
            Assert.IsFalse(CrackPath.SelfOverlaps(points, 1.5f, 3f));
            Assert.AreEqual(Vector3.zero, points[0]);
            Assert.AreEqual(new Vector3(3f, 0f, 5f), points[points.Count - 1]);
        }

        [Test]
        public void Smooth_RoundsCorners_KeepsEnds_AndNeverFoldsAWideStrip()
        {
            var points = new List<Vector3> { Vector3.zero, new Vector3(6f, 0f, 0f), new Vector3(6f, 0f, 6f), new Vector3(12f, 0f, 6f) };
            CrackPath.Smooth(points, 1.2f);
            Assert.Greater(points.Count, 4);
            Assert.AreEqual(Vector3.zero, points[0]);
            Assert.AreEqual(new Vector3(12f, 0f, 6f), points[points.Count - 1]);
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 a = (points[i] - points[i - 1]).normalized;
                Vector3 b = (points[i + 1] - points[i]).normalized;
                Assert.Greater(Vector3.Dot(a, b), 0.6f, $"sharp turn at {i}");
            }
        }

        [Test]
        public void Distance_SeesCrossingAndParallelCracks()
        {
            var a = new List<Vector3> { Vector3.zero, new Vector3(10f, 0f, 0f) };
            var crossing = new List<Vector3> { new Vector3(5f, 0f, -5f), new Vector3(5f, 0f, 5f) };
            var parallel = new List<Vector3> { new Vector3(0f, 0f, 2f), new Vector3(10f, 0f, 2f) };
            var far = new List<Vector3> { new Vector3(0f, 0f, 8f), new Vector3(10f, 0f, 8f) };
            Assert.AreEqual(0f, CrackPath.Distance(a, crossing), 0.001f);
            Assert.AreEqual(2f, CrackPath.Distance(a, parallel), 0.001f);
            Assert.Greater(CrackPath.Distance(a, far), 3.5f);
        }

        [Test]
        public void SelfOverlaps_SeesAUTurn()
        {
            var u = new List<Vector3> { Vector3.zero, new Vector3(8f, 0f, 0f), new Vector3(8f, 0f, 1f), new Vector3(0f, 0f, 1f) };
            var l = new List<Vector3> { Vector3.zero, new Vector3(8f, 0f, 0f), new Vector3(8f, 0f, 8f) };
            Assert.IsTrue(CrackPath.SelfOverlaps(u, 3f, 2f));
            Assert.IsFalse(CrackPath.SelfOverlaps(l, 3f, 2f));
        }

        [Test]
        public void Cover_MarksTheCellsUnderTheCrack()
        {
            var cells = new HashSet<Vector2Int>();
            var path = new List<Vector3> { Vector3.zero, new Vector3(5f, 0f, 0f) };
            CrackPath.Cover(path, 1.5f, 1f, p => new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z)), cells);
            Assert.IsTrue(cells.Contains(new Vector2Int(3, 0)));
            Assert.IsTrue(cells.Contains(new Vector2Int(3, 1)));
            Assert.IsFalse(cells.Contains(new Vector2Int(3, 4)));
        }
    }
}
