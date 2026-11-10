using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Where the story happens on a field level, worked out from its data so any level can host the field shots:
    /// the landing spot on the main road, the base at the road's end, the drill on the deposit nearest the base,
    /// the new dig site a few metres past the first gate (on a deposit when one lies there), the commander's post at
    /// the first gate (and his guard post just behind its field) and the road
    /// entrance the creatures break out at. Each place has a facing for camera anchors.
    /// </summary>
    public sealed class FieldLayout
    {
        private const float LandingShare = 0.8f;
        private const float VeinOffset = 3.6f;
        private const float BeaconOffset = 2.7f;
        private const float DepositFallback = 4f;
        private const float DigBeyondGate = 6f;
        private const float DigMinBeyondGate = 3.5f;
        private const float DigSnap = 4f;
        private const float DigSideOffset = 3f;
        private const float GuardBehindGate = 1.3f;

        private TerrainField _border;

        public FieldLayout(LevelDefinition level)
        {
            Level = level;
            Road = level.Road.Route(0);
            RoadLength = Length(Road);

            Base = Ground(LevelVisualBuilder.BasePosition(level));
            Vector2Int[] main = level.MainRoad;
            Vector3 baseAlong = level.Road.DirectionAt(main[main.Length - 1]);
            BaseFacing = Facing(-baseAlong);

            Entrance = Ground(Road[0]);
            EntranceFacing = Facing(Road.Length > 1 ? Road[1] - Road[0] : Vector3.forward);

            LevelPlayPose.HeroStart(level, out Vector3 gate, out Vector3 gateFacing);
            Gate = Ground(gate);
            GateFacing = Facing(gateFacing);
            GateDistance = DistanceAlong(Road, Gate);

            Landing = Ground(PointAlong(Road, RoadLength * LandingShare));
            LandingFacing = Facing(Base - Landing);
            Vector3 side = LandingFacing * Vector3.right;
            Vein = Ground(Landing + side * VeinOffset);
            BeaconSpot = Ground(Landing + side * BeaconOffset + (LandingFacing * Vector3.forward) * 0.4f);

            // Playtest note 3 (Remco 2026-11-08): the drill goes up where the level keeps it afterwards, the colony rig beside the
            // field; only a continent without a rig drills the deposit nearest the base.
            var clearance = new StructureClearance(level);
            HasColonyRig = clearance.HasColonyRig;
            if (HasColonyRig)
            {
                Drill = Ground(clearance.ColonyRigPosition);
                DrillFacing = clearance.ColonyRigFacing;
            }
            else
            {
                Drill = Ground(NearestDeposit(level, Base, Landing + side * -DepositFallback));
                DrillFacing = Facing(Base - Drill);
            }
            // The new dig site lies past the first gate on the base side, so the swarm pours in between it and the entrance.
            GateCentre = level.Blockades != null && level.Blockades.Length > 0 ? Ground(LevelPlayPose.GatePosition(level, level.Blockades[0].Cell)) : Gate;
            GateCentreDistance = DistanceAlong(Road, GateCentre);
            Vector3 gateCentre = GateCentre;
            Vector3 roadAhead = (PointAlong(Road, GateCentreDistance + 1f) - PointAlong(Road, GateCentreDistance)).normalized;
            Vector3 roadSide = Vector3.Cross(Vector3.up, roadAhead);
            Vector3 digFallback = PointAlong(Road, GateCentreDistance + DigBeyondGate) + roadSide * DigSideOffset;
            DigSite = Ground(DepositPastGate(level, Road, GateCentreDistance + DigMinBeyondGate, digFallback, Drill));
            DigFacing = Facing(gateCentre - DigSite);
            // Mercer holds the gate from behind its field: a step past it on the base side, facing the entrance.
            GuardPost = Ground(gateCentre + roadAhead * GuardBehindGate);

            Pad = Ground(Base + BaseFacing * new Vector3(-2.7f, 0f, -0.6f));
            Core = Ground(Base + BaseFacing * new Vector3(2.5f, 0f, 0.4f));
        }

        public LevelDefinition Level { get; }

        /// <summary>The main road from the entrance to the base.</summary>
        public Vector3[] Road { get; }

        public float RoadLength { get; }

        public Vector3 Landing { get; }
        public Quaternion LandingFacing { get; }
        public Vector3 Vein { get; }
        public Vector3 BeaconSpot { get; }
        public Vector3 Base { get; }
        public Quaternion BaseFacing { get; }
        public Vector3 Pad { get; }
        public Vector3 Core { get; private set; }
        public Vector3 Drill { get; }
        public Quaternion DrillFacing { get; }

        /// <summary>True when the drill stands at the level's colony rig (and takes the rig's size), false on a deposit.</summary>
        public bool HasColonyRig { get; }
        public Vector3 DigSite { get; }
        public Quaternion DigFacing { get; }
        public Vector3 Gate { get; }
        public Quaternion GateFacing { get; }

        /// <summary>The first gate itself (its field across the road), and how far along the road it stands.</summary>
        public Vector3 GateCentre { get; }

        public float GateCentreDistance { get; }

        /// <summary>Where the commander holds the first gate in the cutscene: just behind its field, facing the entrance.</summary>
        public Vector3 GuardPost { get; }

        /// <summary>How far along the road the commander's post lies.</summary>
        public float GateDistance { get; }

        public Vector3 Entrance { get; }
        public Quaternion EntranceFacing { get; }

        public bool TryGetAnchor(CameraAnchor anchor, out Vector3 origin, out Quaternion facing)
        {
            switch (anchor)
            {
                case CameraAnchor.Landing:
                    (origin, facing) = (Landing, LandingFacing);
                    return true;
                case CameraAnchor.Base:
                    (origin, facing) = (Base, BaseFacing);
                    return true;
                case CameraAnchor.Drill:
                    (origin, facing) = (Drill, DrillFacing);
                    return true;
                case CameraAnchor.DigSite:
                    (origin, facing) = (DigSite, DigFacing);
                    return true;
                case CameraAnchor.Gate:
                    (origin, facing) = (Gate, GateFacing);
                    return true;
                case CameraAnchor.Entrance:
                    (origin, facing) = (Entrance, EntranceFacing);
                    return true;
                case CameraAnchor.Commander:
                    (origin, facing) = (DigSite, DigFacing);
                    return true;
                case CameraAnchor.Core:
                    (origin, facing) = (Core, BaseFacing);
                    return true;
                default:
                    (origin, facing) = (Vector3.zero, Quaternion.identity);
                    return false;
            }
        }

        /// <summary>Moves the core to where the base model really has it (the imported base carries its own core).</summary>
        public void PlaceCore(Vector3 core) => Core = Ground(core);

        // On the grid the cell's own height; off it (the colony rig stands beside the field) the landscape's rolling border.
        public Vector3 Ground(Vector3 position)
        {
            Vector2Int cell = Level.Grid.WorldToCell(position);
            float height = Level.Grid.Contains(cell) ? Level.Terrain.HeightAt(cell) : Border.HeightAt(position.x, position.z);
            return new Vector3(position.x, height, position.z);
        }

        private TerrainField Border => _border ??= new TerrainField(Level, new RoadShape(Level));

        /// <summary>The point a distance along a polyline, clamped to its ends.</summary>
        public static Vector3 PointAlong(IReadOnlyList<Vector3> line, float distance)
        {
            for (int i = 1; i < line.Count; i++)
            {
                float segment = Vector3.Distance(line[i - 1], line[i]);
                if (distance <= segment)
                {
                    return Vector3.Lerp(line[i - 1], line[i], segment > 0f ? distance / segment : 0f);
                }

                distance -= segment;
            }

            return line[line.Count - 1];
        }

        /// <summary>How far along a polyline the point nearest to a position lies.</summary>
        public static float DistanceAlong(IReadOnlyList<Vector3> line, Vector3 position)
        {
            float best = float.MaxValue;
            float bestAlong = 0f;
            float walked = 0f;
            for (int i = 1; i < line.Count; i++)
            {
                Vector3 a = line[i - 1];
                Vector3 b = line[i];
                float segment = Vector3.Distance(a, b);
                float t = segment > 0f ? Mathf.Clamp01(Vector3.Dot(position - a, b - a) / (segment * segment)) : 0f;
                float distance = Vector3.Distance(Vector3.Lerp(a, b, t), position);
                if (distance < best)
                {
                    best = distance;
                    bestAlong = walked + t * segment;
                }

                walked += segment;
            }

            return bestAlong;
        }

        public static float Length(IReadOnlyList<Vector3> line)
        {
            float length = 0f;
            for (int i = 1; i < line.Count; i++)
            {
                length += Vector3.Distance(line[i - 1], line[i]);
            }

            return length;
        }

        private static Quaternion Facing(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction.normalized) : Quaternion.identity;
        }

        // The deposit nearest the fallback spot (a few metres past the gate, beside the road) when one lies close to it
        // and past the gate (never the drill's); without one the fallback spot itself.
        private static Vector3 DepositPastGate(LevelDefinition level, Vector3[] road, float minAlong, Vector3 fallback, Vector3 drill)
        {
            Vector3 best = fallback;
            float bestDistance = float.MaxValue;
            foreach (Vector2Int mine in level.MineCells ?? new Vector2Int[0])
            {
                Vector3 candidate = level.Grid.CellToWorld(mine);
                float along = DistanceAlong(road, candidate);
                float distance = Vector3.Distance(candidate, fallback);
                if (along >= minAlong && distance <= DigSnap && Vector3.Distance(candidate, drill) > 1f && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        // The deposits are the level's mining cells; the drill takes the one nearest the base, the dig site the one
        // nearest the entrance. Without deposits a point beside the road stands in.
        private static Vector3 NearestDeposit(LevelDefinition level, Vector3 to, Vector3 fallback)
        {
            Vector2Int[] mines = level.MineCells;
            if (mines == null || mines.Length == 0)
            {
                return fallback;
            }

            GridMap grid = level.Grid;
            Vector3 best = grid.CellToWorld(mines[0]);
            float bestDistance = Vector3.Distance(best, to);
            for (int i = 1; i < mines.Length; i++)
            {
                Vector3 candidate = grid.CellToWorld(mines[i]);
                float distance = Vector3.Distance(candidate, to);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
