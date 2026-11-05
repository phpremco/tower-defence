using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// What a continent boss can see and touch of the level it fights in: the climb layer with the structures marked,
    /// the pools, the drawn decoration (to break the fragile pieces it walks into), the colony rig, and the way to send
    /// creatures out of its crack lines. Built once per level by the <see cref="BossDirector"/> and shared by every boss.
    /// </summary>
    public sealed class BossField
    {
        private const float BaseRadiusCells = 1.2f;
        private const float DrapeStep = 0.5f;

        private readonly List<Tower> _towers = new List<Tower>(32);
        private readonly List<Vector3> _broken = new List<Vector3>(8);

        public BossField(LevelContext context, IReadOnlyList<DecorFormation> formations, EnvironmentInstancer decor, ColonyRigView rig, Action<EnemyDefinition, Vector3> spawnFromCrack, Transform effects)
        {
            Context = context;
            Level = context.Level;
            Climb = ClimbLayer.For(Level, formations);
            // The landscape's own height field (the same the ground mesh is built from), so what lies on the ground sits on it.
            Terrain = new TerrainField(Level, new RoadShape(Level));
            Pools = new LiquidPools(Level);
            Decor = decor;
            Rig = rig;
            SpawnFromCrack = spawnFromCrack;
            Effects = effects;
            var clearance = new StructureClearance(Level);
            HasRig = clearance.HasColonyRig;
            RigPosition = clearance.ColonyRigPosition;
        }

        public LevelContext Context { get; }
        public LevelDefinition Level { get; }
        public GridMap Grid => Level.Grid;
        public ClimbLayer Climb { get; }
        public TerrainField Terrain { get; }
        public LiquidPools Pools { get; }
        public EnvironmentInstancer Decor { get; }
        public ColonyRigView Rig { get; }
        public bool HasRig { get; }
        public Vector3 RigPosition { get; }
        public Action<EnemyDefinition, Vector3> SpawnFromCrack { get; }

        /// <summary>Parent for the views a boss lays on the field (crack lines, rings, wakes).</summary>
        public Transform Effects { get; }

        /// <summary>Every tower standing on the field (not the gates), as of the last <see cref="Refresh"/>.</summary>
        public IReadOnlyList<Tower> Towers => _towers;

        /// <summary>Marks every tower, gate and the base on the climb layer again; call before planning a route.</summary>
        public void Refresh()
        {
            Climb.ClearStructures();
            _towers.Clear();
            StructureRegistry structures = Context.Structures;
            for (int i = 0; i < structures.Count; i++)
            {
                switch (structures[i])
                {
                    case Tower tower when tower.Definition.BlocksGroundEnemies:
                        MarkGate(tower);
                        break;
                    case Tower tower:
                        _towers.Add(tower);
                        Climb.MarkTower(Grid.WorldToCell(tower.Position));
                        break;
                    case IStructure other when other.IsBase:
                        MarkBase(other.Position);
                        break;
                }
            }
        }

        /// <summary>Height of the landscape's surface at a point.</summary>
        public float GroundAt(Vector3 at) => Terrain.HeightAt(at.x, at.z);

        /// <summary>
        /// The points laid on the landscape: every leg cut into short pieces, each point at the ground's height there, so a
        /// crack drawn along them follows the relief instead of cutting under it.
        /// </summary>
        public List<Vector3> Draped(IReadOnlyList<Vector3> points)
        {
            var draped = new List<Vector3>(points.Count * 2);
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    Vector3 from = points[i - 1];
                    Vector3 to = points[i];
                    int pieces = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / DrapeStep));
                    for (int p = 1; p < pieces; p++)
                    {
                        Vector3 at = Vector3.Lerp(from, to, p / (float)pieces);
                        draped.Add(new Vector3(at.x, GroundAt(at), at.z));
                    }
                }

                draped.Add(new Vector3(points[i].x, GroundAt(points[i]), points[i].z));
            }

            return draped;
        }

        /// <summary>Breaks the fragile decoration within a radius; returns where pieces broke (reused list).</summary>
        public IReadOnlyList<Vector3> BreakDecor(Vector3 centre, float radius)
        {
            _broken.Clear();
            Decor?.Break(centre, radius, _broken);
            return _broken;
        }

        // A gate stands across the whole road: every cell of its rung is its footprint, wreck or not.
        private void MarkGate(Tower gate)
        {
            Vector2Int cell = Grid.WorldToCell(gate.Position);
            foreach (Vector2Int rung in Level.Road.Rung(cell))
            {
                Climb.MarkGate(rung);
            }

            Climb.MarkGate(cell);
        }

        private void MarkBase(Vector3 position)
        {
            Vector2Int centre = Grid.WorldToCell(position);
            int reach = Mathf.CeilToInt(BaseRadiusCells);
            for (int x = -reach; x <= reach; x++)
            {
                for (int y = -reach; y <= reach; y++)
                {
                    var cell = centre + new Vector2Int(x, y);
                    if ((Grid.CellToWorld(cell) - position).sqrMagnitude <= BaseRadiusCells * BaseRadiusCells * Grid.CellSize * Grid.CellSize)
                    {
                        Climb.MarkBase(cell);
                    }
                }
            }
        }
    }
}
