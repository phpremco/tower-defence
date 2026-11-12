using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// What a ground boss entrance needs round it: the sealed ruin gate (about six cells wide at its scale) stands behind the
    /// cell, facing the way the giant walks out, and the giant walks out of its threshold, so nothing that draws a mound of
    /// its own (a nest) may stand that close and no tower may stand where the gate will rise. Shared by the level validator,
    /// the build grid, the gate view and the generator that places the arena's ruin gate.
    /// </summary>
    public static class GroundEntranceRules
    {
        /// <summary>Cells (in either direction, diagonals included) kept free round a ground entrance: half the arch and a step.</summary>
        public const int ThresholdCells = 3;

        /// <summary>True when a cell lies inside the threshold kept free round a ground entrance.</summary>
        public static bool CrowdsThreshold(Vector2Int entrance, Vector2Int cell)
        {
            Vector2Int offset = cell - entrance;
            return Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) <= ThresholdCells;
        }

        /// <summary>
        /// The grid step that points most nearly at the fixed play camera (its yaw from <see cref="CameraSettings"/>): a gate
        /// whose giant walks out this way shows the camera its opening (round 55: seen from the side it reads as a slab).
        /// </summary>
        public static Vector2Int TowardCamera(float yawDegrees)
        {
            float yaw = yawDegrees * Mathf.Deg2Rad;
            var toward = new Vector2(-Mathf.Sin(yaw), -Mathf.Cos(yaw));
            return Mathf.Abs(toward.x) > Mathf.Abs(toward.y)
                ? new Vector2Int(toward.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, toward.y > 0f ? 1 : -1);
        }

        /// <summary>
        /// Half the gate's size on the ground in world metres (x across the opening, y along its axis): its meshes' bounds (the
        /// rest pose) in the gate root's own frame, so a turn on the field does not widen it, at <paramref name="scale"/>.
        /// </summary>
        public static Vector2 GateHalfSize(Transform gate, float scale)
        {
            Matrix4x4 toGate = gate.worldToLocalMatrix;
            float x = 0f;
            float z = 0f;
            foreach (SkinnedMeshRenderer skinned in gate.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned.sharedMesh == null)
                {
                    continue;
                }

                Matrix4x4 toLocal = toGate * skinned.transform.localToWorldMatrix;
                Bounds bounds = skinned.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    Vector3 local = toLocal.MultiplyPoint3x4(point);
                    x = Mathf.Max(x, Mathf.Abs(local.x));
                    z = Mathf.Max(z, Mathf.Abs(local.z));
                }
            }

            return new Vector2(x, z) * scale;
        }

        /// <summary>
        /// The grid cells a gate covers when it stands <paramref name="setBack"/> metres behind the entrance, its axis along
        /// <paramref name="exit"/> (the way the giant walks out): every cell on the grid whose square touches the gate's
        /// rectangle of <paramref name="halfSize"/> (x across, y along). Cells off the grid are left out.
        /// </summary>
        public static List<Vector2Int> GateCells(GridMap grid, Vector2Int entrance, Vector2Int exit, float setBack, Vector2 halfSize)
        {
            var along = new Vector3(exit.x, 0f, exit.y);
            var across = new Vector3(along.z, 0f, -along.x);
            Vector3 centre = grid.CellToWorld(entrance) - along * setBack;
            float halfCell = grid.CellSize * 0.5f;
            int reach = Mathf.CeilToInt((Mathf.Max(halfSize.x, halfSize.y) + setBack) / grid.CellSize) + 1;
            var cells = new List<Vector2Int>();
            for (int x = -reach; x <= reach; x++)
            {
                for (int y = -reach; y <= reach; y++)
                {
                    Vector2Int cell = entrance + new Vector2Int(x, y);
                    if (!grid.Contains(cell))
                    {
                        continue;
                    }

                    Vector3 offset = grid.CellToWorld(cell) - centre;
                    if (Mathf.Abs(Vector3.Dot(offset, across)) <= halfSize.x + halfCell && Mathf.Abs(Vector3.Dot(offset, along)) <= halfSize.y + halfCell)
                    {
                        cells.Add(cell);
                    }
                }
            }

            return cells;
        }
    }
}
