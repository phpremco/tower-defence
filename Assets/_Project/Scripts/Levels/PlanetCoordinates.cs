using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Pure maths for places on the planet: longitude and latitude to a point on the unit sphere, the shortest
    /// route between two such points and the turn that brings a point to the front of the globe.
    /// </summary>
    public static class PlanetCoordinates
    {
        /// <summary>Two levels closer than this, in degrees of arc, overlap on the map once the view zooms onto their region.</summary>
        public const float MinimumSpacingDegrees = 8f;

        /// <summary>Longitude 0 faces +z, east is +x, north is +y.</summary>
        public static Vector3 ToDirection(float longitudeDegrees, float latitudeDegrees)
        {
            float lon = longitudeDegrees * Mathf.Deg2Rad;
            float lat = latitudeDegrees * Mathf.Deg2Rad;
            float flat = Mathf.Cos(lat);
            return new Vector3(flat * Mathf.Sin(lon), Mathf.Sin(lat), flat * Mathf.Cos(lon));
        }

        /// <summary>Texture coordinates of a place on an equirectangular map, both 0..1.</summary>
        public static Vector2 ToUv(float longitudeDegrees, float latitudeDegrees)
        {
            return new Vector2((longitudeDegrees + 180f) / 360f, (latitudeDegrees + 90f) / 180f);
        }

        /// <summary>Degrees of arc between two points on the sphere.</summary>
        public static float AngleBetween(Vector3 a, Vector3 b) => Vector3.Angle(a, b);

        /// <summary>A point on the shortest route from a to b, t from 0 to 1, staying on the unit sphere.</summary>
        public static Vector3 Along(Vector3 a, Vector3 b, float t) => Vector3.Slerp(a, b, t).normalized;

        /// <summary>
        /// The turn of the globe (tilt about x after a spin about y) that brings the point to face a camera on
        /// the -z side. Returns (yaw, pitch) in degrees; apply as AngleAxis(pitch, right) * AngleAxis(yaw, up).
        /// </summary>
        public static Vector2 TurnToFront(Vector3 direction)
        {
            float yaw = Mathf.Atan2(-direction.x, direction.z) * Mathf.Rad2Deg + 180f;
            Vector3 spun = Quaternion.AngleAxis(yaw, Vector3.up) * direction;
            float pitch = -Mathf.Atan2(spun.y, -spun.z) * Mathf.Rad2Deg;
            return new Vector2(Mathf.Repeat(yaw + 180f, 360f) - 180f, pitch);
        }

        /// <summary>The globe rotation for a given yaw and pitch, the same convention as <see cref="TurnToFront"/>.</summary>
        public static Quaternion Rotation(float yaw, float pitch)
        {
            return Quaternion.AngleAxis(pitch, Vector3.right) * Quaternion.AngleAxis(yaw, Vector3.up);
        }

        /// <summary>The shortest signed difference between two angles in degrees, in -180..180.</summary>
        public static float DeltaAngle(float from, float to) => Mathf.DeltaAngle(from, to);
    }
}
