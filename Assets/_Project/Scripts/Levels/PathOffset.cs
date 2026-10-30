using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Shifts a route sideways by a fixed distance, so enemies spread over the width of the road instead of walking
    /// one and the same line. Corners are mitred: the shifted corner lies on both neighbouring shifted segments, so
    /// an enemy keeps its distance from the centre line all the way round a bend.
    /// </summary>
    public static class PathOffset
    {
        // Past this the mitre of a hairpin would shoot off to infinity, so such a corner is only pushed along its bisector.
        private const float SharpestMitreDot = -0.6f;

        public static Vector3[] Shift(Vector3[] points, float distance)
        {
            var result = new Vector3[points.Length];
            if (points.Length < 2 || Mathf.Approximately(distance, 0f))
            {
                points.CopyTo(result, 0);
                return result;
            }

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 incoming = i > 0 ? Flat(points[i] - points[i - 1]) : Vector3.zero;
                Vector3 outgoing = i < points.Length - 1 ? Flat(points[i + 1] - points[i]) : Vector3.zero;
                if (incoming == Vector3.zero)
                {
                    incoming = outgoing;
                }

                if (outgoing == Vector3.zero)
                {
                    outgoing = incoming;
                }

                // Right-hand normals of both segments: positive distances shift to the right of the direction of travel.
                Vector3 normalIn = Vector3.Cross(Vector3.up, incoming);
                Vector3 normalOut = Vector3.Cross(Vector3.up, outgoing);
                float dot = Vector3.Dot(normalIn, normalOut);
                Vector3 bisector = normalIn + normalOut;
                Vector3 shift = dot > SharpestMitreDot
                    ? bisector / (1f + dot) * distance
                    : bisector.normalized * distance;
                result[i] = points[i] + shift;
            }

            return result;
        }

        private static Vector3 Flat(Vector3 step)
        {
            step.y = 0f;
            return step.sqrMagnitude > 0.000001f ? step.normalized : Vector3.zero;
        }
    }
}
