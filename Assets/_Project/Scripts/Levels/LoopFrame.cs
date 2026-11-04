using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A closed loop of points read by distance along it: the point, the walking direction and the side to its left, at
    /// any distance (wrapped round). The bank sweep bends its segments onto this frame. Pure maths on world x/z.
    /// </summary>
    public sealed class LoopFrame
    {
        private readonly Vector2[] _points;
        private readonly float[] _distances;

        public LoopFrame(Vector2[] points)
        {
            _points = points;
            _distances = new float[points.Length + 1];
            for (int i = 0; i < points.Length; i++)
            {
                _distances[i + 1] = _distances[i] + Vector2.Distance(points[i], points[(i + 1) % points.Length]);
            }

            Perimeter = _distances[points.Length];
        }

        public float Perimeter { get; }

        /// <summary>The loop at <paramref name="distance"/> from its first point: position, unit direction of travel and the unit side to its left.</summary>
        public void Sample(float distance, out Vector2 point, out Vector2 along, out Vector2 left)
        {
            float u = Mathf.Repeat(distance, Perimeter);
            int low = 0;
            int high = _points.Length - 1;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (_distances[mid] <= u)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            int next = (low + 1) % _points.Length;
            float span = _distances[low + 1] - _distances[low];
            float t = span > 1e-6f ? (u - _distances[low]) / span : 0f;
            point = Vector2.Lerp(_points[low], _points[next], t);

            // The direction blends from the segment before to the segment after, so a bend never shows a kink.
            Vector2 here = Direction(low);
            Vector2 blended = t < 0.5f ? Vector2.Lerp(Direction(low - 1), here, t + 0.5f) : Vector2.Lerp(here, Direction(low + 1), t - 0.5f);
            along = blended.sqrMagnitude > 1e-10f ? blended.normalized : here;
            left = new Vector2(-along.y, along.x);
        }

        private Vector2 Direction(int index)
        {
            int count = _points.Length;
            int a = ((index % count) + count) % count;
            Vector2 d = _points[(a + 1) % count] - _points[a];
            return d.sqrMagnitude > 1e-10f ? d.normalized : Vector2.right;
        }
    }
}
