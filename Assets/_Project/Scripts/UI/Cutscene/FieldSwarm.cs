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
    /// The attack: the ground bursts open beside the road at the entrance, one living burrow after another (the
    /// level's Blender spawn origin with its own opening: tremor, cracks spreading, the burst with dust, thrown slabs
    /// and a flash, then the open maw), and the creatures run out at speed in their own run cycles and stream down the
    /// road; rollers roll, flyers swoop over them, and a big one climbs out at the entrance and screams. At the first
    /// gate they are held by its field and claw at it, the crowd growing back towards the entrance, so nothing ever
    /// stands idle. The creatures are the game's own <see cref="Enemy"/> and <see cref="EnemyView"/>, driven by the
    /// swarm clock (seconds since the first tremor): a jump in time re-places them, so any moment can be sought.
    /// </summary>
    public sealed class FieldSwarm
    {
        private const float StepSeconds = 0.1f;
        private const float SeekThreshold = 0.6f;
        // Faster than in a level: the swarm rushes out of the ground.
        private const float RushShare = 1.3f;
        private const float LaneWidth = 1.2f;
        private const float RingSpacing = 1.15f;
        private const int Rings = 9;
        private const float RingSeconds = 1.4f;

        // The burrows (README round 23): the opening clip's tremor, burst (1.8 s), maw up, the first creature at 2.53 s.
        private const float BurstAt = 1.8f;
        private const float EmergeAt = 2.53f;
        private const float GlowRampSeconds = 1f;
        private const float FirstOpen = 0.4f;
        private const float OpenGap = 0.8f;
        private const float StreamGap = 0.85f;
        private const float StreamEnd = 15f;
        private const float BurstLight = 7f;
        private const float OpenLight = 1.4f;
        private const float LightRange = 4.5f;
        private const float FlashSeconds = 0.45f;
        private const int CracksPerBurrow = 3;
        private const float CrackLength = 2.6f;
        private const float CrackWidth = 0.55f;
        private const float CrackGlow = 2.4f;
        private const int BurstDust = 14;
        private const int BurstDebris = 22;

        // Flyers lift out of the maw to a cruising height and swoop as they go.
        private const float FlyerHeight = 2.4f;
        private const float FlyerLiftSeconds = 1.2f;
        private const float SwoopHeight = 0.7f;
        private const float SwoopSeconds = 2.6f;

        private const float ScreamerEmerge = 3.2f;
        private const float ScreamAt = 6f;
        private const float RingSecondsScream = 1.3f;
        private const float ScreamRadius = 7f;
        private const float RearHeight = 0.35f;
        private const float ScreamerScale = 0.7f;
        private const float ScreamerSpeed = 0.25f;
        private const float ScreamerWalk = 9f;
        private static readonly Color CrackColour = new Color(1f, 0.32f, 0.12f);
        private static readonly Color RingColour = new Color(1f, 0.35f, 0.2f);
        private static readonly Color FlashColour = new Color(1f, 0.45f, 0.18f);
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // Where the burrows break open: distance along the road from the entrance and to its side (negative: left).
        private static readonly Vector2[] BurrowSpots = { new Vector2(1.2f, 3.1f), new Vector2(3.2f, -3.4f), new Vector2(5.6f, 4.0f), new Vector2(0.4f, -1.6f) };

        private readonly List<Creature> _creatures = new List<Creature>();
        private readonly List<Burrow> _burrows = new List<Burrow>();
        private readonly TargetRegistry _targets = new TargetRegistry();
        private readonly StructureRegistry _structures = new StructureRegistry();
        private readonly CutsceneContext _context;
        private readonly Transform _ring;
        private readonly Material _ringMaterial;
        private readonly Material _crackMaterial;
        private Creature _screamer;
        private Vector3 _screamerSpot;
        private float _simClock;
        private float _lastClock = -1f;

        private sealed class Creature
        {
            public Enemy Enemy;
            public EnemyDefinition Definition;
            public Vector3[] Route;
            public float Emerge;
            public float Applied = -1f;
            public float Speed;
            public bool Flies;
            public float GroundY;
            public int Index;
        }

        private sealed class Burrow
        {
            public Transform Root;
            public ArtModel Model;
            public Color Glow;
            public Light Light;
            public Transform Emerge;
            public float Open;
            public Transform[] Cracks;
            public ParticleSystem Dust;
            public ParticleSystem Debris;
            public ImpactFlash Flash;
        }

        /// <summary>A ring of the crowd at the first gate: it holds walkers once the swarm clock has reached it.</summary>
        private sealed class HoldRing : IStructure
        {
            private readonly FieldSwarm _swarm;
            private readonly float _from;

            public HoldRing(FieldSwarm swarm, Vector3 position, float from)
            {
                _swarm = swarm;
                Position = position;
                _from = from;
            }

            public Vector3 Position { get; }
            public bool IsAlive => true;
            public bool BlocksGround => _swarm._simClock >= _from;
            public bool IsBase => false;
            public float HealthFraction => 1f;

            public void TakeDamage(float amount)
            {
            }

            public void Repair(float amount)
            {
            }
        }

        public FieldSwarm(Transform parent, FieldLayout layout, CutsceneContext context)
        {
            _context = context;
            Root = new GameObject("Swarm").transform;
            Root.SetParent(parent, false);
            _crackMaterial = Additive(CutsceneSprite.CrackSwarmA, CrackColour * CrackGlow);
            _ringMaterial = Additive(CutsceneSprite.ScreamRing, RingColour);
            BuildBurrows(layout, context);
            BuildCreatures(layout, context);
            BuildRings(layout);
            _ring = Decal("ScreamRing", _ringMaterial, _screamerSpot + Vector3.up * 0.06f);
        }

        public Transform Root { get; }

        /// <summary>Where the big one stands and screams, for the camera and the dust.</summary>
        public Vector3 ScreamerSpot => _screamerSpot;

        /// <summary>The shot of the swarm begins: its burst effects start their clock; in any other shot they rest.</summary>
        public void Enter(bool bursting)
        {
            foreach (Burrow burrow in _burrows)
            {
                foreach (ParticleSystem system in new[] { burrow.Dust, burrow.Debris })
                {
                    if (system == null)
                    {
                        continue;
                    }

                    system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    if (bursting)
                    {
                        system.Play(true);
                    }
                }
            }

            _lastClock = -1f;
        }

        /// <summary>Poses everything at this moment of the swarm clock (seconds since the first burrow began to tremble).</summary>
        public void Pose(float clock, float time)
        {
            Root.gameObject.SetActive(clock > 0f);
            if (clock <= 0f)
            {
                return;
            }

            foreach (Burrow burrow in _burrows)
            {
                PoseBurrow(burrow, clock, time);
            }

            foreach (Creature creature in _creatures)
            {
                Drive(creature, clock);
            }

            PoseScreamer(clock);
            _lastClock = clock;
        }

        // Closed and dark, then the opening clip from its own start: the cracks spread and glow through the tremor, the
        // burst flashes and throws dust and slabs, and the open maw idles with its glow on.
        private void PoseBurrow(Burrow burrow, float clock, float time)
        {
            float since = clock - burrow.Open;
            ArtModel model = burrow.Model;
            if (since < 0f)
            {
                model.Sample(ModelClipRole.Closed, 0f, loop: false);
            }
            else if (since < model.LengthOf(ModelClipRole.Open))
            {
                model.Sample(ModelClipRole.Open, since, loop: false);
            }
            else
            {
                model.Sample(ModelClipRole.Idle, since - model.LengthOf(ModelClipRole.Open), loop: true);
            }

            float glow = CutsceneMath.Span(since, 0f, GlowRampSeconds);
            model.Material.SetColor(EmissionColorId, burrow.Glow * glow);
            float flash = Mathf.Exp(-Mathf.Max(0f, since - BurstAt) / FlashSeconds) * (since >= BurstAt ? 1f : 0f);
            burrow.Light.enabled = since > 0f;
            burrow.Light.intensity = OpenLight * glow + BurstLight * flash;

            float spread = CutsceneMath.Smooth(CutsceneMath.Span(since, 0f, BurstAt));
            float pulse = 0.75f + 0.25f * Mathf.Sin(time * 6f + burrow.Open);
            for (int i = 0; i < burrow.Cracks.Length; i++)
            {
                Transform crack = burrow.Cracks[i];
                float open = CutsceneMath.Span(spread, i * 0.15f, 0.7f + i * 0.1f);
                crack.gameObject.SetActive(open > 0f);
                crack.localScale = new Vector3(Mathf.Max(0.001f, 0.4f + 0.6f * open), 1f, Mathf.Max(0.001f, open));
            }

            GlowDecal.SetColour(_crackMaterial, CrackColour * (CrackGlow * pulse));

            // The full effect library burst plays only when the moment is played, never on a seek.
            if (burrow.Flash != null && _lastClock >= 0f && _lastClock < burrow.Open + BurstAt && clock >= burrow.Open + BurstAt && clock - _lastClock < SeekThreshold)
            {
                SpawnOriginDefinition origins = _context.FieldLevel.SpawnOrigins;
                burrow.Flash.gameObject.SetActive(true);
                burrow.Flash.Show(burrow.Root.position, origins.BurstRadius, FlashColour, origins.BurstVfx, ParkFlash);
            }
        }

        private static void ParkFlash(ImpactFlash flash) => flash.gameObject.SetActive(false);

        private void PoseScreamer(float clock)
        {
            float since = clock - ScreamAt;
            float ring = CutsceneMath.Span(since, 0f, RingSecondsScream);
            _ring.gameObject.SetActive(since > 0f && ring < 1f);
            float radius = ScreamRadius * CutsceneMath.Smooth(ring);
            _ring.localScale = new Vector3(Mathf.Max(0.001f, radius * 2f), Mathf.Max(0.001f, radius * 2f), 1f);
            GlowDecal.SetColour(_ringMaterial, RingColour * (1.5f * (1f - ring)));
            if (_screamer == null || !_screamer.Enemy.gameObject.activeSelf)
            {
                return;
            }

            // It rears up for the scream and settles again, then comes on behind its brood.
            float rear = Mathf.Sin(CutsceneMath.Span(since, -0.4f, 1.6f) * Mathf.PI);
            Transform body = _screamer.Enemy.transform;
            Vector3 position = body.position;
            position.y = _screamerSpot.y + RearHeight * rear;
            body.position = position;
            body.localScale = Vector3.one * (ScreamerScale * (1f + 0.08f * rear));
        }

        // Each creature has its own clock from the moment it climbs out. Small steps keep the run and the turns exact; a
        // jump (a seek, or the next shot) starts it over and runs it up to the moment. The gate's rings read the clock of
        // the step being taken, so a creature re-run after a seek meets the crowd it met when the moment was played.
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
                float height = creature.Flies ? FlyerHeight : 0f;
                creature.Enemy.Initialize(creature.Definition, creature.Route, 1f, 1f, height, _targets, _structures, creature.Speed, EnemyOrigin.Nest);
                creature.Applied = 0f;
            }

            while (creature.Applied < age)
            {
                float step = Mathf.Min(StepSeconds, age - creature.Applied);
                _simClock = creature.Emerge + creature.Applied + step;
                creature.Enemy.Step(step);
                creature.Applied += step;
            }

            creature.Applied = age;
            if (creature.Flies)
            {
                // Out of the maw and up, then a slow swoop over the road.
                Transform body = creature.Enemy.transform;
                float lift = CutsceneMath.Smooth(CutsceneMath.Span(age, 0f, FlyerLiftSeconds));
                float swoop = Mathf.Sin((age / SwoopSeconds + CutsceneMath.Hash(creature.Index, 41)) * Mathf.PI * 2f) * SwoopHeight;
                Vector3 position = body.position;
                position.y = creature.GroundY + (FlyerHeight + swoop) * lift;
                body.position = position;
            }
        }

        private void BuildBurrows(FieldLayout layout, CutsceneContext context)
        {
            SpawnOriginDefinition origins = context.FieldLevel.SpawnOrigins;
            SpawnOriginArt art = origins != null ? origins.For(TerrainKind.Ground) : null;
            bool low = context.ParticleScale < 1f;
            ImpactVfxDefinition burst = origins != null ? origins.BurstVfx : null;
            for (int i = 0; i < BurrowSpots.Length; i++)
            {
                Vector2 spot = BurrowSpots[i];
                Vector3 onRoad = FieldLayout.PointAlong(layout.Road, spot.x);
                Vector3 ahead = FieldLayout.PointAlong(layout.Road, spot.x + 0.5f) - onRoad;
                Vector3 side = Vector3.Cross(Vector3.up, ahead.normalized);
                Vector3 position = layout.Ground(onRoad + side * spot.y);
                var root = new GameObject($"Burrow{i}").transform;
                root.SetParent(Root, false);
                // README round 23: yaw 0 keeps the burrow's camouflage in line with the ground texture.
                root.position = position;
                var burrow = new Burrow { Root = root, Open = FirstOpen + i * OpenGap };
                if (art != null)
                {
                    burrow.Model = ArtModel.CreateSampled(root, art.Model, ownMaterial: true);
                    SpawnOriginView.KeepBoundsLive(burrow.Model);
                    burrow.Glow = burrow.Model.Material.GetColor(EmissionColorId);
                    burrow.Emerge = burrow.Model.Find("Emerge");
                }

                if (burrow.Emerge == null)
                {
                    burrow.Emerge = root;
                }

                Transform glow = burrow.Model != null ? burrow.Model.Find("Glow") : null;
                if (glow == null)
                {
                    glow = root;
                }

                burrow.Light = AddLight(glow, FlashColour, LightRange);
                burrow.Cracks = BuildCracks(root, i);
                if (burst != null)
                {
                    burrow.Dust = CutsceneBursts.Burst(root, "BurstDust", Vector3.up * 0.2f, burst.DustSheet != null ? burst.DustSheet : burst.SmokeSheet, low, context.Scaled(BurstDust), burrow.Open + BurstAt, new Vector2(1.2f, 2.4f), new Vector2(0.8f, 2.4f), -0.05f, new Color(0.6f, 0.55f, 0.5f, 0.8f), 0.6f, VfxParticles.Frames.OverLife);
                    burrow.Debris = CutsceneBursts.Burst(root, "BurstDebris", Vector3.up * 0.3f, burst.DebrisSheet, low, context.Scaled(BurstDebris), burrow.Open + BurstAt, new Vector2(0.12f, 0.3f), new Vector2(3f, 6.5f), 1.6f, Color.white, 0.4f, VfxParticles.Frames.RandomCell);
                    var flash = new GameObject("BurstFlash").AddComponent<ImpactFlash>();
                    flash.transform.SetParent(root, false);
                    flash.Configure(new VfxQuality(context.ParticleScale, low, !low));
                    flash.gameObject.SetActive(false);
                    burrow.Flash = flash;
                }

                if (burrow.Model != null)
                {
                    _burrows.Add(burrow);
                }
            }

            if (_burrows.Count == 0)
            {
                Debug.LogWarning("The cutscene swarm has no burrow art (the level's ground spawn origin); no creature breaks out.");
            }

            Vector3 entranceSide = layout.EntranceFacing * Vector3.right;
            _screamerSpot = layout.Ground(layout.Entrance + layout.EntranceFacing * Vector3.forward * 1.2f + entranceSide * 1.6f);
        }

        // Fissures radiating from the opening, each a Blender crack strip that grows out from the maw.
        private Transform[] BuildCracks(Transform root, int burrow)
        {
            var cracks = new Transform[CracksPerBurrow];
            for (int i = 0; i < CracksPerBurrow; i++)
            {
                float yaw = i * (360f / CracksPerBurrow) + CutsceneMath.Hash(burrow * 7 + i, 3) * 50f;
                var pivot = new GameObject($"Crack{i}").transform;
                pivot.SetParent(root, false);
                pivot.localPosition = Vector3.up * 0.04f;
                pivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
                // The strip runs from the maw outwards: its quad sits half a length out, flat on the ground.
                Transform strip = Decal("Strip", _crackMaterial, Vector3.zero);
                strip.SetParent(pivot, false);
                strip.localPosition = new Vector3(0f, 0f, CrackLength * 0.5f + 0.6f);
                strip.localRotation = Quaternion.Euler(90f, 0f, 0f);
                strip.localScale = new Vector3(CrackWidth, CrackLength, 1f);
                strip.gameObject.SetActive(true);
                cracks[i] = pivot;
            }

            return cracks;
        }

        private void BuildCreatures(FieldLayout layout, CutsceneContext context)
        {
            CutsceneCast cast = context.Cast;
            if (cast == null || cast.Swarm == null || cast.Swarm.Length == 0 || _burrows.Count == 0)
            {
                return;
            }

            int walkers = context.Scaled(cast.SwarmCount);
            int flyers = cast.Flyers != null && cast.Flyers.Length > 0 ? context.Scaled(cast.FlyerCount) : 0;
            int total = walkers + flyers;
            for (int i = 0; i < total; i++)
            {
                bool flies = i % Mathf.Max(1, total / Mathf.Max(1, flyers)) == 1 && flyers > 0;
                EnemyDefinition definition = flies ? cast.Flyers[i % cast.Flyers.Length] : cast.Swarm[i % cast.Swarm.Length];
                if (definition == null)
                {
                    continue;
                }

                Burrow burrow = _burrows[i % _burrows.Count];
                int wave = i / _burrows.Count;
                float first = burrow.Open + EmergeAt;
                float emerge = Mathf.Min(StreamEnd, first + wave * StreamGap + CutsceneMath.Hash(i, 17) * 0.3f);
                float lane = (CutsceneMath.Hash(i, 11) - 0.5f) * 2f * LaneWidth;
                Vector3 start = burrow.Emerge.position;
                var creature = new Creature
                {
                    Definition = definition,
                    Route = RouteFrom(layout, start, lane),
                    Emerge = emerge,
                    Speed = RushShare * (0.9f + 0.2f * CutsceneMath.Hash(i, 19)),
                    Flies = flies,
                    GroundY = burrow.Root.position.y,
                    Index = i,
                };
                creature.Enemy = Spawn(definition, i);
                _creatures.Add(creature);
            }

            if (cast.Screamer != null)
            {
                Vector3 ahead = FieldLayout.PointAlong(layout.Road, ScreamerWalk);
                _screamer = new Creature
                {
                    Definition = cast.Screamer,
                    Route = new[] { _screamerSpot, layout.Ground(ahead) },
                    Emerge = ScreamerEmerge,
                    Speed = ScreamerSpeed,
                };
                _screamer.Enemy = Spawn(cast.Screamer, total);
                _screamer.Enemy.transform.localScale = Vector3.one * ScreamerScale;
                _creatures.Add(_screamer);
            }
        }

        // The crowd at the first gate: a ring at the field, then rings further back towards the entrance that start to hold
        // walkers one after another once the first ones have arrived, so the crowd grows backwards instead of stacking.
        private void BuildRings(FieldLayout layout)
        {
            float front = float.MaxValue;
            foreach (Creature creature in _creatures)
            {
                if (creature.Flies || creature == _screamer)
                {
                    continue;
                }

                float distance = layout.GateCentreDistance - FieldLayout.DistanceAlong(layout.Road, creature.Route[0]);
                front = Mathf.Min(front, creature.Emerge + Mathf.Max(0f, distance) / Mathf.Max(0.1f, creature.Definition.Speed * creature.Speed));
            }

            if (front == float.MaxValue)
            {
                return;
            }

            for (int k = 0; k < Rings; k++)
            {
                Vector3 centre = FieldLayout.PointAlong(layout.Road, layout.GateCentreDistance - k * RingSpacing);
                _structures.Add(new HoldRing(this, centre, k == 0 ? 0f : front + k * RingSeconds));
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

        // From the maw to the road just ahead, then down the road on a lane of its own all the way to the base.
        private static Vector3[] RouteFrom(FieldLayout layout, Vector3 start, float lane)
        {
            Vector3[] road = layout.Road;
            float joinAlong = FieldLayout.DistanceAlong(road, start) + 1.2f;
            var points = new List<Vector3> { start, Offset(road, joinAlong, lane, FieldLayout.PointAlong(road, joinAlong)) };
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

        // A Blender cutscene sprite, additive in its HDR colour; the painted radial glow without the art.
        private static Material Additive(CutsceneSprite sprite, Color colour)
        {
            Texture2D texture = CutsceneFxSet.Get(sprite);
            return texture != null ? GlowDecal.Additive(texture, colour) : new Material(GlowDecal.RadialMaterial(colour));
        }

        private static Light AddLight(Transform parent, Color colour, float range)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.range = range;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }
    }
}
