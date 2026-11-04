using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The attack: cracks split open beside the road near the entrance, creatures climb out one after another and
    /// pour down the road towards the commander's gate, and a big one rears up at the entrance and screams. The
    /// creatures are the game's own <see cref="Enemy"/> and <see cref="EnemyView"/> (so they walk, breathe and climb
    /// exactly as in a level), built once and driven by the swarm clock: a jump in time re-places them, so any
    /// moment can be sought. They halt in a crowd short of the gate.
    /// </summary>
    public sealed class FieldSwarm
    {
        private const int Cracks = 5;
        private const float FirstEmerge = 0.4f;
        private const float EmergeGap = 0.2f;
        private const float StepSeconds = 0.1f;
        private const float SeekThreshold = 0.6f;
        private const float SpeedShare = 0.85f;
        private const float GateMargin = 3f;
        private const float RankSpacing = 0.9f;
        private const float LaneWidth = 1.1f;
        private const float CrackSide = 2.2f;
        private const float CrackGlow = 2.2f;
        private const float CrackLength = 1.9f;
        private const float CrackWidth = 0.28f;
        private const float ScreamerEmerge = 2.6f;
        private const float ScreamAt = 6f;
        private const float RingSeconds = 1.3f;
        private const float RingRadius = 7f;
        private const float RearHeight = 0.35f;
        // The boss at full size towers over the gate; a little smaller she still dominates and the gate stays readable.
        private const float ScreamerScale = 0.7f;
        private static readonly Color CrackColour = new Color(1f, 0.32f, 0.12f);
        private static readonly Color RingColour = new Color(1f, 0.35f, 0.2f);

        private readonly List<Creature> _creatures = new List<Creature>();
        private readonly Transform[] _cracks = new Transform[Cracks];
        private readonly Vector3[] _crackPoints = new Vector3[Cracks];
        private readonly Material _crackMaterial;
        private readonly TargetRegistry _targets = new TargetRegistry();
        private readonly StructureRegistry _structures = new StructureRegistry();
        private readonly Transform _ring;
        private readonly Material _ringMaterial;
        private Creature _screamer;
        private Vector3 _screamerSpot;

        private sealed class Creature
        {
            public Enemy Enemy;
            public EnemyDefinition Definition;
            public Vector3[] Route;
            public float Emerge;
            public float Limit;
            public float Applied = -1f;
            public float Speed;
        }

        public FieldSwarm(Transform parent, FieldLayout layout, CutsceneContext context)
        {
            Root = new GameObject("Swarm").transform;
            Root.SetParent(parent, false);
            _crackMaterial = new Material(GlowDecal.RadialMaterial(CrackColour * CrackGlow));
            _ringMaterial = new Material(GlowDecal.RadialMaterial(RingColour));
            BuildCracks(layout);
            BuildCreatures(layout, context);
            _ring = Decal("ScreamRing", _ringMaterial, _screamerSpot + Vector3.up * 0.06f);
        }

        public Transform Root { get; }

        /// <summary>Where the big one stands and screams, for the camera and the dust.</summary>
        public Vector3 ScreamerSpot => _screamerSpot;

        /// <summary>Poses everything at this moment of the swarm clock (seconds since the cracks began to open).</summary>
        public void Pose(float clock, float time)
        {
            Root.gameObject.SetActive(clock > 0f);
            if (clock <= 0f)
            {
                return;
            }

            for (int i = 0; i < Cracks; i++)
            {
                float open = CutsceneMath.Smooth(CutsceneMath.Span(clock, i * 0.25f, i * 0.25f + 1.2f));
                _cracks[i].gameObject.SetActive(open > 0f);
                _cracks[i].localScale = new Vector3(Mathf.Max(0.001f, CrackWidth * (0.5f + open)), Mathf.Max(0.001f, CrackLength * open), 1f);
            }

            float pulse = 0.75f + 0.25f * Mathf.Sin(time * 5f);
            GlowDecal.SetColour(_crackMaterial, CrackColour * (CrackGlow * pulse));

            foreach (Creature creature in _creatures)
            {
                Drive(creature, clock);
            }

            PoseScreamer(clock);
        }

        private void PoseScreamer(float clock)
        {
            float since = clock - ScreamAt;
            float ring = CutsceneMath.Span(since, 0f, RingSeconds);
            _ring.gameObject.SetActive(since > 0f && ring < 1f);
            float radius = RingRadius * CutsceneMath.Smooth(ring);
            _ring.localScale = new Vector3(Mathf.Max(0.001f, radius * 2f), Mathf.Max(0.001f, radius * 2f), 1f);
            GlowDecal.SetColour(_ringMaterial, RingColour * (1.5f * (1f - ring)));
            if (_screamer == null || !_screamer.Enemy.gameObject.activeSelf)
            {
                return;
            }

            // It rears up for the scream and settles again.
            float rear = Mathf.Sin(CutsceneMath.Span(since, -0.4f, 1.6f) * Mathf.PI);
            Transform body = _screamer.Enemy.transform;
            body.position = _screamerSpot + Vector3.up * (RearHeight * rear);
            body.localScale = Vector3.one * (ScreamerScale * (1f + 0.08f * rear));
        }

        // Each creature has its own clock from the moment it climbs out. Small steps keep the walk and the turns
        // exact; a jump (a seek, or the next shot) starts it over and walks it up to the moment.
        private void Drive(Creature creature, float clock)
        {
            float age = clock - creature.Emerge;
            GameObject go = creature.Enemy.gameObject;
            if (age <= 0f)
            {
                if (go.activeSelf)
                {
                    creature.Enemy.Deactivate();
                    go.SetActive(false);
                }

                creature.Applied = -1f;
                return;
            }

            if (creature.Applied < 0f || age < creature.Applied || age - creature.Applied > SeekThreshold)
            {
                go.SetActive(true);
                creature.Enemy.Deactivate();
                creature.Enemy.Initialize(creature.Definition, creature.Route, 1f, 1f, 0f, _targets, _structures, creature.Speed, EnemyOrigin.Ground);
                creature.Applied = 0f;
            }

            while (creature.Applied < age && creature.Enemy.PathProgress < creature.Limit)
            {
                float step = Mathf.Min(StepSeconds, age - creature.Applied);
                creature.Enemy.Step(step);
                creature.Applied += step;
            }

            creature.Applied = age;
        }

        private void BuildCracks(FieldLayout layout)
        {
            float span = Mathf.Max(2f, layout.GateDistance - GateMargin - 1.5f);
            for (int i = 0; i < Cracks; i++)
            {
                float along = Mathf.Lerp(0.6f, span, i / (float)(Cracks - 1));
                Vector3 onRoad = FieldLayout.PointAlong(layout.Road, along);
                Vector3 direction = FieldLayout.PointAlong(layout.Road, along + 0.5f) - onRoad;
                Vector3 side = Vector3.Cross(Vector3.up, direction.normalized);
                float sign = i % 2 == 0 ? 1f : -1f;
                Vector3 point = layout.Ground(onRoad + side * (CrackSide * sign * (0.8f + 0.4f * CutsceneMath.Hash(i, 2))));
                _crackPoints[i] = point;
                Transform crack = Decal($"Crack{i}", _crackMaterial, point + Vector3.up * 0.05f);
                crack.localRotation = Quaternion.Euler(90f, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 60f * sign, 0f);
                _cracks[i] = crack;
            }

            Vector3 entranceSide = layout.EntranceFacing * Vector3.right;
            _screamerSpot = layout.Ground(layout.Entrance + layout.EntranceFacing * Vector3.forward * 1.2f + entranceSide * 1.6f);
        }

        private void BuildCreatures(FieldLayout layout, CutsceneContext context)
        {
            CutsceneCast cast = context.Cast;
            if (cast == null || cast.Swarm == null || cast.Swarm.Length == 0)
            {
                return;
            }

            int count = context.Scaled(cast.SwarmCount);
            for (int i = 0; i < count; i++)
            {
                EnemyDefinition definition = cast.Swarm[i % cast.Swarm.Length];
                if (definition == null)
                {
                    continue;
                }

                int crack = i % Cracks;
                float lane = (CutsceneMath.Hash(i, 11) - 0.5f) * 2f * LaneWidth;
                Vector3[] route = RouteFrom(layout, _crackPoints[crack], lane, out float joinDistance, out float joinAlong);
                int rank = i / Cracks;
                float stop = layout.GateDistance - GateMargin - rank * RankSpacing - CutsceneMath.Hash(i, 13) * 0.8f;
                var creature = new Creature
                {
                    Definition = definition,
                    Route = route,
                    Emerge = FirstEmerge + i * EmergeGap + CutsceneMath.Hash(i, 17) * 0.3f,
                    Limit = joinDistance + Mathf.Max(0.5f, stop - joinAlong),
                    Speed = SpeedShare,
                };
                creature.Enemy = Spawn(definition, i);
                _creatures.Add(creature);
            }

            if (cast.Screamer != null)
            {
                Vector3 facing = layout.EntranceFacing * Vector3.forward;
                _screamer = new Creature
                {
                    Definition = cast.Screamer,
                    Route = new[] { _screamerSpot, _screamerSpot + facing * 0.01f },
                    Emerge = ScreamerEmerge,
                    Limit = 0f,
                    Speed = 0f,
                };
                _screamer.Enemy = Spawn(cast.Screamer, count);
                _creatures.Add(_screamer);
            }
        }

        private Enemy Spawn(EnemyDefinition definition, int index)
        {
            var go = new GameObject($"{definition.name}_{index}");
            go.transform.SetParent(Root, false);
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyView>().HideHealthBar();
            // The swarm clock steps the creature, never the frame loop.
            enemy.enabled = false;
            go.SetActive(false);
            return enemy;
        }

        // From the crack to the road, then down the road on a lane of its own.
        private static Vector3[] RouteFrom(FieldLayout layout, Vector3 crack, float lane, out float joinDistance, out float joinAlong)
        {
            Vector3[] road = layout.Road;
            joinAlong = FieldLayout.DistanceAlong(road, crack) + 0.6f;
            Vector3 join = FieldLayout.PointAlong(road, joinAlong);
            var points = new List<Vector3> { crack, Offset(road, joinAlong, lane, join) };
            joinDistance = Vector3.Distance(crack, points[1]);
            float walked = 0f;
            for (int i = 1; i < road.Length; i++)
            {
                walked += Vector3.Distance(road[i - 1], road[i]);
                if (walked > joinAlong)
                {
                    points.Add(Offset(road, walked, lane, road[i]));
                }
            }

            return points.ToArray();
        }

        // Shifts a road point sideways onto a lane, using the direction of the road just before it.
        private static Vector3 Offset(Vector3[] road, float along, float lane, Vector3 point)
        {
            Vector3 ahead = FieldLayout.PointAlong(road, along + 0.3f);
            Vector3 behind = FieldLayout.PointAlong(road, Mathf.Max(0f, along - 0.3f));
            Vector3 direction = ahead - behind;
            direction.y = 0f;
            Vector3 side = direction.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, direction.normalized) : Vector3.right;
            return point + side * lane;
        }

        private Transform Decal(string name, Material material, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = GlowDecal.Quad();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            go.SetActive(false);
            return go.transform;
        }
    }
}
