using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The "Sway" vertex colour of a plant as the environment shader reads it (README round 8): R bend weight from 0 at
    /// the root to 1 at the tip, G phase 0..1, B flutter. Checks whether a mesh carries it and rebuilds it from the
    /// plant's shape when the export's colour set did not survive the import.
    /// </summary>
    internal static class EnvironmentSway
    {
        // Red that follows the height this well is a real bend weight, not a bake helper.
        private const float MinCorrelation = 0.6f;

        // Bending grows faster towards the tip, as on a stalk.
        private const float BendExponent = 1.4f;

        /// <summary>Whether the mesh's red channel rises with its height (in the frame the matrix puts it in, y up).</summary>
        public static bool Carries(Mesh mesh, Matrix4x4 frame)
        {
            Color[] colours = mesh.colors;
            Vector3[] vertices = mesh.vertices;
            if (colours.Length != vertices.Length || vertices.Length == 0)
            {
                return false;
            }

            double sumY = 0, sumR = 0, sumYY = 0, sumRR = 0, sumYR = 0;
            int count = vertices.Length;
            for (int i = 0; i < count; i++)
            {
                double y = frame.MultiplyPoint3x4(vertices[i]).y;
                double r = colours[i].r;
                sumY += y;
                sumR += r;
                sumYY += y * y;
                sumRR += r * r;
                sumYR += y * r;
            }

            double covariance = sumYR / count - sumY / count * (sumR / count);
            double varianceY = sumYY / count - sumY / count * (sumY / count);
            double varianceR = sumRR / count - sumR / count * (sumR / count);
            return varianceY > 0 && varianceR > 0 && covariance / System.Math.Sqrt(varianceY * varianceR) >= MinCorrelation;
        }

        /// <summary>Sway colours from the shape: bend by height, phase by the direction a leaf points, flutter at the outer tips.</summary>
        public static Color[] Build(Mesh mesh, Matrix4x4 frame)
        {
            Vector3[] vertices = mesh.vertices;
            var points = new Vector3[vertices.Length];
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float maxRadius = 0.0001f;
            for (int i = 0; i < vertices.Length; i++)
            {
                points[i] = frame.MultiplyPoint3x4(vertices[i]);
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
                maxRadius = Mathf.Max(maxRadius, new Vector2(points[i].x, points[i].z).magnitude);
            }

            float height = Mathf.Max(0.0001f, maxY - minY);
            var colours = new Color[vertices.Length];
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i];
                float bend = Mathf.Pow(Mathf.Clamp01((p.y - Mathf.Max(0f, minY)) / height), BendExponent);
                float phase = Mathf.Repeat(Mathf.Atan2(p.z, p.x) / (2f * Mathf.PI), 1f);
                float flutter = bend * Mathf.Clamp01(new Vector2(p.x, p.z).magnitude / maxRadius);
                colours[i] = new Color(bend, phase, flutter, 1f);
            }

            return colours;
        }
    }
}
