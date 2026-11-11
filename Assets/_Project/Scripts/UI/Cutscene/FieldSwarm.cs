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
    /// The threat: the ground bursts open beside the road at the entrance, one living burrow after another (the level's
    /// Blender spawn origin with its own opening: tremor, cracks spreading, the burst with dust, thrown slabs and a flash,
    /// then the open maw), and a small brood of different species climbs out and takes up its place in slow steps. The
    /// Queen rises in front of them, walks up to the head of the group, rears and screams; then they all stand and glare
    /// at the colony, turning their heads now and then, a flyer or two hovering over them. Later they creep a few metres
    /// closer to the gate and stand again. Nothing runs, nothing rolls: the menace is in the stillness (Remco,
    /// 2026-11-05). The creatures are the game's own <see cref="Enemy"/> and <see cref="EnemyView"/>, placed by the swarm
    /// clock (seconds since the first tremor) as a pure function of time, so any moment can be sought.
    /// </summary>
    public sealed class FieldSwarm
    {
        private const float SeekThreshold = 0.6f;
        // Longer than any creature's climb out of the ground.
        private const float SpawnAge = 1.5f;

        // The burrows (README round 23): the opening clip's tremor, burst (1.8 s), maw up, the first creature at 2.53 s.
        private const float BurstAt = 1.8f;
        private const float EmergeAt = 2.53f;
        private const float GlowRampSeconds = 1f;
        private const float FirstOpen = 0.4f;
        private const float OpenGap = 0.8f;
        private const float EmergeGap = 0.7f;
        private const float BurstLight = 10f;
        // An open maw lights the ground and the creatures round it from below, warm, so they read against the night.
        private const float OpenLight = 3.2f;
        private const float LightRange = 7f;
        private const float FlashSeconds = 0.45f;
        private const int CracksPerBurrow = 3;
        private const float CrackLength = 2.6f;
        private const float CrackWidth = 0.55f;
        private const float CrackGlow = 2.4f;
        private const int BurstDust = 14;
        private const int BurstDebris = 22;

        // The brood climbs out, then walks to its place in slow, deliberate steps (world units per second).
        private const float RiseSeconds = 1.1f;
        private const float BroodPace = 0.75f;
        private const float QueenPace = 0.6f;
        // Later they all creep this far further down the road towards the gate, slower still, one after another.
        private const float AdvanceAt = 12.5f;
        private const float AdvanceStagger = 0.9f;
        private const float AdvanceDistance = 3f;
        private const float AdvancePace = 0.35f;
        // Standing, they turn towards the colony and their heads keep searching a little.
        private const float TurnSeconds = 0.8f;
        private const float GlareSwayDegrees = 9f;
        private const float GlareSwayRate = 0.35f;
        // A creature with a strike of its own snaps at the air now and then while it stands.
        private const float SnapGap = 4.2f;
        private const float SnapJitter = 1.6f;

        private const float FlyerLiftSeconds = 1.4f;
        private const float HoverBob = 0.18f;
        private const float HoverBobSeconds = 2.8f;

        private const float QueenEmerge = 3.2f;
        private const float ScreamAt = 7.8f;
        private const float RingSecondsScream = 1.3f;
        private const float ScreamRadius = 6f;
        private const float RearHeight = 0.2f;
        private const float QueenScale = 0.8f;

        // Playtest note 4 (Remco 2026-11-08): before the game starts they roar once more, turn and run back into the ground,
        // so the first wave reads as the first wave. From the roar: a beat of rage, the run (staggered), the dive, the burrows close.
        private const float RoarSeconds = 1.1f;
        private const float RetreatStagger = 0.45f;
        private const float RetreatTurnSeconds = 0.35f;
        private const float RetreatPace = 3.2f;
        private const float SinkSeconds = 0.7f;
        private const float SinkDepth = 1.6f;
        private const float CloseDelay = 0.2f;
        private const float CloseSpeed = 1.6f;
        // Round 44: the burrows shut one after another, this far apart, each with its own closing sound. The curator's files
        // put the slam this long before their end, so a file starts where its slam lands on the frame the burrow shuts.
        private const float CloseStagger = 0.45f;
        private const float CloseSlamBeforeEnd = 0.15f;
        private const float BurrowCloseVolume = 0.8f;
        // The ground trembles as a burrow cracks open and dies down after its burst; it shakes again as the slabs fold shut.
        private const float BurstTrembleDecay = 0.8f;
        private const float ClosingTremble = 0.6f;
        private static readonly Color CrackColour = new Color(1f, 0.32f, 0.12f);
        private static readonly Color RingColour = new Color(1f, 0.35f, 0.2f);
        private static readonly Color FlashColour = new Color(1f, 0.45f, 0.18f);
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // Where the burrows break open: distance along the road from the entrance and to its side (negative: left).
        private static readonly Vector2[] BurrowSpots = { new Vector2(1.2f, 3.1f), new Vector2(3.2f, -3.4f), new Vector2(0.4f, -1.6f) };

        // Where each of the brood first stands (along the road, to its side): a loose, uneven group flanking the Queen, so
        // the low camera in front of her sees them on both sides of her, never a line. Flyers hover over theirs at the height in z.
        private static readonly Vector2[] WalkerSpots = { new Vector2(5.6f, -2.2f), new Vector2(5.3f, 1.9f), new Vector2(3.9f, -3f), new Vector2(4f, 2.6f), new Vector2(6.4f, -1.2f), new Vector2(2.4f, 1.7f) };
        private static readonly Vector3[] FlyerSpots = { new Vector3(3.4f, 1.4f, 3f), new Vector3(3f, -1.8f, 2.6f) };

        // The Queen climbs out a little behind the head of the group and walks up to it.
        private static readonly Vector2 QueenRise = new Vector2(3.8f, 0.6f);
        private static readonly Vector2 QueenStand = new Vector2(5.4f, 0f);

        private readonly List<Creature> _creatures = new List<Creature>();
        private readonly List<Burrow> _burrows = new List<Burrow>();
        private readonly TargetRegistry _targets = new TargetRegistry();
        private readonly StructureRegistry _structures = new StructureRegistry();
        private readonly CutsceneContext _context;
        private readonly FieldLayout _layout;
        private readonly Transform _ring;
        private readonly Material _ringMaterial;
        private readonly Material _crackMaterial;
        private Creature _queen;
        private float _lastClock = -1f;
        private float _retreatAt = float.PositiveInfinity;
        private float _closeAt = float.PositiveInfinity;

        /// <summary>One creature of the brood and its choreography: out of the ground, to its place, later a little closer.</summary>
        private sealed class Creature
        {
            public Enemy Enemy;
            public EnemyView View;
            public EnemyDefinition Definition;
            public int Index;
            public float Emerge;
            public Vector3 From;
            public Vector3 Stand;
            public Vector3 Advance;
            public float Pace;
            public float AdvanceFrom;
            public float RetreatFrom = float.MaxValue;
            public float Hover;
            public float SnapUntil = -1f;
            public float NextSnap;
            public bool Active;

            public float WalkFrom => Emerge + RiseSeconds;
            public float WalkTo => WalkFrom + Vector3.Distance(Flat(From), Flat(Stand)) / Pace;
            public float AdvanceTo => AdvanceFrom + Vector3.Distance(Flat(Stand), Flat(Advance)) / AdvancePace;
            public float RetreatTo => RetreatFrom + Vector3.Distance(Flat(Advance), Flat(From)) / RetreatPace;
            public float GoneAt => RetreatTo + SinkSeconds;
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
            public int Index;
            public bool CloseSounded;
        }

        public FieldSwarm(Transform parent, FieldLayout layout, CutsceneContext context)
        {
            _context = context;
            _layout = layout;
            Root = new GameObject("Swarm").transform;
            Root.SetParent(parent, false);
            _crackMaterial = Additive(CutsceneSprite.CrackSwarmA, CrackColour * CrackGlow);
            _ringMaterial = Additive(CutsceneSprite.ScreamRing, RingColour);
            BuildBurrows(context);
            BuildCreatures(context);
            Vector3 ringAt = _queen != null ? _queen.Stand : OnRoad(QueenStand);
            _ring = Decal("ScreamRing", _ringMaterial, ringAt + Vector3.up * 0.06f);
        }

        public Transform Root { get; }

        /// <summary>Where the Queen stands at the head of her brood (what the commander turns to face).</summary>
        public Vector3 QueenSpot => _queen != null ? _queen.Stand : OnRoad(QueenStand);

        /// <summary>
        /// Swarm clock of the last roar, after which the brood runs back into the ground; infinite holds them where they
        /// stand. Round 39 (Remco): they stay until the player has read the last line, so the field set sets it only once
        /// the handoff shot has begun, never from a fixed time.
        /// </summary>
        public float RetreatClock => _retreatAt;

        /// <summary>Holds the brood (infinite) or lets it roar and run back from this swarm clock on.</summary>
        public void SetRetreat(float clock)
        {
            if (clock == _retreatAt)
            {
                return;
            }

            _retreatAt = clock;
            foreach (Creature creature in _creatures)
            {
                creature.RetreatFrom = RetreatFrom(creature.Index);
            }

            _closeAt = float.IsPositiveInfinity(clock) ? clock : LastGone() + CloseDelay;
        }

        /// <summary>True while no creature has begun to turn away at this clock: the brood still stands and glares.</summary>
        public bool Holding(float clock)
        {
            foreach (Creature creature in _creatures)
            {
                if (clock >= creature.RetreatFrom - RetreatTurnSeconds)
                {
                    return false;
                }
            }

            return true;
        }

        private float RetreatFrom(int index) => _retreatAt + RoarSeconds + CutsceneMath.Hash(index, 47) * RetreatStagger;

        // The moment the last of them is under the ground, when the burrows close behind them.
        private float LastGone()
        {
            float last = _retreatAt;
            foreach (Creature creature in _creatures)
            {
                last = Mathf.Max(last, creature.GoneAt);
            }

            return last;
        }

        /// <summary>
        /// How hard the ground round the burrows trembles at a swarm clock, 0..1: rising as the cracks spread, dying down after
        /// the burst, and again while the slabs fold shut behind the brood.
        /// </summary>
        public float Tremble(float clock)
        {
            float tremble = 0f;
            foreach (Burrow burrow in _burrows)
            {
                float since = clock - burrow.Open;
                if (since > 0f)
                {
                    float shake = since < BurstAt ? CutsceneMath.Span(since, 0f, BurstAt) : Mathf.Exp(-(since - BurstAt) / BurstTrembleDecay);
                    tremble = Mathf.Max(tremble, shake);
                }

                float shut = CloseStartOf(burrow);
                if (clock > shut && clock < CloseTimeOf(burrow))
                {
                    tremble = Mathf.Max(tremble, ClosingTremble);
                }
            }

            return tremble;
        }

        /// <summary>The swarm clock at which a burrow has folded fully shut (infinite while the brood holds), for its sound and tests.</summary>
        public float CloseTimeOf(int burrow) => burrow >= 0 && burrow < _burrows.Count ? CloseTimeOf(_burrows[burrow]) : float.PositiveInfinity;

        public int BurrowCount => _burrows.Count;

        // Each burrow folds its opening clip backwards from its own start; it is shut when the clip is back at its first frame.
        private float CloseStartOf(Burrow burrow) => _closeAt + burrow.Index * CloseStagger;

        private float CloseTimeOf(Burrow burrow) => CloseStartOf(burrow) + burrow.Model.LengthOf(ModelClipRole.Open) / CloseSpeed;

        // Round 44 (Remco: the slam "must be 100% well timed"): each burrow's file starts so the slam inside it lands on the frame
        // that burrow shuts. From the clock alone: a seek into the window picks the file up where it would be, a seek back
        // before it arms it again, a seek past its end stays silent.
        private void SoundCloses(float clock, bool played)
        {
            foreach (Burrow burrow in _burrows)
            {
                // A jump stopped every one-shot in flight: a burrow whose slam is still to come picks its file up again.
                if (!played)
                {
                    burrow.CloseSounded = false;
                }

                AudioClip clip = _context.Audio.Variant(CutsceneSound.BurrowClose, burrow.Index, out float gain);
                float shut = CloseTimeOf(burrow);
                if (clip == null || float.IsInfinity(shut))
                {
                    burrow.CloseSounded = false;
                    continue;
                }

                float start = shut - (clip.length - CloseSlamBeforeEnd);
                if (clock < start)
                {
                    burrow.CloseSounded = false;
                    continue;
                }

                if (!burrow.CloseSounded)
                {
                    burrow.CloseSounded = true;
                    _context.Audio.PlayFrom(clip, BurrowCloseVolume * gain, clock - start);
                }
            }
        }

        /// <summary>The shot of the attack begins: its burst effects start their clock; in any other shot they rest.</summary>
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

            bool played = _lastClock >= 0f && clock >= _lastClock && clock - _lastClock < SeekThreshold;
            foreach (Burrow burrow in _burrows)
            {
                PoseBurrow(burrow, clock, time);
            }

            foreach (Creature creature in _creatures)
            {
                Place(creature, clock, played);
            }

            PoseQueen(clock, played);
            SoundCloses(clock, played);
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
            // Once the last of them has dived in, the opening runs backwards: the slabs fold shut and the glow dies.
            float closing = (clock - CloseStartOf(burrow)) * CloseSpeed;
            if (closing > 0f)
            {
                float openLength = model.LengthOf(ModelClipRole.Open);
                model.Sample(ModelClipRole.Open, Mathf.Max(0f, openLength - closing), loop: false);
                glow *= 1f - CutsceneMath.Span(closing, 0f, openLength);
            }

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

        // The ring of the scream spreads from her feet; she swells for it and plays her own scream when the moment is played
        // (a seek shows her standing). Size, not height: a lift would read as walking to the view.
        private void PoseQueen(float clock, bool played)
        {
            // Two screams: the first at the head of her brood, the last one before they all go back into the ground.
            float since = clock >= _retreatAt ? clock - _retreatAt : clock - ScreamAt;
            float ring = CutsceneMath.Span(since, 0f, RingSecondsScream);
            _ring.gameObject.SetActive(since > 0f && ring < 1f);
            if (_queen != null && _queen.Active)
            {
                _ring.position = _queen.Enemy.transform.position + Vector3.up * 0.06f;
            }

            float radius = ScreamRadius * CutsceneMath.Smooth(ring);
            _ring.localScale = new Vector3(Mathf.Max(0.001f, radius * 2f), Mathf.Max(0.001f, radius * 2f), 1f);
            GlowDecal.SetColour(_ringMaterial, RingColour * (1.5f * (1f - ring)));
            if (_queen == null || !_queen.Active)
            {
                return;
            }

            if (played && ((_lastClock < ScreamAt && clock >= ScreamAt) || (_lastClock < _retreatAt && clock >= _retreatAt)))
            {
                Strike(_queen, ModelClipRole.Power, clock);
            }

            float rear = Mathf.Sin(CutsceneMath.Span(since, -0.4f, 1.6f) * Mathf.PI);
            _queen.Enemy.transform.localScale = Vector3.one * (QueenScale * (1f + 0.08f * rear));
        }

        // Out of the ground when its time comes, then wherever its choreography has it at this moment. A jump in time (a
        // seek, the next shot) starts the creature over at the moment, without a second climb out of the ground.
        private void Place(Creature creature, float clock, bool played)
        {
            GameObject go = creature.Enemy.gameObject;
            if (clock < creature.Emerge || clock >= creature.GoneAt)
            {
                if (creature.Active)
                {
                    EndStrike(creature);
                    creature.Enemy.Deactivate();
                    go.SetActive(false);
                    creature.Active = false;
                }

                return;
            }

            Vector3 position = PositionAt(creature, clock, out Vector3 heading);
            if (!creature.Active || !played)
            {
                EndStrike(creature);
                go.SetActive(true);
                creature.Enemy.Deactivate();
                creature.Enemy.Initialize(creature.Definition, new[] { position, position + heading }, 1f, 1f, 0f, _targets, _structures, 1f, EnemyOrigin.Nest);
                if (clock - creature.Emerge > SpawnAge)
                {
                    creature.View.SkipSpawn();
                }

                creature.Active = true;
                creature.NextSnap = Mathf.Max(FirstSnap(creature), clock + CutsceneMath.Hash(creature.Index, 29) * SnapJitter);
            }

            creature.Enemy.Place(position, heading);
            // A strike ends with its clip, and at the latest when the run back begins.
            if (creature.SnapUntil >= 0f && (clock >= creature.SnapUntil || clock >= creature.RetreatFrom))
            {
                EndStrike(creature);
            }

            if (played && creature != _queen && _lastClock < _retreatAt && clock >= _retreatAt)
            {
                EndStrike(creature);
                Roar(creature, clock);
            }
            else if (played && creature != _queen && clock < _retreatAt)
            {
                Snap(creature, clock);
            }
        }

        // Rising out of the maw, a slow walk to its place, a turn to the colony; later the same again a few metres closer.
        // Standing, the head keeps searching: a slow sway of the heading round the line to the gate.
        private Vector3 PositionAt(Creature creature, float clock, out Vector3 heading)
        {
            if (clock >= creature.RetreatFrom - RetreatTurnSeconds)
            {
                return RetreatAt(creature, clock, out heading);
            }

            Vector3 position;
            Vector3 travel;
            float turn;
            if (clock < creature.AdvanceFrom)
            {
                position = Vector3.Lerp(creature.From, creature.Stand, CutsceneMath.Smooth(CutsceneMath.Span(clock, creature.WalkFrom, creature.WalkTo)));
                travel = creature.Stand - creature.From;
                turn = CutsceneMath.Smooth(CutsceneMath.Span(clock, creature.WalkTo, creature.WalkTo + TurnSeconds));
            }
            else
            {
                position = Vector3.Lerp(creature.Stand, creature.Advance, CutsceneMath.Smooth(CutsceneMath.Span(clock, creature.AdvanceFrom, creature.AdvanceTo)));
                travel = creature.Advance - creature.Stand;
                turn = clock < creature.AdvanceTo
                    ? 1f - CutsceneMath.Smooth(CutsceneMath.Span(clock, creature.AdvanceFrom, creature.AdvanceFrom + TurnSeconds))
                    : CutsceneMath.Smooth(CutsceneMath.Span(clock, creature.AdvanceTo, creature.AdvanceTo + TurnSeconds));
            }

            position = _layout.Ground(position);
            Vector3 look = Flat(_layout.GateCentre - position).normalized;
            float sway = Mathf.Sin((clock * GlareSwayRate + CutsceneMath.Hash(creature.Index, 31)) * Mathf.PI * 2f) * GlareSwayDegrees;
            look = Quaternion.Euler(0f, sway, 0f) * look;
            travel = Flat(travel);
            Vector3 along = travel.sqrMagnitude > 0.0001f ? travel.normalized : look;
            heading = Vector3.Slerp(along, look, turn);
            if (creature.Hover > 0f)
            {
                float age = clock - creature.Emerge;
                float lift = CutsceneMath.Smooth(CutsceneMath.Span(age, 0f, FlyerLiftSeconds));
                float bob = Mathf.Sin((age / HoverBobSeconds + CutsceneMath.Hash(creature.Index, 41)) * Mathf.PI * 2f) * HoverBob;
                position.y += (creature.Hover + bob) * lift;
            }

            return position;
        }

        // The run back: a quick turn away from the gate (by yaw, so it never flips over), a run to the maw it came out of
        // (the Queen to where she rose), then down into the ground. A flyer comes down to the maw as it goes.
        private Vector3 RetreatAt(Creature creature, float clock, out Vector3 heading)
        {
            float run = CutsceneMath.Smooth(CutsceneMath.Span(clock, creature.RetreatFrom, creature.RetreatTo));
            Vector3 position = _layout.Ground(Vector3.Lerp(creature.Advance, creature.From, run));
            Vector3 back = Flat(creature.From - creature.Advance);
            Vector3 look = Flat(_layout.GateCentre - creature.Advance);
            float lookYaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
            float backYaw = back.sqrMagnitude > 0.0001f ? Mathf.Atan2(back.x, back.z) * Mathf.Rad2Deg : lookYaw + 180f;
            float turn = CutsceneMath.Smooth(CutsceneMath.Span(clock, creature.RetreatFrom - RetreatTurnSeconds, creature.RetreatFrom));
            heading = Quaternion.Euler(0f, Mathf.LerpAngle(lookYaw, backYaw, turn), 0f) * Vector3.forward;

            float sink = CutsceneMath.Smooth(CutsceneMath.Span(clock, creature.RetreatTo, creature.GoneAt));
            if (creature.Hover > 0f)
            {
                position.y += creature.Hover * (1f - Mathf.Max(run, sink));
            }

            position.y -= SinkDepth * sink;
            return position;
        }

        // The last roar before they go: the creature's own power clip, or its strike when it has no power of its own.
        private static void Roar(Creature creature, float clock)
        {
            ModelClipRole role = creature.View.ClipOf(ModelClipRole.Power) != null ? ModelClipRole.Power : ModelClipRole.Attack;
            Strike(creature, role, clock);
        }

        private static bool IsStanding(Creature creature, float clock) => clock >= creature.WalkTo && (clock < creature.AdvanceFrom || clock >= creature.AdvanceTo);

        private static float FirstSnap(Creature creature) => creature.WalkTo + TurnSeconds + CutsceneMath.Hash(creature.Index, 37) * SnapJitter;

        // Now and then, while it stands, a creature with a strike of its own snaps at the air: a warning, not an attack.
        private static void Snap(Creature creature, float clock)
        {
            if (creature.SnapUntil >= 0f || clock < creature.NextSnap)
            {
                return;
            }

            if (IsStanding(creature, clock))
            {
                Strike(creature, ModelClipRole.Attack, clock);
            }

            creature.NextSnap = clock + SnapGap + CutsceneMath.Hash(creature.Index, Mathf.FloorToInt(clock)) * SnapJitter;
        }

        private static void Strike(Creature creature, ModelClipRole role, float clock)
        {
            ModelClip clip = creature.View.ClipOf(role);
            if (clip == null)
            {
                return;
            }

            creature.View.PlayScripted(role);
            creature.SnapUntil = clock + clip.Length;
        }

        private static void EndStrike(Creature creature)
        {
            if (creature.SnapUntil < 0f)
            {
                return;
            }

            creature.View.ReleaseScript();
            creature.SnapUntil = -1f;
        }

        private void BuildBurrows(CutsceneContext context)
        {
            SpawnOriginDefinition origins = context.FieldLevel.SpawnOrigins;
            SpawnOriginArt art = origins != null ? origins.For(TerrainKind.Ground) : null;
            bool low = context.ParticleScale < 1f;
            ImpactVfxDefinition burst = origins != null ? origins.BurstVfx : null;
            for (int i = 0; i < BurrowSpots.Length; i++)
            {
                Vector3 position = OnRoad(BurrowSpots[i]);
                var root = new GameObject($"Burrow{i}").transform;
                root.SetParent(Root, false);
                // README round 23: yaw 0 keeps the burrow's camouflage in line with the ground texture.
                root.position = position;
                var burrow = new Burrow { Root = root, Open = FirstOpen + i * OpenGap, Index = i };
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

        // A few of the brood, each from a burrow in turn, a flyer or two over them, and the Queen at their head.
        private void BuildCreatures(CutsceneContext context)
        {
            CutsceneCast cast = context.Cast;
            if (cast == null || _burrows.Count == 0)
            {
                return;
            }

            int walkers = cast.Swarm != null && cast.Swarm.Length > 0 ? Mathf.Min(context.Scaled(cast.SwarmCount), WalkerSpots.Length) : 0;
            int flyers = cast.Flyers != null && cast.Flyers.Length > 0 ? Mathf.Min(context.Scaled(cast.FlyerCount), FlyerSpots.Length) : 0;
            for (int i = 0; i < walkers; i++)
            {
                AddFromBurrow(cast.Swarm[i % cast.Swarm.Length], i, WalkerSpots[i], 0f);
            }

            for (int i = 0; i < flyers; i++)
            {
                Vector3 spot = FlyerSpots[i];
                AddFromBurrow(cast.Flyers[i % cast.Flyers.Length], walkers + i, new Vector2(spot.x, spot.y), spot.z);
            }

            if (cast.Screamer != null)
            {
                _queen = Add(cast.Screamer, walkers + flyers, QueenEmerge, OnRoad(QueenRise), QueenStand, QueenPace, AdvanceAt, 0f);
            }
        }

        private void AddFromBurrow(EnemyDefinition definition, int index, Vector2 spot, float hover)
        {
            if (definition == null)
            {
                return;
            }

            Burrow burrow = _burrows[index % _burrows.Count];
            int turn = index / _burrows.Count;
            float emerge = burrow.Open + EmergeAt + turn * EmergeGap + CutsceneMath.Hash(index, 17) * 0.3f;
            float pace = BroodPace * (0.85f + 0.3f * CutsceneMath.Hash(index, 19));
            float advance = AdvanceAt + AdvanceStagger * (1f + CutsceneMath.Hash(index, 23) * 2f);
            Add(definition, index, emerge, _layout.Ground(burrow.Emerge.position), spot, pace, advance, hover);
        }

        private Creature Add(EnemyDefinition definition, int index, float emerge, Vector3 from, Vector2 spot, float pace, float advance, float hover)
        {
            var creature = new Creature
            {
                Definition = definition,
                Index = index,
                Emerge = emerge,
                From = from,
                Stand = OnRoad(spot),
                Advance = OnRoad(spot + Vector2.right * AdvanceDistance),
                Pace = pace,
                AdvanceFrom = advance,
                RetreatFrom = RetreatFrom(index),
                Hover = hover,
            };
            creature.Enemy = Spawn(definition, index);
            creature.View = creature.Enemy.GetComponent<EnemyView>();
            _creatures.Add(creature);
            return creature;
        }

        // A spot given as distance along the road from the entrance and to its side (negative: left), on the ground.
        private Vector3 OnRoad(Vector2 spot)
        {
            Vector3 onRoad = FieldLayout.PointAlong(_layout.Road, spot.x);
            Vector3 ahead = FieldLayout.PointAlong(_layout.Road, spot.x + 0.5f) - onRoad;
            Vector3 side = Vector3.Cross(Vector3.up, ahead.normalized);
            return _layout.Ground(onRoad + side * spot.y);
        }

        private static Vector3 Flat(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);

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
