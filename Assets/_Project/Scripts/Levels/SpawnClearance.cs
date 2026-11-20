using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The ground a level's spawn points keep free of towers (round 88a, Remco: "I can build on the opening hole"): every
    /// element nest (main or surprise) under its opening's slabs and every route gate under its mound or pod. A cell is
    /// kept free when a tower on it would stand on or in the prop: its middle lies within the prop's radius plus half a
    /// tower's foot. The build grid blocks these cells, so the placement, the pads, the radar, the balance model's build
    /// spots and the bots all read the same ground; the level validator and a test hold the rule on every level.
    /// </summary>
    public static class SpawnClearance
    {
        /// <summary>Half the living opening's slabs across, in cells (the burrow's rest pose measures 2.8 cells).</summary>
        public const float NestRadiusCells = 1.4f;

        /// <summary>Half a tower's foot on its pad, in cells.</summary>
        public const float TowerHalfCells = 0.4f;

        /// <summary>One spawn point's circle on the ground, in world units.</summary>
        public readonly struct Prop
        {
            public Prop(string name, Vector3 centre, float radius)
            {
                Name = name;
                Centre = centre;
                Radius = radius;
            }

            public string Name { get; }
            public Vector3 Centre { get; }
            public float Radius { get; }
        }

        /// <summary>Every spawn point of the level with the radius its drawn prop covers on the ground.</summary>
        public static List<Prop> Props(LevelDefinition level)
        {
            var props = new List<Prop>();
            float cell = level.CellSize;
            SpawnOriginDefinition art = level.SpawnOrigins;
            float gateScale = art != null ? art.GateScale : 1f;
            float airScale = art != null ? art.AirGateScale : 1f;
            for (int p = 0; p < level.Paths.Length; p++)
            {
                if (level.Paths[p].Waypoints == null || level.Paths[p].Waypoints.Length < 2)
                {
                    continue;
                }

                bool flying = level.Paths[p].Flying;
                float radius = LevelValidator.GateFootprint * gateScale * (flying ? airScale : 1f) * 0.5f;
                props.Add(new Prop($"{(flying ? "air" : "ground")} gate of '{level.Paths[p].Name}'", level.Road.Route(p)[0], radius));
            }

            GridMap grid = level.Grid;
            foreach (ElementNest nest in level.Nests)
            {
                props.Add(new Prop($"{nest.Element} nest at {nest.Cell}", grid.CellToWorld(nest.Cell), NestRadiusCells * cell));
            }

            return props;
        }

        /// <summary>True when a tower on the cell would stand on or in the prop.</summary>
        public static bool Covers(Prop prop, GridMap grid, Vector2Int cell)
        {
            Vector3 offset = grid.CellToWorld(cell) - prop.Centre;
            float reach = prop.Radius + TowerHalfCells * grid.CellSize;
            return offset.x * offset.x + offset.z * offset.z < reach * reach;
        }

        /// <summary>Adds every grid cell the prop keeps free of towers.</summary>
        public static void Collect(Prop prop, GridMap grid, ICollection<Vector2Int> cells)
        {
            Vector2Int middle = grid.WorldToCell(prop.Centre);
            int reach = Mathf.CeilToInt(prop.Radius / grid.CellSize + TowerHalfCells) + 1;
            for (int x = -reach; x <= reach; x++)
            {
                for (int y = -reach; y <= reach; y++)
                {
                    Vector2Int cell = middle + new Vector2Int(x, y);
                    if (grid.Contains(cell) && Covers(prop, grid, cell))
                    {
                        cells.Add(cell);
                    }
                }
            }
        }

        /// <summary>Adds every grid cell a spawn point of the level keeps free of towers.</summary>
        public static void Collect(LevelDefinition level, HashSet<Vector2Int> cells)
        {
            GridMap grid = level.Grid;
            foreach (Prop prop in Props(level))
            {
                Collect(prop, grid, cells);
            }
        }
    }
}
