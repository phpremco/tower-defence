using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Where a warning marker may sit on the HUD (round 88b): an edge marker (an arrow with its word, one block) goes to
    /// the point of the frame nearest to where its direction leaves the screen that lies inside the safe area and
    /// touches no obstacle (a HUD panel, another marker); a ring over a place in view keeps its place and only its word
    /// flips above or below. A marker whose direction runs into a panel therefore slides along that panel's free border
    /// instead of covering it. Pure geometry in one space (the safe area's), no Unity objects; the buffers are reused,
    /// so a call allocates nothing once they have grown.
    /// </summary>
    public sealed class MarkerLayout
    {
        // A block pushed to an obstacle's edge must not count as touching it through float error.
        private const float Clearance = 0.5f;
        // A block laid against the frame's own edge is inside it, whatever the last float bit says.
        private const float Tolerance = 0.01f;
        private const float MinDirection = 1f;

        private readonly List<float> _xs = new List<float>();
        private readonly List<float> _ys = new List<float>();

        /// <summary>True when a block with this rect lies in the area and touches no obstacle.</summary>
        public static bool IsFree(Rect block, Rect area, IReadOnlyList<Rect> obstacles)
        {
            if (block.xMin < area.xMin - Tolerance || block.xMax > area.xMax + Tolerance || block.yMin < area.yMin - Tolerance || block.yMax > area.yMax + Tolerance)
            {
                return false;
            }

            for (int i = 0; i < obstacles.Count; i++)
            {
                if (block.Overlaps(obstacles[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The centre nearest to <paramref name="ideal"/> at which a block of this size is free. The free space is bounded
        /// by straight edges (the area's and the obstacles', each grown by half the block), so the nearest free point has
        /// its x and its y each on the ideal's own or on one of those edges: every such pair is tried. False when the
        /// block fits nowhere.
        /// </summary>
        public bool Nearest(Vector2 ideal, Vector2 size, Rect area, IReadOnlyList<Rect> obstacles, out Vector2 centre)
        {
            centre = ideal;
            Vector2 half = size * 0.5f;
            Rect inner = Rect.MinMaxRect(area.xMin + half.x, area.yMin + half.y, area.xMax - half.x, area.yMax - half.y);
            if (inner.width < 0f || inner.height < 0f)
            {
                return false;
            }

            _xs.Clear();
            _ys.Clear();
            _xs.Add(Mathf.Clamp(ideal.x, inner.xMin, inner.xMax));
            _xs.Add(inner.xMin);
            _xs.Add(inner.xMax);
            _ys.Add(Mathf.Clamp(ideal.y, inner.yMin, inner.yMax));
            _ys.Add(inner.yMin);
            _ys.Add(inner.yMax);
            for (int i = 0; i < obstacles.Count; i++)
            {
                Rect obstacle = obstacles[i];
                AddInside(_xs, obstacle.xMin - half.x - Clearance, inner.xMin, inner.xMax);
                AddInside(_xs, obstacle.xMax + half.x + Clearance, inner.xMin, inner.xMax);
                AddInside(_ys, obstacle.yMin - half.y - Clearance, inner.yMin, inner.yMax);
                AddInside(_ys, obstacle.yMax + half.y + Clearance, inner.yMin, inner.yMax);
            }

            float best = float.PositiveInfinity;
            for (int ix = 0; ix < _xs.Count; ix++)
            {
                for (int iy = 0; iy < _ys.Count; iy++)
                {
                    var candidate = new Vector2(_xs[ix], _ys[iy]);
                    float distance = (candidate - ideal).sqrMagnitude;
                    if (distance < best && IsFree(new Rect(candidate - half, size), area, obstacles))
                    {
                        best = distance;
                        centre = candidate;
                    }
                }
            }

            return !float.IsPositiveInfinity(best);
        }

        /// <summary>
        /// An edge marker towards <paramref name="target"/> (a point in the area's space, on or off screen): the block of
        /// the arrow (a square of <paramref name="arrowBox"/>, wide enough for the arrow at any turn) and the word, the
        /// word on the side away from the edge, as near as it can be to where the line from the middle to the target
        /// leaves the frame. The arrow turns towards the target from where it ends up.
        /// </summary>
        public bool PlaceEdge(Vector2 target, float arrowBox, Vector2 labelSize, float gap, Rect area, IReadOnlyList<Rect> obstacles, out EdgeMarkerPlacement placed)
        {
            var size = new Vector2(Mathf.Max(arrowBox, labelSize.x), arrowBox + gap + labelSize.y);
            Vector2 half = size * 0.5f;
            Vector2 direction = target - area.center;
            if (direction.sqrMagnitude < MinDirection)
            {
                direction = Vector2.up;
            }

            // A target inside the frame (in view, but under a panel) is its own ideal place; one outside is pulled in along its line.
            float reachX = (area.width * 0.5f - half.x) / Mathf.Max(0.001f, Mathf.Abs(direction.x));
            float reachY = (area.height * 0.5f - half.y) / Mathf.Max(0.001f, Mathf.Abs(direction.y));
            Vector2 ideal = area.center + direction * Mathf.Max(0f, Mathf.Min(1f, Mathf.Min(reachX, reachY)));
            if (!Nearest(ideal, size, area, obstacles, out Vector2 centre))
            {
                placed = default;
                return false;
            }

            // Pointing down the arrow sits under its word, pointing up over it: the arrow is always the part nearest the edge.
            float side = direction.y <= 0f ? 1f : -1f;
            Vector2 arrow = centre - new Vector2(0f, side * (half.y - arrowBox * 0.5f));
            Vector2 label = centre + new Vector2(0f, side * (half.y - labelSize.y * 0.5f));
            Vector2 aim = target - arrow;
            if (aim.sqrMagnitude < MinDirection)
            {
                aim = direction;
            }

            placed = new EdgeMarkerPlacement(arrow, label, new Rect(centre - half, size), Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            return true;
        }

        /// <summary>
        /// The word of a ring in view: over the ring when that is free, else under it, else the free place nearest to
        /// over it. The ring itself never moves: it marks a place on the field.
        /// </summary>
        public bool PlaceRingLabel(Vector2 ring, float offset, Vector2 labelSize, Rect area, IReadOnlyList<Rect> obstacles, out Vector2 label)
        {
            Vector2 half = labelSize * 0.5f;
            label = ring + Vector2.up * offset;
            if (IsFree(new Rect(label - half, labelSize), area, obstacles))
            {
                return true;
            }

            Vector2 under = ring - Vector2.up * offset;
            if (IsFree(new Rect(under - half, labelSize), area, obstacles))
            {
                label = under;
                return true;
            }

            return Nearest(label, labelSize, area, obstacles, out label);
        }

        private static void AddInside(List<float> values, float value, float min, float max)
        {
            if (value >= min && value <= max)
            {
                values.Add(value);
            }
        }
    }
}
