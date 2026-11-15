using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The way a flyer really flies its route (round 65, Remco: flyers turned on the spot at every waypoint): each corner
    /// of the route is cut by an arc, so the heading comes round at a steady rate instead of snapping. The arc's radius
    /// is what the creature's speed and turn rate allow (it cannot turn faster than its data says), drawn in where the
    /// legs on either side are too short for it. Pure: the spawner rounds a route once when a flyer is launched, and the
    /// balance model measures the same curve.
    /// </summary>
    public static class FlightCurve
    {
        // A corner flatter than this is flown straight through.
        private const float MinTurnDegrees = 2f;
        // One point of the arc for every so many degrees of turn: small enough that the steps do not show.
        private const float DegreesPerPoint = 12f;
        // Each corner may use at most this share of the leg it shares with the next corner, so two arcs never overlap.
        private const float LegShare = 0.5f;

        /// <summary>The radius a flyer turns on: its speed over its turn rate (degrees per second), never below the floor.</summary>
        public static float Radius(float speed, float turnDegreesPerSecond, float minRadius) =>
            turnDegreesPerSecond <= 0f ? minRadius : Mathf.Max(minRadius, speed / (turnDegreesPerSecond * Mathf.Deg2Rad));

        /// <summary>
        /// The route with every corner rounded on <paramref name="radius"/>. The first and the last point stay; the
        /// height of a point on an arc follows the legs it joins.
        /// </summary>
        public static Vector3[] Round(IReadOnlyList<Vector3> route, float radius)
        {
            var result = new List<Vector3>(route.Count * 4);
            if (route.Count > 0)
            {
                result.Add(route[0]);
            }

            for (int i = 1; i < route.Count - 1; i++)
            {
                AddCorner(route[i - 1], route[i], route[i + 1], radius, result);
            }

            if (route.Count > 1)
            {
                result.Add(route[route.Count - 1]);
            }

            return result.ToArray();
        }

        /// <summary>The length of a route on the ground plane.</summary>
        public static float Length(IReadOnlyList<Vector3> route)
        {
            float length = 0f;
            for (int i = 1; i < route.Count; i++)
            {
                length += Flat(route[i] - route[i - 1]).magnitude;
            }

            return length;
        }

        /// <summary>The sharpest change of heading between two legs of a route, in degrees.</summary>
        public static float SharpestTurn(IReadOnlyList<Vector3> route)
        {
            float sharpest = 0f;
            for (int i = 1; i < route.Count - 1; i++)
            {
                Vector3 before = Flat(route[i] - route[i - 1]);
                Vector3 after = Flat(route[i + 1] - route[i]);
                if (before.sqrMagnitude > 0f && after.sqrMagnitude > 0f)
                {
                    sharpest = Mathf.Max(sharpest, Vector3.Angle(before, after));
                }
            }

            return sharpest;
        }

        private static void AddCorner(Vector3 from, Vector3 corner, Vector3 to, float radius, List<Vector3> result)
        {
            Vector3 incoming = Flat(corner - from);
            Vector3 outgoing = Flat(to - corner);
            float lengthIn = incoming.magnitude;
            float lengthOut = outgoing.magnitude;
            float turn = lengthIn > 0f && lengthOut > 0f ? Vector3.Angle(incoming, outgoing) : 0f;
            if (turn < MinTurnDegrees || radius <= 0f)
            {
                result.Add(corner);
                return;
            }

            incoming /= lengthIn;
            outgoing /= lengthOut;
            // The arc leaves the incoming leg this far before the corner and joins the outgoing leg as far after it.
            float half = turn * 0.5f * Mathf.Deg2Rad;
            float tangent = Mathf.Min(radius * Mathf.Tan(half), Mathf.Min(lengthIn, lengthOut) * LegShare);
            float fitted = tangent / Mathf.Tan(half);
            Vector3 start = corner - incoming * tangent;
            Vector3 end = corner + outgoing * tangent;
            Vector3 inward = (outgoing - incoming).normalized;
            Vector3 centre = corner + inward * (fitted / Mathf.Cos(half));
            centre.y = corner.y;

            int points = Mathf.Max(2, Mathf.CeilToInt(turn / DegreesPerPoint));
            Vector3 arm = Flat(start - centre);
            float side = Mathf.Sign(Vector3.Cross(incoming, outgoing).y);
            for (int p = 0; p <= points; p++)
            {
                float share = p / (float)points;
                Vector3 at = centre + Quaternion.AngleAxis(turn * share * side, Vector3.up) * arm;
                at.y = Mathf.Lerp(start.y, end.y, share);
                result.Add(at);
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
