using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The ground every structure of a level keeps free of decoration: the base (its model's footprint), the road
    /// gates (the road's width), the enemy entrances, the mining nodes and the nests, each with the theme's margin.
    /// Every decor builder asks it before it puts a rock, a plant, a cliff or a silhouette down, so nothing ever
    /// grows through a structure, whatever the level's layout.
    /// </summary>
    public sealed class StructureClearance
    {
        private const float GateOverhang = 0.3f;
        private const float EntranceRadiusCells = 0.9f;
        private const float MineRadiusCells = 0.6f;
        private const float NestRadiusCells = 0.9f;

        private readonly List<Zone> _zones = new List<Zone>();

        /// <summary>One kept-free circle on the ground.</summary>
        public readonly struct Zone
        {
            public Zone(string name, Vector3 centre, float radius, bool nest)
            {
                Name = name;
                Centre = centre;
                Radius = radius;
                Nest = nest;
            }

            public string Name { get; }
            public Vector3 Centre { get; }
            public float Radius { get; }

            /// <summary>A nest's ring, where the nest's own shells may lie.</summary>
            public bool Nest { get; }
        }

        public StructureClearance(LevelDefinition level)
        {
            VisualTheme theme = level.Theme;
            float margin = theme != null ? theme.DecorClearance : 0f;
            float cell = level.CellSize;
            if (level.MainRoad.Length > 0)
            {
                _zones.Add(new Zone("Base", LevelVisualBuilder.BasePosition(level), BaseFootprint(theme) + margin, false));
            }

            float gateRadius = level.Road.Width * cell * 0.5f + GateOverhang + margin;
            foreach (BlockadeSpot gate in level.Blockades)
            {
                _zones.Add(new Zone($"Gate {gate.Cell}", level.Road.CentreOf(gate.Cell), gateRadius, false));
            }

            for (int i = 0; i < level.Paths.Length; i++)
            {
                if (level.Paths[i].Waypoints != null && level.Paths[i].Waypoints.Length >= 2 && !level.Paths[i].Flying)
                {
                    _zones.Add(new Zone($"Entrance {i}", level.Road.Route(i)[0], EntranceRadiusCells * cell + margin, false));
                }
            }

            GridMap grid = level.Grid;
            foreach (Vector2Int mine in level.MineCells)
            {
                _zones.Add(new Zone($"Mine {mine}", grid.CellToWorld(mine), MineRadiusCells * cell + margin, false));
            }

            foreach (ElementNest nest in level.Nests)
            {
                _zones.Add(new Zone($"Nest {nest.Cell}", grid.CellToWorld(nest.Cell), NestRadiusCells * cell + margin, true));
            }

            // Found against the zones above only, so the rig never blocks itself; from here on every decor builder keeps
            // its ground free like a structure's.
            ColonyRigDefinition rig = theme != null && theme.Environment != null ? theme.Environment.ColonyRig : null;
            if (rig != null && rig.Drill != null && ColonyRigPlacement.TryFind(level, rig, this, out Vector3 at, out Quaternion facing))
            {
                HasColonyRig = true;
                ColonyRigPosition = at;
                ColonyRigFacing = facing;
                _zones.Add(new Zone("Colony rig", at, rig.ClearRadius + margin, false));
            }
        }

        public IReadOnlyList<Zone> Zones => _zones;

        /// <summary>Whether the continent's colony rig found a spot beside this field.</summary>
        public bool HasColonyRig { get; }

        public Vector3 ColonyRigPosition { get; }

        public Quaternion ColonyRigFacing { get; }

        /// <summary>
        /// True when a piece standing at <paramref name="position"/> and reaching <paramref name="radius"/> round it
        /// would touch a structure. <paramref name="nestDressing"/> lets a nest's own shells lie in its ring.
        /// </summary>
        public bool Blocks(Vector3 position, float radius, bool nestDressing = false)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                Zone zone = _zones[i];
                if (nestDressing && zone.Nest)
                {
                    continue;
                }

                float dx = position.x - zone.Centre.x;
                float dz = position.z - zone.Centre.z;
                float reach = zone.Radius + radius;
                if (dx * dx + dz * dz < reach * reach)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>How far a placed piece reaches round its pivot on the ground: its mesh bounds carried through its matrix.</summary>
        public static float Reach(Mesh mesh, Matrix4x4 matrix) => ReachFrom(mesh, matrix, matrix.GetColumn(3));

        // The farthest corner of the mesh bounds from a point on the ground, measured flat.
        private static float ReachFrom(Mesh mesh, Matrix4x4 matrix, Vector3 pivot)
        {
            if (mesh == null)
            {
                return 0f;
            }

            Bounds bounds = mesh.bounds;
            float reach = 0f;
            for (int corner = 0; corner < 8; corner++)
            {
                var local = new Vector3(
                    (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                Vector3 world = matrix.MultiplyPoint3x4(local) - pivot;
                reach = Mathf.Max(reach, new Vector2(world.x, world.z).magnitude);
            }

            return reach;
        }

        // An imported base measures its model (the widest corner of its meshes round the pivot); the procedural one uses the theme's figure.
        private static float BaseFootprint(VisualTheme theme)
        {
            if (theme == null)
            {
                return 0f;
            }

            ModelArtDefinition art = theme.BaseArt != null ? theme.BaseArt.Model : null;
            if (art == null || art.Model == null)
            {
                return theme.BaseFootprint;
            }

            float reach = 0f;
            foreach (MeshFilter filter in art.Model.GetComponentsInChildren<MeshFilter>(true))
            {
                reach = Mathf.Max(reach, ReachFrom(filter.sharedMesh, art.Model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix, Vector3.zero) * art.Scale);
            }

            foreach (SkinnedMeshRenderer skinned in art.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                reach = Mathf.Max(reach, ReachFrom(skinned.sharedMesh, art.Model.transform.worldToLocalMatrix * skinned.transform.localToWorldMatrix, Vector3.zero) * art.Scale);
            }

            return reach > 0f ? reach : theme.BaseFootprint;
        }
    }
}
