using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The geometry of the HUD radar: the level's ground rectangle (centred on the world origin, x across, z up the
    /// field) is fitted inside the round scope by its diagonal, so it fits at any turn of the map, and a point maps to the
    /// scope linearly. Pure maths, so it is tested without a scene.
    /// </summary>
    public static class RadarMath
    {
        /// <summary>
        /// Half extents in scope units of the field drawn inside a scope of <paramref name="radius"/>: the field's half
        /// diagonal meets the radius, so every corner stays inside the circle whatever the map's turn.
        /// </summary>
        public static Vector2 FitHalfExtents(Vector2 fieldSize, float radius)
        {
            float diagonal = fieldSize.magnitude;
            if (diagonal <= 0f)
            {
                return Vector2.zero;
            }

            return fieldSize * (radius / diagonal);
        }

        /// <summary>Scope units per world unit for a field drawn in a scope of <paramref name="radius"/>.</summary>
        public static float Scale(Vector2 fieldSize, float radius)
        {
            float diagonal = fieldSize.magnitude;
            return diagonal > 0f ? radius * 2f / diagonal : 0f;
        }

        /// <summary>A ground position (x, z) as a point in the unturned map, the scope's centre at the field's centre.</summary>
        public static Vector2 WorldToRadar(Vector3 world, Vector2 fieldSize, float radius)
        {
            float scale = Scale(fieldSize, radius);
            return new Vector2(world.x * scale, world.z * scale);
        }

        /// <summary>The ground position (y zero) under a point of the unturned map.</summary>
        public static Vector3 RadarToWorld(Vector2 radar, Vector2 fieldSize, float radius)
        {
            float scale = Scale(fieldSize, radius);
            return scale > 0f ? new Vector3(radar.x / scale, 0f, radar.y / scale) : Vector3.zero;
        }

        /// <summary>
        /// The ground an orthographic view covers, as width (along the camera's right) and depth (along its forward
        /// on the ground): a screen height lands on the ground stretched by one over the sine of the pitch.
        /// </summary>
        public static Vector2 ViewFootprint(float orthographicSize, float aspect, float pitchDegrees)
        {
            float sine = Mathf.Max(0.05f, Mathf.Sin(pitchDegrees * Mathf.Deg2Rad));
            return new Vector2(orthographicSize * 2f * aspect, orthographicSize * 2f / sine);
        }

        /// <summary>How brightly a contact shows: full when the sweep has just passed it, fading until the next pass.</summary>
        public static float Ping(Vector2 radar, float sweepDegrees, float degreesPerSecond, float fade)
        {
            // The sweep turns clockwise from up, so a contact's angle is measured clockwise from up as well.
            float angle = Mathf.Atan2(radar.x, radar.y) * Mathf.Rad2Deg;
            float since = Mathf.Repeat(sweepDegrees - angle, 360f) / Mathf.Max(1f, degreesPerSecond);
            return Mathf.Exp(-since * fade);
        }
    }
}
