using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// Lays the levels of a continent on the globe as one winding route, numbered in walking order. The route snakes in
    /// four rows that follow the round shape of the continent (shorter rows near its north and south coasts), rows
    /// gently wave and are joined by rounded turns, and the sites sit at equal steps along that line, so they do not
    /// line up in columns. It starts in the south-west (with bearing 0). Several variants (rows running east or
    /// columns running north, small turns of the whole route, phases of the wave) are tried; an optional ruggedness
    /// sampler scores them and the route over the flattest ground wins, so the sites keep off the mountain chains.
    /// Built in a tangent frame around the continent's middle and wrapped onto the sphere, so the spacing holds near
    /// the poles as well.
    /// Pure and deterministic: the same arguments always give the same route.
    /// </summary>
    public static class NodePlacer
    {
        public const int Rows = 4;
        public const int Columns = 5;

        /// <summary>Distance between the rows of the route, in degrees of arc, before it is fitted into the continent.</summary>
        public const float StepDegrees = 9.5f;

        /// <summary>How far every site keeps from the continent's rim, in degrees of arc.</summary>
        public const float CoastInsetDegrees = 4f;

        /// <summary>No two sites closer than this, in degrees of arc (the planet map's minimum spacing).</summary>
        public const float MinimumSpacingDegrees = 6f;

        // The wave of a row: its height as a share of the row step and its length as a share of the widest row.
        private const double WaveShare = 0.25;
        private const double WaveLengthShare = 1.1;

        // Turns of the whole route tried for the flattest ground, in degrees, and phases of the row wave.
        private static readonly double[] TurnOffsets = { 0.0, -6.0, 6.0, -12.0, 12.0, -18.0, 18.0, -24.0, 24.0 };
        private static readonly double[] WavePhases = { 0.0, 0.5 * Math.PI, Math.PI, 1.5 * Math.PI };

        private static readonly bool[] Orientations = { false, true };

        // Where the ground around a site is sampled, in degrees from it.
        private const double SettlementReach = 3.0;
        private static readonly double[][] Neighbourhood =
        {
            new[] { 0.0, 0.0 }, new[] { SettlementReach, 0.0 }, new[] { -SettlementReach, 0.0 }, new[] { 0.0, SettlementReach }, new[] { 0.0, -SettlementReach },
        };

        private const int SamplesPerDegree = 8;
        private const int TurnSamples = 48;
        private const int StepSearchIterations = 24;

        public static GlobePoint[] Route(float centreLongitude, float centreLatitude, float radiusDegrees, int count = Rows * Columns, float bearingDegrees = 0f, Func<GlobePoint, float> ruggedness = null)
        {
            count = Math.Max(0, Math.Min(count, Rows * Columns));
            Frame frame = new Frame(centreLongitude, centreLatitude);
            double usable = Math.Max(0.0, radiusDegrees - CoastInsetDegrees);

            GlobePoint[] best = null;
            double bestScore = double.MaxValue;
            // Rows running east (the default) first, then columns running north; both start in the south-west.
            foreach (bool columns in Orientations)
            {
                foreach (double phase in WavePhases)
                {
                    double[][] plane = Winding(usable, phase, columns);
                    if (plane == null)
                    {
                        continue;
                    }

                    foreach (double turn in TurnOffsets)
                    {
                        GlobePoint[] sites = Wrap(frame, plane, count, bearingDegrees + turn, 0.0, 0.0);
                        if (!Spaced(sites))
                        {
                            continue;
                        }

                        double score = Score(frame, plane, count, bearingDegrees + turn, ruggedness);
                        // Strictly better only, so ties keep the earliest (rows, least turned) variant.
                        if (score < bestScore - 1e-9)
                        {
                            bestScore = score;
                            best = sites;
                        }
                    }
                }
            }

            return best ?? Wrap(frame, Serpentine(usable), count, bearingDegrees, 0.0, 0.0);
        }

        // The roughest ground at and around each site (a settlement spreads a little), summed over the route.
        private static double Score(Frame frame, double[][] plane, int count, double bearing, Func<GlobePoint, float> ruggedness)
        {
            if (ruggedness == null)
            {
                return 0.0;
            }

            var worst = new double[count];
            for (int n = 0; n < Neighbourhood.Length; n++)
            {
                GlobePoint[] around = Wrap(frame, plane, count, bearing, Neighbourhood[n][0], Neighbourhood[n][1]);
                for (int i = 0; i < count; i++)
                {
                    worst[i] = Math.Max(worst[i], ruggedness(around[i]));
                }
            }

            double sum = 0.0;
            for (int i = 0; i < count; i++)
            {
                sum += worst[i];
            }

            return sum;
        }

        private static bool Spaced(GlobePoint[] sites)
        {
            for (int i = 0; i < sites.Length; i++)
            {
                for (int k = 0; k < i; k++)
                {
                    if (GlobePoint.AngleBetween(sites[i], sites[k]) < MinimumSpacingDegrees)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // The winding line in the tangent plane (x east, y north, degrees), sampled at Rows * Columns equal steps
        // along its length; with columns the same line is mirrored over the south-west diagonal, so it runs north
        // first. Null when the continent is too small for it; the serpentine below takes over then.
        private static double[][] Winding(double usable, double phase, bool columns)
        {
            double step = StepDegrees;
            double turnRadius = step * 0.5;
            double halfRows = (Rows - 1) * 0.5;
            double outer = halfRows * step;
            if (usable <= outer + turnRadius)
            {
                return null;
            }

            // Each row reaches as far as the round coast allows, leaving room for the turn beyond its end.
            var halfWidths = new double[Rows];
            double widest = 0.0;
            for (int row = 0; row < Rows; row++)
            {
                double y = (row - halfRows) * step;
                halfWidths[row] = Math.Sqrt(usable * usable - y * y) - turnRadius;
                widest = Math.Max(widest, halfWidths[row]);
            }

            double amplitude = step * WaveShare;
            double waveLength = 2.0 * widest * WaveLengthShare;
            var line = new List<double[]>();
            for (int row = 0; row < Rows; row++)
            {
                double y = (row - halfRows) * step;
                bool eastward = row % 2 == 0;
                double from = eastward ? -halfWidths[row] : halfWidths[row];
                double to = -from;
                int samples = Math.Max(2, (int)(Math.Abs(to - from) * SamplesPerDegree));
                for (int s = 0; s <= samples; s++)
                {
                    double x = from + (to - from) * s / samples;
                    line.Add(new[] { x, y + amplitude * Math.Sin(2.0 * Math.PI * x / waveLength + phase) });
                }

                if (row + 1 < Rows)
                {
                    AddTurn(line, to, (row + 1 - halfRows) * step, -halfWidths[row + 1] * (eastward ? -1.0 : 1.0), amplitude, waveLength, phase, eastward, turnRadius);
                }
            }

            // The turns bulge past the round coast a little; the whole route shrinks until every site is inside.
            double[][] sites = Resample(line, Rows * Columns);
            double reach = Reach(sites);
            if (reach > usable)
            {
                double scale = usable / reach;
                foreach (double[] site in sites)
                {
                    site[0] *= scale;
                    site[1] *= scale;
                }
            }

            if (columns)
            {
                foreach (double[] site in sites)
                {
                    double x = site[0];
                    site[0] = site[1];
                    site[1] = x;
                }
            }

            return sites;
        }

        // A rounded turn from the end of one row to the start of the next: a cubic curve that leaves and arrives
        // level, bulging one turn radius beyond the further of the two row ends.
        private static void AddTurn(List<double[]> line, double endX, double nextY, double startX, double amplitude, double waveLength, double phase, bool eastward, double turnRadius)
        {
            double[] a = line[line.Count - 1];
            var d = new[] { startX, nextY + amplitude * Math.Sin(2.0 * Math.PI * startX / waveLength + phase) };
            double side = eastward ? 1.0 : -1.0;
            double reach = side * (Math.Max(side * endX, side * startX) + turnRadius * 4.0 / 3.0);
            var b = new[] { reach, a[1] };
            var c = new[] { reach, d[1] };
            for (int s = 1; s < TurnSamples; s++)
            {
                double t = s / (double)TurnSamples;
                double u = 1.0 - t;
                line.Add(new[]
                {
                    u * u * u * a[0] + 3 * u * u * t * b[0] + 3 * u * t * t * c[0] + t * t * t * d[0],
                    u * u * u * a[1] + 3 * u * u * t * b[1] + 3 * u * t * t * c[1] + t * t * t * d[1],
                });
            }
        }

        // Points along the polyline at one straight-line step from each other, starting at its first point. The step
        // is the largest that still fits every point on the line, so the route is used to its end and a tight turn
        // never brings two neighbours closer than their step (equal steps along the curve would).
        private static double[][] Resample(List<double[]> line, int count)
        {
            double low = 0.0;
            double high = Length(line);
            for (int i = 0; i < StepSearchIterations; i++)
            {
                double middle = (low + high) * 0.5;
                if (Walk(line, count, middle) != null)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            return Walk(line, count, low);
        }

        // Null when the line ends before all points are placed.
        private static double[][] Walk(List<double[]> line, int count, double step)
        {
            var points = new double[count][];
            points[0] = line[0];
            int sample = 0;
            for (int k = 1; k < count; k++)
            {
                while (sample < line.Count && Distance(points[k - 1], line[sample]) < step)
                {
                    sample++;
                }

                if (sample >= line.Count)
                {
                    return null;
                }

                points[k] = line[sample];
            }

            return points;
        }

        private static double Length(List<double[]> line)
        {
            double total = 0.0;
            for (int i = 1; i < line.Count; i++)
            {
                total += Distance(line[i - 1], line[i]);
            }

            return total;
        }

        // The plain four-by-five serpentine, shrunk to fit: the fallback for a continent too small to wind in.
        private static double[][] Serpentine(double usable)
        {
            double halfColumns = (Columns - 1) * 0.5;
            double halfRows = (Rows - 1) * 0.5;
            double reach = Math.Sqrt(halfColumns * halfColumns + halfRows * halfRows);
            double step = Math.Min(StepDegrees, usable / reach);
            var points = new double[Rows * Columns][];
            for (int i = 0; i < points.Length; i++)
            {
                int row = i / Columns;
                int along = i % Columns;
                int column = row % 2 == 0 ? along : Columns - 1 - along;
                points[i] = new[] { (column - halfColumns) * step, (row - halfRows) * step };
            }

            return points;
        }

        private static double Reach(double[][] points)
        {
            double reach = 0.0;
            foreach (double[] p in points)
            {
                reach = Math.Max(reach, Math.Sqrt(p[0] * p[0] + p[1] * p[1]));
            }

            return reach;
        }

        private static double Distance(double[] a, double[] b)
        {
            double dx = b[0] - a[0];
            double dy = b[1] - a[1];
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // The first sites of the plane route on the globe, turned by the bearing and moved by (offsetX, offsetY) degrees.
        private static GlobePoint[] Wrap(Frame frame, double[][] plane, int count, double bearingDegrees, double offsetX, double offsetY)
        {
            double bearing = bearingDegrees * Math.PI / 180.0;
            double cos = Math.Cos(bearing);
            double sin = Math.Sin(bearing);
            var sites = new GlobePoint[count];
            for (int i = 0; i < count; i++)
            {
                double x = plane[i][0] + offsetX;
                double y = plane[i][1] + offsetY;
                sites[i] = frame.Wrap(x * cos - y * sin, x * sin + y * cos);
            }

            return sites;
        }

        // The tangent frame at the continent's middle: its point, east and north, in the frame of GlobePoint.Direction.
        private readonly struct Frame
        {
            private readonly double[] _centre;
            private readonly double[] _east;
            private readonly double[] _north;

            public Frame(float longitude, float latitude)
            {
                double[] centre = new GlobePoint(longitude, latitude).Direction();
                double lon = longitude * Math.PI / 180.0;
                double lat = latitude * Math.PI / 180.0;
                // Derivatives of GlobePoint.Direction along longitude and latitude, normalised.
                _centre = centre;
                _east = new[] { -Math.Cos(lon), 0.0, -Math.Sin(lon) };
                _north = new[] { Math.Sin(lat) * Math.Sin(lon), Math.Cos(lat), -Math.Sin(lat) * Math.Cos(lon) };
            }

            // The point reached by walking an arc of |(u, v)| degrees from the middle in the direction (u, v).
            public GlobePoint Wrap(double u, double v)
            {
                double length = Math.Sqrt(u * u + v * v);
                double angle = length * Math.PI / 180.0;
                var direction = new double[3];
                for (int k = 0; k < 3; k++)
                {
                    double tangent = length > 0.0 ? (_east[k] * u + _north[k] * v) / length : 0.0;
                    direction[k] = Math.Cos(angle) * _centre[k] + Math.Sin(angle) * tangent;
                }

                return GlobePoint.FromDirection(direction);
            }
        }
    }
}
