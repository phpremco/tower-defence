using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Which cell the pointer means. The camera looks down at a slant, so a tall tower's body is drawn over the cells
    /// behind it: a ray that only hits the ground would land half a tile or more past the tower the player points
    /// at. Every standing structure is therefore a vertical column the ray can hit first; only when it misses them
    /// all does the ground decide, at the height of the terrain where the ray comes down. Pure math, no scene access.
    /// </summary>
    public static class TowerPicking
    {
        private const int GroundIterations = 3;
        private const float Parallel = 1e-6f;

        /// <summary>The space a structure fills on screen: a vertical column standing on its foot.</summary>
        public readonly struct Column
        {
            public Column(Vector2Int cell, Vector3 foot, float radius, float height)
            {
                Cell = cell;
                Foot = foot;
                Radius = radius;
                Height = height;
            }

            /// <summary>The cell the structure belongs to, which the cursor moves to when the column is hit.</summary>
            public Vector2Int Cell { get; }

            /// <summary>Where the column stands: the structure's own position, which may be shifted inside its cell.</summary>
            public Vector3 Foot { get; }

            public float Radius { get; }
            public float Height { get; }
        }

        /// <summary>The nearest column along the ray, if the ray passes through any.</summary>
        public static bool TryPick(Ray ray, IReadOnlyList<Column> columns, out Vector2Int cell)
        {
            cell = default;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < columns.Count; i++)
            {
                Column column = columns[i];
                if (RayHitsColumn(ray, column.Foot, column.Radius, column.Height, out float distance) && distance < nearest)
                {
                    nearest = distance;
                    cell = column.Cell;
                }
            }

            return !float.IsPositiveInfinity(nearest);
        }

        /// <summary>
        /// True when the ray enters the upright cylinder standing on <paramref name="foot"/> (its side or its top);
        /// <paramref name="distance"/> is how far along the ray it first touches it.
        /// </summary>
        public static bool RayHitsColumn(Ray ray, Vector3 foot, float radius, float height, out float distance)
        {
            distance = float.PositiveInfinity;
            Vector3 origin = ray.origin;
            Vector3 direction = ray.direction;
            float top = foot.y + height;

            // The side: where the ray, seen from above, is one radius from the column's axis.
            float ox = origin.x - foot.x;
            float oz = origin.z - foot.z;
            float a = direction.x * direction.x + direction.z * direction.z;
            if (a > Parallel)
            {
                float b = 2f * (ox * direction.x + oz * direction.z);
                float c = ox * ox + oz * oz - radius * radius;
                float discriminant = b * b - 4f * a * c;
                if (discriminant >= 0f)
                {
                    float t = (-b - Mathf.Sqrt(discriminant)) / (2f * a);
                    float y = origin.y + direction.y * t;
                    if (t >= 0f && y >= foot.y && y <= top)
                    {
                        distance = t;
                    }
                }
            }

            // The top: a ray from above can come down through the cap before it reaches the side.
            if (Mathf.Abs(direction.y) > Parallel)
            {
                float t = (top - origin.y) / direction.y;
                if (t >= 0f && t < distance)
                {
                    float x = origin.x + direction.x * t - foot.x;
                    float z = origin.z + direction.z * t - foot.z;
                    if (x * x + z * z <= radius * radius)
                    {
                        distance = t;
                    }
                }
            }

            return !float.IsPositiveInfinity(distance);
        }

        /// <summary>
        /// Where the ray comes down on ground whose height <paramref name="heightAt"/> gives per point (raised
        /// plateaus stand above the flat grid). False when the ray never points down.
        /// </summary>
        public static bool TryGround(Ray ray, Func<Vector3, float> heightAt, out Vector3 point)
        {
            point = default;
            if (ray.direction.y >= -Parallel)
            {
                return false;
            }

            float height = 0f;
            for (int i = 0; i < GroundIterations; i++)
            {
                float t = (height - ray.origin.y) / ray.direction.y;
                point = ray.origin + ray.direction * t;
                float next = heightAt(point);
                if (Mathf.Approximately(next, height))
                {
                    return true;
                }

                height = next;
            }

            // Still stepping between two heights at an edge: keep the last point, it lies on one of them.
            return true;
        }
    }
}
