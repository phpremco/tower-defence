using System;

namespace Game.Balance
{
    /// <summary>A place on the planet in degrees: longitude -180..180 (east positive), latitude -90..90 (north positive).</summary>
    public readonly struct GlobePoint
    {
        public GlobePoint(float longitude, float latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        public float Longitude { get; }

        public float Latitude { get; }

        /// <summary>Degrees of arc between two places, the same convention as the game's <c>PlanetCoordinates</c>.</summary>
        public static double AngleBetween(GlobePoint a, GlobePoint b)
        {
            double[] p = a.Direction();
            double[] q = b.Direction();
            double dot = Math.Max(-1.0, Math.Min(1.0, p[0] * q[0] + p[1] * q[1] + p[2] * q[2]));
            return Math.Acos(dot) * 180.0 / Math.PI;
        }

        /// <summary>
        /// The point on the unit sphere, the same frame as the game's <c>PlanetCoordinates.ToDirection</c>: longitude 0
        /// faces +z, north is +y and east is -x, which is what a left-handed (Unity) camera needs to see east on the right.
        /// </summary>
        public double[] Direction()
        {
            double lon = Longitude * Math.PI / 180.0;
            double lat = Latitude * Math.PI / 180.0;
            return new[] { -Math.Cos(lat) * Math.Sin(lon), Math.Sin(lat), Math.Cos(lat) * Math.Cos(lon) };
        }

        /// <summary>The place a unit direction points at (the inverse of <see cref="Direction"/>), rounded to 0.01 degrees.</summary>
        public static GlobePoint FromDirection(double[] direction)
        {
            double latitude = Math.Asin(Math.Max(-1.0, Math.Min(1.0, direction[1]))) * 180.0 / Math.PI;
            double longitude = Math.Atan2(-direction[0], direction[2]) * 180.0 / Math.PI;
            return new GlobePoint((float)Math.Round(longitude, 2), (float)Math.Round(latitude, 2));
        }

        public override string ToString() => $"({Longitude:0.0}, {Latitude:0.0})";
    }
}
