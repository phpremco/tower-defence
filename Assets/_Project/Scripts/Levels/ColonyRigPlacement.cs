using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Where a level's colony rig stands: on the open ground just outside the field, where the slanted camera still
    /// shows it (its foot and its top, inside the band the HUD leaves free), clear of every structure's kept-free
    /// ground, and as close to the base as that allows, since the rig is the colony's. Every cell of the field is a
    /// road or a build pad, so the rig never stands on one. Pure and deterministic: the same level always puts it in
    /// the same spot, and the decor builders keep that spot free.
    /// </summary>
    public static class ColonyRigPlacement
    {
        /// <summary>The widest screen the spot must show on (16:9); a wider window only shows more round it.</summary>
        public const float ReferenceAspect = 16f / 9f;

        private const float SearchStep = 0.5f;
        private const float SearchReachCells = 14f;

        /// <param name="clearance">The structures' kept-free ground (without the rig itself).</param>
        public static bool TryFind(LevelDefinition level, ColonyRigDefinition rig, StructureClearance clearance, out Vector3 position, out Quaternion facing)
        {
            position = default;
            facing = Quaternion.identity;
            if (level == null || rig == null || level.MainRoad.Length == 0)
            {
                return false;
            }

            Vector2 half = level.WorldSize * 0.5f;
            Vector3 home = LevelVisualBuilder.BasePosition(level);
            float radius = rig.ClearRadius;
            float outside = radius + rig.FieldGap;
            float reach = SearchReachCells * level.CellSize;
            float best = float.PositiveInfinity;
            for (float x = -half.x - reach; x <= half.x + reach; x += SearchStep)
            {
                for (float z = -half.y - reach; z <= half.y + reach; z += SearchStep)
                {
                    var at = new Vector3(x, 0f, z);
                    if (!OutsideField(at, half, outside) || !Visible(level, rig, at, radius) || clearance.Blocks(at, radius))
                    {
                        continue;
                    }

                    float distance = (at - home).sqrMagnitude;
                    if (distance < best)
                    {
                        best = distance;
                        position = at;
                    }
                }
            }

            if (float.IsPositiveInfinity(best))
            {
                return false;
            }

            // The derrick turns its working side to the base, like the cutscene's rig towards the landing.
            Vector3 towards = home - position;
            towards.y = 0f;
            facing = towards.sqrMagnitude > 0.01f ? Quaternion.LookRotation(towards.normalized) : Quaternion.identity;
            return true;
        }

        /// <summary>True when the circle round <paramref name="at"/> lies wholly outside the field's rectangle.</summary>
        public static bool OutsideField(Vector3 at, Vector2 half, float radius)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(at.x) - half.x);
            float dz = Mathf.Max(0f, Mathf.Abs(at.z) - half.y);
            return dx * dx + dz * dz >= radius * radius;
        }

        // The foot ring and the top of the rig both stay inside the free part of the screen.
        private static bool Visible(LevelDefinition level, ColonyRigDefinition rig, Vector3 at, float radius)
        {
            return FixedCameraRig.InView(level, at, ReferenceAspect, radius)
                && FixedCameraRig.InView(level, at + Vector3.up * rig.Height, ReferenceAspect, radius * 0.5f);
        }
    }
}
