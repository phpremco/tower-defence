using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Moves a world label out of the HUD: the label slides the shortest way off each panel it covers, as long as it
    /// stays on screen, until it touches none. A label that cannot be freed (squeezed between panels, or a panel
    /// that fills the screen) is reported as not placeable, and the caller hides it. All rects share one space.
    /// </summary>
    public static class KeepOutMath
    {
        // A label that has just been pushed to a panel's edge must not count as touching it through float error.
        private const float Clearance = 0.5f;
        private const int MaxStepsPerZone = 2;

        /// <summary>True when <paramref name="label"/> can be shown; <paramref name="offset"/> is the move that frees it.</summary>
        public static bool Resolve(Rect label, IReadOnlyList<Rect> zones, Rect area, out Vector2 offset)
        {
            Rect placed = ClampInto(label, area);
            int steps = zones.Count * MaxStepsPerZone + 1;
            for (int step = 0; step < steps; step++)
            {
                int hit = FirstOverlap(placed, zones);
                if (hit < 0)
                {
                    offset = placed.position - label.position;
                    return true;
                }

                if (!TryShiftOff(placed, zones[hit], area, out placed))
                {
                    break;
                }
            }

            offset = Vector2.zero;
            return false;
        }

        /// <summary>Slides a rect fully inside the area, keeping it as close as it was; a rect larger than the area is centred.</summary>
        public static Rect ClampInto(Rect rect, Rect area)
        {
            float x = rect.width >= area.width ? area.center.x - rect.width * 0.5f : Mathf.Clamp(rect.x, area.xMin, area.xMax - rect.width);
            float y = rect.height >= area.height ? area.center.y - rect.height * 0.5f : Mathf.Clamp(rect.y, area.yMin, area.yMax - rect.height);
            return new Rect(x, y, rect.width, rect.height);
        }

        private static int FirstOverlap(Rect rect, IReadOnlyList<Rect> zones)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                if (rect.Overlaps(zones[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        // Of the four ways off the panel the shortest that keeps the label on screen wins; a full-width bar therefore
        // always pushes up or down, a side panel sideways.
        private static bool TryShiftOff(Rect rect, Rect zone, Rect area, out Rect shifted)
        {
            shifted = rect;
            float best = float.PositiveInfinity;
            Consider(rect, new Vector2(0f, zone.yMax - rect.yMin + Clearance), area, ref best, ref shifted);
            Consider(rect, new Vector2(0f, zone.yMin - rect.yMax - Clearance), area, ref best, ref shifted);
            Consider(rect, new Vector2(zone.xMax - rect.xMin + Clearance, 0f), area, ref best, ref shifted);
            Consider(rect, new Vector2(zone.xMin - rect.xMax - Clearance, 0f), area, ref best, ref shifted);
            return !float.IsPositiveInfinity(best);
        }

        private static void Consider(Rect rect, Vector2 move, Rect area, ref float best, ref Rect shifted)
        {
            var candidate = new Rect(rect.position + move, rect.size);
            float length = move.sqrMagnitude;
            if (length < best && Inside(candidate, area))
            {
                best = length;
                shifted = candidate;
            }
        }

        private static bool Inside(Rect rect, Rect area)
        {
            return rect.xMin >= area.xMin - Clearance && rect.xMax <= area.xMax + Clearance && rect.yMin >= area.yMin - Clearance && rect.yMax <= area.yMax + Clearance;
        }
    }
}
