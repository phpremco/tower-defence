using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The body every continent boss shares (Docs/BOSSES.md): its entrance out of a pool, walking back to the road round
    /// towers, climbing rocks, gates and the base (a gate sags, takes its weight and lets the walkers follow over it),
    /// breaking fragile decoration, and diving into pools within the rules. Its one signature mechanic is a separate
    /// <see cref="IBossSignature"/> chosen from its data; the brain asks it each step on the road whether it wants to act.
    /// While it acts the creature is directed and its own walk waits; between acts it walks the road as any creature.
    /// Logic only: it moves the creature and tells the views what to show.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class ContinentBossBrain : MonoBehaviour
    {
        private const float BreakEverySeconds = 0.2f;
        // The wake sheet across: about 1.2 times the body (README round 29), the body twice its half width.
        private float WakeSize => _boss.BodyHalfWidth * 2.4f;
        private const float BodyRadiusPerScale = 0.9f;
        private const float DiveCheckSeconds = 0.4f;
        private const float RoarRate = 1.2f;
        private const float HeightFollow = 6f;
        private const float MaxSlopeTilt = 0.6f;
        private const float BreakoutBurstRadius = 1.8f;
        private const float DebrisBurstRadius = 0.6f;
        private const int MaxReplans = 4;

        // Gate crossings (README round 33): a gate it lies on keeps at least this burden so the walkers keep their bridge; a
        // blow on a gate raises dust this wide and kicks the camera at this share of its impact kick.
        private const float StandingBurden = 0.05f;
        private const float GateDustRadius = 1.1f;
        private const float GateBlowKick = 0.45f;
        private const float MinClipSeconds = 0.1f;
        private const string Battering = "Battering";
        private static readonly Color GateDust = new Color(0.62f, 0.6f, 0.56f);

        // Where it comes out of a pool: the bank nearest the road weighs this much more than the far side of the pool.
        private const float RoadPull = 4f;
        private const float RefreshSeconds = 0.5f;

        // A trip from one pool to another swims at most this many times its own speed, so it never flashes across the map.
        private const float ShortcutSpeedUp = 2f;

        // Before it rises out of its pool the surface churns this long: the warning.
        private const float StirSeconds = 1.6f;

        // A bank where its body would come up over a tower loses this much against one where it would not.
        private const float BodyOverlapCost = 40f;
        private const int EdgeCells = 2;
        private static readonly Vector2Int NoCell = new Vector2Int(-1, -1);

        // A boss rising out of a pool takes one that joins the road no later than this share of the way to the base.
        private const float MaxEntranceRoadShare = 0.5f;

        private static readonly Color CrystalShards = new Color(0.7f, 0.85f, 1f);
        private static readonly Color SandSplash = new Color(0.85f, 0.72f, 0.45f);
        private static readonly Color WaterSplash = new Color(0.55f, 0.85f, 1f);
        private static readonly Color LavaSplash = new Color(1f, 0.45f, 0.12f);
        private static readonly Color IceSplash = new Color(0.85f, 0.95f, 1f);
        private static readonly Color Bubbles = new Color(0.85f, 0.9f, 1f, 0.7f);

        private readonly List<Vector3> _route = new List<Vector3>(64);
        private readonly DamageTally<Tower> _tally = new DamageTally<Tower>();
        private readonly BossRecord _record = new BossRecord();
        private BossField _field;
        private Enemy _enemy;
        private EnemyView _view;
        private BossDefinition _boss;
        private IBossSignature _signature;
        private BossSignature _signatureKind = (BossSignature)(-1);
        private PoolDiveRules _dives;
        private TargetRingView _ring;
        private WakeView _wake;
        private float _sinceBreak;
        private float _sinceDiveCheck;
        private bool _rocksClosed;
        private Coroutine _life;

        /// <summary>What it is doing right now, for probes and tests.</summary>
        public string State { get; internal set; } = "Idle";

        /// <summary>What it did in this fight.</summary>
        public BossRecord Record => _record;

        /// <summary>How many times it has gone under at a threshold.</summary>
        public int Burrows => _record.Burrows;

        /// <summary>How many times it hid in a pool.</summary>
        public int Dives => _record.Dives;

        /// <summary>How many rocks, gates or base roofs it climbed.</summary>
        public int Climbs => _record.Climbs;

        /// <summary>Every tower it has thrown, in order.</summary>
        public IReadOnlyList<Tower> Thrown => _record.Thrown;

        /// <summary>True while it stands on a gate or the base roof (its footprint overlaps the structure by design).</summary>
        public bool OnStructure { get; internal set; }

        /// <summary>True while it is under the ground or a liquid.</summary>
        public bool Hidden => _enemy != null && _enemy.IsConcealed;

        public BossDefinition Boss => _boss;

        /// <summary>Its signature mechanic, for the tests.</summary>
        public IBossSignature Signature => _signature;

        internal BossField Field => _field;
        internal Enemy Enemy => _enemy;
        internal EnemyView View => _view;
        internal DamageTally<Tower> Tally => _tally;
        internal TargetRingView Ring => _ring;
        internal bool Alive => _enemy != null && _enemy.IsAlive;
        internal float Scale => _view != null ? _view.ModelScale : 1f;

        /// <summary>Half the width of its body, legs included, in world units (its data).</summary>
        internal float BodyHalfWidth => _boss.BodyHalfWidth;

        public void Initialize(BossField field)
        {
            _field = field;
            _enemy = GetComponent<Enemy>();
            _view = GetComponent<EnemyView>();
            _ring = TargetRingView.Create(field.Effects);
            _wake = WakeView.Create(field.Effects, new DivePower().WakeGlow, Bubbles);
            _enemy.DamagedBy += OnDamaged;
            _enemy.Deactivated += OnDeactivated;
        }

        /// <summary>Starts the fight afresh for a boss just sent out (pooled bosses come back).</summary>
        public void Begin()
        {
            _boss = _enemy.Definition.Boss;
            if (_signature == null || _signatureKind != _boss.Signature)
            {
                _signature = BossSignatures.For(_boss.Signature, this);
                _signatureKind = _boss.Signature;
            }

            _dives = new PoolDiveRules(_boss.Dive.MaxSeconds);
            _wake.SetGlow(_boss.Dive.WakeGlow);
            _tally.Clear();
            _record.Clear();
            OnStructure = false;
            _signature.Begin();
            if (_life != null)
            {
                StopCoroutine(_life);
            }

            _life = StartCoroutine(Life());
        }

        private void OnDestroy()
        {
            if (_enemy != null)
            {
                _enemy.DamagedBy -= OnDamaged;
                _enemy.Deactivated -= OnDeactivated;
            }
        }

        private void OnDamaged(Enemy enemy, object source, float amount)
        {
            if (source is Tower tower)
            {
                _tally.Add(tower, amount);
            }
        }

        private void OnDeactivated(Enemy enemy)
        {
            if (_life != null)
            {
                StopCoroutine(_life);
                _life = null;
            }

            _ring.Hide();
            _wake.Show(false, transform.position);
            _field.Context.BossRumble = 0f;
            _signature?.End();
            State = "Gone";
        }

        private void Update()
        {
            if (_life != null && Alive)
            {
                _signature.Always(Time.deltaTime);
            }
        }

        private IEnumerator Life()
        {
            if (_boss.Arrival == BossArrival.ColonyRig && _field.HasRig)
            {
                IEnumerator entrance = _signature.Entrance();
                if (entrance != null)
                {
                    yield return entrance;
                }
            }
            else if (_boss.Arrival == BossArrival.Pool)
            {
                bool rose = false;
                yield return RiseFromPool(value => rose = value);
                if (rose)
                {
                    // Out of its pool it may set off on its mechanic at once (a hunt); else it makes for the road.
                    yield return _signature.NextAct() ?? BackToRoad();
                }
            }
            else if (_boss.Arrival == BossArrival.Ruins || _boss.Arrival == BossArrival.Rift)
            {
                // Its own way in: the sealed ruin gate, or the ground at the centre opening and the map coming to it.
                IEnumerator entrance = _signature.Entrance();
                if (entrance != null)
                {
                    yield return entrance;
                }
            }

            while (Alive)
            {
                State = "Road";
                IEnumerator act = _signature.NextAct();
                if (act != null)
                {
                    yield return act;
                    continue;
                }

                IStructure ledge = StructureAhead();
                if (ledge != null)
                {
                    yield return _signature.AtStructure(ledge) ?? ClimbStructure(ledge);
                    continue;
                }

                IStructure rammed = RamTarget();
                if (rammed != null)
                {
                    yield return Ram(rammed);
                    continue;
                }

                if (TryPlanDive(out DivePlan dive))
                {
                    yield return Dive(dive);
                    continue;
                }

                _signature.OnRoad(Time.deltaTime);
                BreakDecorNear(Time.deltaTime);
                yield return null;
            }
        }

        // ---------------------------------------------------------------- shared moments

        internal IEnumerator Roar()
        {
            BossClip roar = BossClip.Of(_view, ModelClipRole.Roar, RoarRate);
            if (!roar.Exists)
            {
                yield break;
            }

            State = "Roar";
            _view.PlayScripted(ModelClipRole.Roar, roar.Rate);
            yield return Wait(roar.Seconds);
            _view.ReleaseScript();
        }

        /// <summary>Plays one of its clips to the end at its mechanic rate, breaking fragile decor while it lasts.</summary>
        internal IEnumerator PlayClip(ModelClipRole role, float rate)
        {
            BossClip clip = BossClip.Of(_view, role, rate);
            if (!clip.Exists)
            {
                yield break;
            }

            _view.PlayScripted(role, clip.Rate, true);
            yield return WaitBreaking(clip.Seconds);
            _view.ReleaseScript();
        }

        internal void Impact(Vector3 at, float radius, Color colour, bool kick = true)
        {
            ProjectilePool projectiles = _field.Context.Projectiles;
            projectiles.PlayBurst(new Vector3(at.x, 0f, at.z), radius, kick ? projectiles.Vfx.Mine : projectiles.Vfx.Death, colour);
        }

        internal IEnumerator Wait(float seconds)
        {
            float waited = 0f;
            while (waited < seconds && Alive)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        internal IEnumerator WaitBreaking(float seconds)
        {
            float waited = 0f;
            while (waited < seconds && Alive)
            {
                waited += Time.deltaTime;
                BreakDecorNear(Time.deltaTime);
                yield return null;
            }
        }

        internal Vector3 OnGround(Vector3 at) => new Vector3(at.x, _field.GroundAt(at), at.z);

        // ---------------------------------------------------------------- walking off the road and climbing

        internal IEnumerator BackToRoad()
        {
            _rocksClosed = false;
            for (int attempt = 0; attempt < MaxReplans && Alive; attempt++)
            {
                _field.Refresh();
                if (!BossRoutes.WalkToRoad(_field, transform.position, _boss.Climb.ClimbsRocks && !_rocksClosed, _route))
                {
                    break;
                }

                bool replan = false;
                yield return WalkRoute(_route, value => replan = value);
                if (!replan)
                {
                    break;
                }
            }

            _view.Tilt = Quaternion.identity;
            JoinRoad();
        }

        /// <summary>
        /// Walks off the road to a cell (a spot beside a tower, a bank), round towers, over rocks, replanning when the way
        /// changes; reports whether it got there. <paramref name="clearance"/> keeps its whole body a cell off every tower.
        /// </summary>
        internal IEnumerator WalkTo(Vector2Int goal, bool clearance, System.Func<bool> abort, System.Action<bool> arrived, string state)
        {
            _rocksClosed = false;
            arrived(false);
            for (int attempt = 0; attempt < MaxReplans && Alive; attempt++)
            {
                if (abort != null && abort())
                {
                    yield break;
                }

                _field.Refresh();
                bool rocks = _boss.Climb.ClimbsRocks && !_rocksClosed;
                if (!PlanWalk(goal, rocks, clearance ? ClearanceCells : 0))
                {
                    yield break;
                }

                bool replan = false;
                yield return WalkRoute(_route, value => replan = value, abort, state);
                if (!replan)
                {
                    _view.Tilt = Quaternion.identity;
                    arrived(_field.Grid.WorldToCell(transform.position) == goal);
                    yield break;
                }
            }

            _view.Tilt = Quaternion.identity;
        }

        // Its body clear of the towers if it can, a cell less if that finds no way, at last any way round the footprints.
        private bool PlanWalk(Vector2Int goal, bool rocks, int clearance)
        {
            for (int cells = clearance; cells >= 0; cells--)
            {
                if (BossRoutes.WalkTo(_field, transform.position, goal, rocks, cells, _route))
                {
                    return true;
                }
            }

            return false;
        }

        // Cells kept between its pivot and a tower so its legs pass clear: half its body plus half a tower, in whole cells.
        private int ClearanceCells => Mathf.Max(1, Mathf.CeilToInt(BodyHalfWidth + 0.5f) - 1);

        internal void JoinRoad()
        {
            Vector3[] road = OffRoadRoute.Build(_field.Level, transform.position, out float progress);
            road[0] = transform.position;
            _enemy.Reroute(road, progress);
            State = "Road";
        }

        // Walks a planned route at its own pace, following the ground's height with a tilt and breaking fragile decor. A
        // rock straight ahead, as far off as the climb clip's ledge, is climbed; then (or when a tower went up on the way,
        // or a rock it cannot climb is next) the caller plans again from where it stands. It never walks through a rock.
        internal IEnumerator WalkRoute(List<Vector3> route, System.Action<bool> replan, System.Func<bool> abort = null, string state = "Walking")
        {
            State = state;
            replan(false);
            BossClip up = BossClip.Of(_view, ModelClipRole.ClimbUp, _boss.Climb.ClipRate);
            float sinceRefresh = 0f;
            for (int i = 1; i < route.Count && Alive; i++)
            {
                Vector3 target = route[i];
                while (Alive)
                {
                    if (abort != null && abort())
                    {
                        yield break;
                    }

                    Vector3 position = transform.position;
                    Vector3 flat = new Vector3(target.x - position.x, 0f, target.z - position.z);
                    float distance = flat.magnitude;
                    if (distance < 0.02f)
                    {
                        break;
                    }

                    Vector3 dir = flat / distance;
                    if (!_rocksClosed && RockAhead(position, dir, up.Anchor))
                    {
                        bool climbed = false;
                        yield return ClimbOver(dir, value => climbed = value);
                        // Climbed: plan on from the landing. No room to land: plan round the rocks instead.
                        _rocksClosed |= !climbed;
                        replan(true);
                        yield break;
                    }

                    Vector2Int targetCell = _field.Grid.WorldToCell(target);
                    if (_field.Climb.KindAt(targetCell) == SurfaceKind.Rock && distance < _field.Grid.CellSize)
                    {
                        _rocksClosed = true;
                        replan(true);
                        yield break;
                    }

                    float step = Mathf.Min(distance, _enemy.GroundSpeed * Time.deltaTime);
                    Vector3 next = position + dir * step;
                    float ground = Mathf.Max(_field.Climb.HeightAt(_field.Grid.WorldToCell(next)), _field.GroundAt(next));
                    next.y = Mathf.MoveTowards(position.y, ground, HeightFollow * Time.deltaTime);
                    _enemy.Place(next, dir);
                    float slope = step > 0.0001f ? Mathf.Clamp((next.y - position.y) / step, -MaxSlopeTilt, MaxSlopeTilt) : 0f;
                    _view.Tilt = Quaternion.AngleAxis(-slope * _boss.Climb.MaxTilt, Vector3.Cross(Vector3.up, dir));
                    BreakDecorNear(Time.deltaTime);
                    yield return null;
                    sinceRefresh += Time.deltaTime;
                    if (sinceRefresh < RefreshSeconds)
                    {
                        continue;
                    }

                    sinceRefresh = 0f;
                    _field.Refresh();
                    if (_field.Climb.IsFootprint(targetCell))
                    {
                        // A tower went up on the way meanwhile.
                        replan(true);
                        yield break;
                    }
                }
            }

            _view.Tilt = Quaternion.identity;
        }

        // A solid rock high enough to climb, its face as far ahead as the climb clip expects, with open ground before it.
        private bool RockAhead(Vector3 position, Vector3 dir, float ledgeAhead)
        {
            if (!_boss.Climb.ClimbsRocks || ledgeAhead <= 0f)
            {
                return false;
            }

            Vector3 at = position + dir * (ledgeAhead + _field.Grid.CellSize * 0.5f);
            Vector2Int cell = _field.Grid.WorldToCell(at);
            return _field.Climb.KindAt(cell) == SurfaceKind.Rock && _field.Climb.HeightAt(cell) >= _boss.Climb.MinLedge
                && BossRoutes.ClearOfFootprints(_field, position, at);
        }

        // Clambers up the rock in front and down the far side (fixed-pivot clips played faster), if it can land clear.
        private IEnumerator ClimbOver(Vector3 dir, System.Action<bool> done)
        {
            BossClip up = BossClip.Of(_view, ModelClipRole.ClimbUp, _boss.Climb.ClipRate);
            BossClip down = BossClip.Of(_view, ModelClipRole.ClimbDown, _boss.Climb.ClipRate);
            Vector3 start = transform.position;
            Vector3 top = start + dir * up.Forward;
            Vector3 land = top + dir * down.Forward;
            land.y = 0f;
            done(false);
            if (!up.Exists || !down.Exists || !BossRoutes.Clear(_field, land, land) || !BossRoutes.ClearOfFootprints(_field, start, land))
            {
                yield break;
            }

            string was = State;
            State = "Climbing";
            _record.Climbs++;
            _view.Tilt = Quaternion.identity;
            float height = Mathf.Max(_field.Climb.HeightAt(_field.Grid.WorldToCell(top)), up.Up);
            _enemy.Place(start, dir);
            _view.PlayScripted(ModelClipRole.ClimbUp, up.Rate, true);
            yield return WaitBreaking(up.Seconds);
            _enemy.Place(new Vector3(top.x, height, top.z), dir);
            _view.PlayScripted(ModelClipRole.ClimbDown, down.Rate, true);
            yield return WaitBreaking(down.Seconds);
            _enemy.Place(land, dir);
            _view.ReleaseScript();
            State = was;
            done(true);
        }

        /// <summary>
        /// A gate (or the base) in front: it climbs onto it, weighs it down and strikes it from above until it falls (or
        /// its mechanic calls it away), then climbs down the far side. Walkers follow it over the gate while it stands there.
        /// A boss with its own gate crossing (Gate_Mount, Gate_Straddle_Loop, Gate_Dismount) plays that on a gate; the base
        /// and a boss without them use the rock climb.
        /// </summary>
        internal IEnumerator ClimbStructure(IStructure structure)
        {
            State = "ClimbingGate";
            _record.Climbs++;
            _enemy.SetDirected(true);
            if (structure is Tower gate && !structure.IsBase && HasGateCrossing)
            {
                yield return CrossGate(gate);
                yield break;
            }

            BossClip up = BossClip.Of(_view, ModelClipRole.ClimbUp, _boss.Climb.ClipRate);
            Vector3 dir = BossGeometry.Flat(structure.Position - transform.position);
            Vector3 start = transform.position;
            _enemy.Place(start, dir);
            _view.PlayScripted(ModelClipRole.ClimbUp, up.Rate, true);
            yield return Wait(up.Seconds);
            float height = structure.IsBase ? ClimbLayer.BaseHeight : ClimbLayer.GateHeight;
            Vector3 top = start + dir * up.Forward;
            _enemy.Place(new Vector3(top.x, height, top.z), dir);
            Straddle();
            OnStructure = true;
            var tower = structure as Tower;
            tower?.SetBurden(1f);
            EnemyDefinition kind = _enemy.Definition;
            float blowEvery = kind.AttackInterval;
            float sinceBlow = 0f;
            float multiplier = _field.Context.Difficulty.StructureDamageMultiplier;
            while (Alive && structure.IsAlive && !_signature.WantsOffStructure)
            {
                float dt = Time.deltaTime;
                structure.TakeDamage(_boss.Climb.GateWeightDamage * multiplier * dt);
                sinceBlow += dt;
                if (sinceBlow >= blowEvery)
                {
                    sinceBlow -= blowEvery;
                    structure.TakeDamage(kind.BlockadeDamage * multiplier);
                    _field.Context.StructureHit.Raise(structure.Position);
                }

                yield return null;
            }

            tower?.SetBurden(0f);
            if (!Alive)
            {
                OnStructure = false;
                yield break;
            }

            yield return ClimbDown(top, dir);
        }

        /// <summary>Climbs down off a structure on its far side (the rock-climb clip) and walks on along the road.</summary>
        internal IEnumerator ClimbDown(Vector3 top, Vector3 dir)
        {
            BossClip down = BossClip.Of(_view, ModelClipRole.ClimbDown, _boss.Climb.ClipRate);
            _view.PlayScripted(ModelClipRole.ClimbDown, down.Rate, true);
            yield return Wait(down.Seconds);
            OnStructure = false;
            Vector3 land = top + dir * down.Forward;
            _enemy.Place(new Vector3(land.x, 0f, land.z), dir);
            _view.ReleaseScript();
            JoinRoad();
        }

        /// <summary>True when the model carries its own gate crossing (ArtSource README round 33): all three clips.</summary>
        internal bool HasGateCrossing =>
            _view.HasClip(ModelClipRole.GateMount) && _view.HasClip(ModelClipRole.GateStraddle) && _view.HasClip(ModelClipRole.GateDismount);

        // Its own crossing (README round 33). The clips are authored round the real gate with the pivot fixed: Mount starts
        // with the gate's centre gate_forward_m ahead and ends with the body over it, so the pivot then moves onto the gate
        // centre ON THE GROUND (end_offset_up_m is 0; the clip itself lifts the body onto the beam). The gate sags by the
        // clip's own per-frame burden instead of a flat 1, and each weight frame is a blow with dust and a kick.
        private IEnumerator CrossGate(Tower gate)
        {
            float rate = _boss.Climb.GateCrossRate;
            BossClip mount = BossClip.Of(_view, ModelClipRole.GateMount, rate);
            Vector3 centre = new Vector3(gate.Position.x, 0f, gate.Position.z);
            Vector3 dir = BossGeometry.Flat(centre - transform.position);
            // It set off at the clip's own reach (StructureAhead), less at most a frame's step: start exactly there.
            _enemy.Place(OnGround(centre - dir * mount.Anchor), dir);
            _view.PlayScripted(ModelClipRole.GateMount, mount.Rate, true);
            float blow = _enemy.Definition.BlockadeDamage * _field.Context.Difficulty.StructureDamageMultiplier;
            yield return PressGate(gate, mount, blow);
            if (Alive)
            {
                _enemy.Place(OnGround(centre), dir);
                OnStructure = true;
                State = "OnGate";
                yield return Straddling(gate);
            }

            if (!Alive)
            {
                gate.SetBurden(0f);
                OnStructure = false;
                yield break;
            }

            State = "ClimbingDown";
            BossClip dismount = BossClip.Of(_view, ModelClipRole.GateDismount, rate);
            _view.PlayScripted(ModelClipRole.GateDismount, dismount.Rate, true);
            yield return PressGate(gate, dismount, blow);
            gate.SetBurden(0f);
            OnStructure = false;
            _enemy.Place(OnGround(centre + dir * dismount.Forward), dir);
            _view.ReleaseScript();
            JoinRoad();
        }

        // Plays out a fixed-pivot crossing clip already started: the gate's sag follows the clip frame by frame, and each
        // weight frame is a blow of the given damage.
        private IEnumerator PressGate(Tower gate, BossClip clip, float blow)
        {
            ModelClip source = _view.ClipOf(clip.Role);
            float[] moments = source != null ? source.MomentSeconds : System.Array.Empty<float>();
            int next = 0;
            float elapsed = 0f;
            while (elapsed < clip.Seconds && Alive)
            {
                elapsed += Time.deltaTime;
                float at = elapsed * clip.Rate;
                gate.SetBurden(gate.IsAlive && source != null ? source.BurdenAt(at) : 0f);
                for (; next < moments.Length && at >= moments[next]; next++)
                {
                    GateBlow(gate, blow);
                }

                yield return null;
            }
        }

        // Draped over the gate in its straddle loop at rate 1, so the blows fall on the authored weight frames: the beam sags
        // with the loop's burden (never quite 0 while it lies there, so the walkers keep their bridge), its weight wears the
        // gate down and each weight frame strikes it. A blow carries the creature's damage per attack interval, so the gate
        // falls as fast as it did under the old timed blows. It leaves when the gate falls, or at the end of a loop once its
        // mechanic calls it away (the dismount starts in the loop's first pose).
        private IEnumerator Straddling(Tower gate)
        {
            _view.LoopScripted(ModelClipRole.GateStraddle, 1f);
            ModelClip loop = _view.ClipOf(ModelClipRole.GateStraddle);
            float length = Mathf.Max(MinClipSeconds, loop.Length);
            float[] moments = loop.MomentSeconds;
            EnemyDefinition kind = _enemy.Definition;
            float multiplier = _field.Context.Difficulty.StructureDamageMultiplier;
            float interval = Mathf.Max(MinClipSeconds, kind.AttackInterval);
            float blow = moments.Length > 0 ? kind.BlockadeDamage * multiplier * (length / moments.Length) / interval : kind.BlockadeDamage * multiplier;
            float sinceBlow = 0f;
            float at = 0f;
            bool leaving = false;
            while (Alive && gate.IsAlive)
            {
                float dt = Time.deltaTime;
                float was = at;
                at += dt;
                bool wrapped = at >= length;
                if (wrapped)
                {
                    at -= length;
                }

                gate.SetBurden(Mathf.Max(StandingBurden, loop.BurdenAt(at)));
                gate.TakeDamage(_boss.Climb.GateWeightDamage * multiplier * dt);
                for (int i = 0; i < moments.Length; i++)
                {
                    bool crossed = wrapped ? moments[i] > was || moments[i] <= at : moments[i] > was && moments[i] <= at;
                    if (crossed)
                    {
                        GateBlow(gate, blow);
                    }
                }

                if (moments.Length == 0)
                {
                    // A loop without weight frames keeps the old timed blows.
                    sinceBlow += dt;
                    if (sinceBlow >= interval)
                    {
                        sinceBlow -= interval;
                        GateBlow(gate, blow);
                    }
                }

                leaving |= _signature.WantsOffStructure;
                if (leaving && wrapped)
                {
                    yield break;
                }

                yield return null;
            }
        }

        // One heavy push on a gate: the hit, dust at the beam and a short kick.
        private void GateBlow(IStructure gate, float damage)
        {
            if (!gate.IsAlive)
            {
                return;
            }

            gate.TakeDamage(damage);
            _field.Context.StructureHit.Raise(gate.Position);
            Impact(gate.Position, GateDustRadius, GateDust, false);
            _field.Context.BossImpact.Raise(_boss.ImpactKick * GateBlowKick);
        }

        // On a gate without its own crossing it clings in place (Remco: the crawl cycle looked like walking on flat ground):
        // the climb loop held still on one pose.
        internal void Straddle() => _view.LoopScripted(ModelClipRole.ClimbLoop, 0f);

        // A living gate or the base straight ahead, as far ahead as its climb clip reaches (its own mount for a gate).
        private IStructure StructureAhead()
        {
            if (!_boss.Climb.ClimbsGates || _enemy.IsDirected)
            {
                return null;
            }

            BossClip up = HasGateCrossing
                ? BossClip.Of(_view, ModelClipRole.GateMount, _boss.Climb.GateCrossRate)
                : BossClip.Of(_view, ModelClipRole.ClimbUp, _boss.Climb.ClipRate);
            return up.Exists ? Ahead(up.Anchor) : null;
        }

        // The nearest living gate or the base straight ahead within the given reach, or null. Only real structures count: the
        // hero holding the road at a gate post also blocks walkers, but a boss never climbs onto (or rams) the hero.
        private IStructure Ahead(float reach)
        {
            StructureRegistry structures = _field.Context.Structures;
            IStructure best = null;
            float bestSqr = reach * reach;
            for (int i = 0; i < structures.Count; i++)
            {
                IStructure candidate = structures[i];
                if (!candidate.BlocksGround || !(candidate is Tower || candidate.IsBase))
                {
                    continue;
                }

                Vector3 to = candidate.Position - transform.position;
                to.y = 0f;
                float sqr = to.sqrMagnitude;
                if (sqr <= bestSqr && Vector3.Dot(BossGeometry.Flat(to), _enemy.Heading) > 0.5f)
                {
                    best = candidate;
                    bestSqr = sqr;
                }
            }

            return best;
        }

        // Batter (a body that cannot climb, Rotbloom): a living gate ahead within its body plus the ram clearance.
        private IStructure RamTarget()
        {
            ClimbPower climb = _boss.Climb;
            if (!climb.BattersGates || _enemy.IsDirected || !_view.HasClip(climb.RamClip))
            {
                return null;
            }

            IStructure structure = Ahead(_boss.BodyHalfWidth + climb.RamClearance);
            return structure != null && !structure.IsBase ? structure : null;
        }

        // One ram at the gate in front with its own clip; the blow lands on the clip's moment. Between two
        // rams it is free for one step, so the brain can still run its mechanic (a spore volley); it stops again at once, its
        // reach being longer than the distance a creature is held at. It walks on once the gate is down. A ram
        // carries the creature's damage per attack interval over the clip's length, so a gate lasts as long as under bites.
        private IEnumerator Ram(IStructure gate)
        {
            ClimbPower climb = _boss.Climb;
            BossClip ram = BossClip.Of(_view, climb.RamClip, climb.RamRate);
            State = Battering;
            _enemy.SetDirected(true);
            Vector3 dir = BossGeometry.Flat(gate.Position - transform.position);
            _enemy.Place(transform.position, dir);
            _view.PlayScripted(climb.RamClip, ram.Rate, true);
            yield return Wait(ram.Moment);
            EnemyDefinition kind = _enemy.Definition;
            if (Alive && gate.IsAlive)
            {
                float blows = Mathf.Max(1f, ram.Seconds / Mathf.Max(MinClipSeconds, kind.AttackInterval));
                GateBlow(gate, kind.BlockadeDamage * _field.Context.Difficulty.StructureDamageMultiplier * blows);
                _record.Rams++;
            }

            yield return Wait(ram.Seconds - ram.Moment);
            _view.ReleaseScript();
            _enemy.SetDirected(false);
            if (Alive && !gate.IsAlive)
            {
                JoinRoad();
            }
        }

        internal void BreakDecorNear(float dt)
        {
            _sinceBreak += dt;
            if (_sinceBreak < BreakEverySeconds)
            {
                return;
            }

            _sinceBreak = 0f;
            IReadOnlyList<Vector3> broken = _field.BreakDecor(transform.position, BodyRadiusPerScale * Scale);
            for (int i = 0; i < broken.Count; i++)
            {
                _field.Context.Projectiles.PlayBurst(broken[i], DebrisBurstRadius, _field.Context.Projectiles.Vfx.Death, CrystalShards);
            }
        }

        // ---------------------------------------------------------------- pools

        internal readonly struct DivePlan
        {
            public DivePlan(int pool, TerrainKind kind, Vector3 heading, Vector3 under, Vector3 exit, Vector3 exitHeading, float speedUp = 1f)
            {
                Pool = pool;
                Kind = kind;
                Heading = heading;
                Under = under;
                Exit = exit;
                ExitHeading = exitHeading;
                SpeedUp = speedUp;
            }

            public int Pool { get; }
            public TerrainKind Kind { get; }
            public Vector3 Heading { get; }
            public Vector3 Under { get; }
            public Vector3 Exit { get; }
            public Vector3 ExitHeading { get; }

            /// <summary>How much faster than its own swim it may go (a trip from one pool to another).</summary>
            public float SpeedUp { get; }
        }

        private bool TryPlanDive(out DivePlan plan)
        {
            plan = default;
            DivePower dive = _boss.Dive;
            _sinceDiveCheck += Time.deltaTime;
            if (!dive.Dives || _enemy.IsDirected || _sinceDiveCheck < DiveCheckSeconds)
            {
                return false;
            }

            _sinceDiveCheck = 0f;
            BossClip into = BossClip.Of(_view, ModelClipRole.DiveIn, _boss.MechanicClipRate);
            BossClip burst = BossClip.Of(_view, ModelClipRole.SurfaceBurst, _boss.MechanicClipRate);
            if (!into.Exists || !burst.Exists)
            {
                return false;
            }

            int phase = _boss.PhaseAt(_enemy.Health.Fraction);
            Vector3 position = transform.position;
            foreach (TerrainKind kind in PoolKindsOf(dive.Pools))
            {
                int count = _field.Pools.Count(kind);
                for (int p = 0; p < count; p++)
                {
                    int id = PoolId(kind, p);
                    if (!_dives.CanUse(id, phase))
                    {
                        continue;
                    }

                    IReadOnlyList<Vector2Int> cells = _field.Pools.Pool(kind, p);
                    Vector3 nearest = BossGeometry.Nearest(_field.Grid, cells, position, out float distance);
                    if (distance > dive.Reach + into.Forward * 0.5f || Vector3.Dot(BossGeometry.Flat(nearest - position), _enemy.Heading) < -0.2f)
                    {
                        continue;
                    }

                    _field.Refresh();
                    if (!TryExit(cells, nearest, burst, out Vector3 exit, out Vector3 exitHeading))
                    {
                        continue;
                    }

                    Vector3 heading = BossGeometry.Flat(nearest - position);
                    Vector3 under = BossGeometry.Nearest(_field.Grid, cells, position + heading * into.Forward, out _);
                    _dives.MarkUsed(id, phase);
                    plan = new DivePlan(id, kind, heading, under, exit, exitHeading);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A dive as a shortcut (Docs/BOSSES.md: the Cinder Matriarch "comes up in another lava pool, closer to her
        /// target"): into a pool within reach and out of another pool's bank at least <paramref name="gain"/> nearer the
        /// goal, within the rules (each pool once per phase, at most its limit under).
        /// </summary>
        internal bool TryPlanDiveTowards(Vector3 goal, float gain, out DivePlan plan)
        {
            plan = default;
            DivePower dive = _boss.Dive;
            BossClip into = BossClip.Of(_view, ModelClipRole.DiveIn, _boss.MechanicClipRate);
            BossClip burst = BossClip.Of(_view, ModelClipRole.SurfaceBurst, _boss.MechanicClipRate);
            if (!dive.Dives || !into.Exists || !burst.Exists)
            {
                return false;
            }

            _field.Refresh();
            int phase = _boss.PhaseAt(_enemy.Health.Fraction);
            Vector3 position = transform.position;
            float now = BossGeometry.FlatDistance(position, goal);
            float reachUnder = dive.SwimSpeed * ShortcutSpeedUp * _dives.MaxSeconds;
            float best = now - gain;
            bool found = false;
            foreach (TerrainKind kind in PoolKindsOf(dive.Pools))
            {
                for (int p = 0; p < _field.Pools.Count(kind); p++)
                {
                    int entry = PoolId(kind, p);
                    IReadOnlyList<Vector2Int> cells = _field.Pools.Pool(kind, p);
                    Vector3 bank = BossGeometry.Nearest(_field.Grid, cells, position, out float distance);
                    if (!_dives.CanUse(entry, phase) || distance > dive.Reach + into.Forward * 0.5f)
                    {
                        continue;
                    }

                    Vector3 heading = BossGeometry.Flat(bank - position);
                    Vector3 under = BossGeometry.Nearest(_field.Grid, cells, position + heading * into.Forward, out _);
                    for (int q = 0; q < _field.Pools.Count(kind); q++)
                    {
                        int exitPool = PoolId(kind, q);
                        if (q == p || !_dives.CanUse(exitPool, phase)
                            || !TryExitTowards(_field.Pools.Pool(kind, q), under, goal, reachUnder, burst, out Vector3 exit, out Vector3 exitHeading, out float left)
                            || left >= best)
                        {
                            continue;
                        }

                        best = left;
                        found = true;
                        plan = new DivePlan(entry, kind, heading, under, exit, exitHeading, ShortcutSpeedUp);
                    }
                }
            }

            if (found)
            {
                _dives.MarkUsed(plan.Pool, phase);
            }

            return found;
        }

        // Where it comes out of a pool: the pool cell, as far as can be from where it went in, whose bank landing lies clear
        // and nearest the road (BOSSES.md: "at the bank nearest the road").
        private bool TryExit(IReadOnlyList<Vector2Int> cells, Vector3 entry, BossClip burst, out Vector3 exit, out Vector3 heading)
        {
            exit = heading = Vector3.zero;
            float best = float.NegativeInfinity;
            GridMap grid = _field.Grid;
            foreach (Vector2Int cell in cells)
            {
                Vector3 at = grid.CellToWorld(cell);
                for (int t = 0; t < 8; t++)
                {
                    Vector3 dir = Quaternion.Euler(0f, t * 45f, 0f) * Vector3.forward;
                    Vector3 land = at + dir * burst.Forward;
                    if (!WellInside(at) || !WellInside(land) || !BossRoutes.Clear(_field, land, land) || !BossRoutes.ClearOfFootprints(_field, at, land))
                    {
                        continue;
                    }

                    Vector3 road = OffRoadRoute.NearestRoadCellWorld(_field.Level, land);
                    // Its whole body comes up clear of the towers on the bank where it can.
                    int overlaps = BossRoutes.BodyOverlaps(_field, at, land, BodyHalfWidth, NoCell);
                    float score = (at - entry).magnitude - (road - land).magnitude * RoadPull - overlaps * BodyOverlapCost;
                    if (score > best)
                    {
                        best = score;
                        exit = at;
                        heading = dir;
                    }
                }
            }

            return !float.IsNegativeInfinity(best);
        }

        // The cell of another pool, within reach under, whose clear bank landing lies nearest the goal.
        private bool TryExitTowards(IReadOnlyList<Vector2Int> cells, Vector3 from, Vector3 goal, float reach, BossClip burst, out Vector3 exit, out Vector3 heading, out float left)
        {
            exit = heading = Vector3.zero;
            left = float.PositiveInfinity;
            GridMap grid = _field.Grid;
            foreach (Vector2Int cell in cells)
            {
                Vector3 at = grid.CellToWorld(cell);
                if (BossGeometry.FlatDistance(at, from) > reach)
                {
                    continue;
                }

                for (int t = 0; t < 8; t++)
                {
                    Vector3 dir = Quaternion.Euler(0f, t * 45f, 0f) * Vector3.forward;
                    Vector3 land = at + dir * burst.Forward;
                    float distance = BossGeometry.FlatDistance(land, goal);
                    if (distance >= left || !WellInside(at) || !WellInside(land) || !BossRoutes.Clear(_field, land, land) || !BossRoutes.ClearOfFootprints(_field, at, land)
                        || BossRoutes.BodyOverlaps(_field, at, land, BodyHalfWidth, NoCell) > 0)
                    {
                        continue;
                    }

                    left = distance;
                    exit = at;
                    heading = dir;
                }
            }

            return !float.IsPositiveInfinity(left);
        }

        // Into the pool from the bank, hidden under the surface (only blasts reach it, and it heals a little) for no more
        // than its limit, and out at the far bank with a burst that splashes the structures there.
        internal IEnumerator Dive(DivePlan plan, bool backToRoad = true)
        {
            State = "Diving";
            _record.Dives++;
            DivePower dive = _boss.Dive;
            _enemy.SetDirected(true);
            BossClip into = BossClip.Of(_view, ModelClipRole.DiveIn, _boss.MechanicClipRate);
            float surface = TerrainLiquidBuilder.SurfaceLevel(plan.Kind);
            _enemy.Place(transform.position, plan.Heading);
            _view.PlayScripted(ModelClipRole.DiveIn, into.Rate, true);
            yield return Wait(into.Moment);
            Impact(transform.position + plan.Heading * into.Forward * 0.5f, BreakoutBurstRadius, SplashOf(plan.Kind), false);
            _enemy.SetConcealed(true, dive.SplashShare);
            yield return Wait(into.Seconds - into.Moment);
            _view.SetHidden(true);
            State = "Submerged";
            Vector3 from = new Vector3(plan.Under.x, surface, plan.Under.z);
            Vector3 to = new Vector3(plan.Exit.x, surface, plan.Exit.z);
            float distance = Vector3.Distance(from, to);
            float seconds = _dives.SecondsFor(distance, dive.SwimSpeed * plan.SpeedUp);
            _wake.SetKind(plan.Kind, WakeSize);
            yield return Swim(from, to, seconds);
            if (!Alive)
            {
                yield break;
            }

            yield return BurstOut(to, plan.ExitHeading, plan.Kind);
            if (backToRoad)
            {
                yield return BackToRoad();
            }
        }

        // Hidden under the surface from one point to another, the wake following it wherever there is liquid above it.
        private IEnumerator Swim(Vector3 from, Vector3 to, float seconds)
        {
            DivePower dive = _boss.Dive;
            _wake.Show(true, from);
            float elapsed = 0f;
            while (elapsed < seconds && Alive)
            {
                float dt = Time.deltaTime;
                elapsed = Mathf.Min(seconds, elapsed + dt);
                Vector3 at = Vector3.Lerp(from, to, seconds > 0f ? elapsed / seconds : 1f);
                _enemy.Place(at, BossGeometry.Flat(to - from));
                _wake.Face(to - from);
                _wake.Show(_field.Climb.KindAt(at) == SurfaceKind.Liquid || _field.Climb.KindAt(at) == SurfaceKind.Quicksand, at);
                _enemy.Health.Heal(_enemy.Health.Max * dive.RegenPerSecond * dt);
                yield return null;
            }

            _wake.Show(false, to);
        }

        // Up out of the pool onto the bank: the burst splashes the structures there, one kick when it lands.
        private IEnumerator BurstOut(Vector3 to, Vector3 heading, TerrainKind kind)
        {
            DivePower dive = _boss.Dive;
            BossClip burst = BossClip.Of(_view, ModelClipRole.SurfaceBurst, _boss.MechanicClipRate);
            State = "Bursting";
            _enemy.Place(to, heading);
            _view.SetHidden(false);
            _view.PlayScripted(ModelClipRole.SurfaceBurst, burst.Rate, true);
            yield return Wait(burst.Moment);
            Vector3 land = to + heading * burst.Forward;
            _enemy.SetConcealed(false);
            Impact(to + heading * (burst.Forward * 0.5f), BreakoutBurstRadius, SplashOf(kind));
            _field.Context.Structures.DamageInRadius(land, dive.BurstRadius, dive.BurstDamage * _field.Context.Difficulty.StructureDamageMultiplier);
            yield return Wait(burst.Seconds - burst.Moment);
            _enemy.Place(new Vector3(land.x, 0f, land.z), heading);
            _view.ReleaseScript();
        }

        // The pool entrance (Docs/BOSSES.md: the Cinder Matriarch out of the largest lava pool, the Frost Warden out of a
        // frozen lake that cracks open): hidden in its middle, the surface churns and the ground rumbles, then it swims to
        // the bank nearest the road and bursts out with a kick and a roar. False when the level has no such pool.
        private IEnumerator RiseFromPool(System.Action<bool> rose)
        {
            rose(false);
            BossClip burst = BossClip.Of(_view, ModelClipRole.SurfaceBurst, _boss.MechanicClipRate);
            if (!burst.Exists || !TryEntrancePool(_field, _boss.Dive.Pools, out TerrainKind kind, out IReadOnlyList<Vector2Int> cells, out int pool))
            {
                yield break;
            }

            State = "Entrance";
            _enemy.SetDirected(true);
            _enemy.SetConcealed(true, 0f);
            _view.SetHidden(true);
            // It has just come out of this pool: it does not dive straight back into it.
            _dives.MarkUsed(pool, 0);
            _field.Refresh();
            Vector3 middle = Middle(_field, cells);
            float surface = TerrainLiquidBuilder.SurfaceLevel(kind);
            Vector3 from = new Vector3(middle.x, surface, middle.z);
            if (!TryExit(cells, middle, burst, out Vector3 exit, out Vector3 heading))
            {
                exit = middle;
                heading = BossGeometry.Flat(OffRoadRoute.NearestRoadCellWorld(_field.Level, middle) - middle);
            }

            _wake.SetKind(kind, WakeSize);
            _enemy.Place(from, heading);
            _wake.Show(true, from);
            float stirred = 0f;
            while (stirred < StirSeconds && Alive)
            {
                stirred += Time.deltaTime;
                _field.Context.BossRumble = Mathf.Clamp01(stirred / StirSeconds);
                yield return null;
            }

            Impact(from, BreakoutBurstRadius, SplashOf(kind), false);
            Vector3 to = new Vector3(exit.x, surface, exit.z);
            yield return Swim(from, to, _dives.SecondsFor(Vector3.Distance(from, to), _boss.Dive.SwimSpeed));
            _field.Context.BossRumble = 0f;
            yield return BurstOut(to, heading, kind);
            _field.Context.BossImpact.Raise(_boss.ImpactKick);
            yield return Roar();
            rose(Alive);
        }

        // The largest pool whose way to the road joins it in the road's first half, so the fight runs the road (a lake
        // by the base would skip it); without one, the pool that joins the road earliest.
        internal static bool TryEntrancePool(BossField field, PoolKinds kinds, out TerrainKind kind, out IReadOnlyList<Vector2Int> cells, out int id)
        {
            kind = TerrainKind.Ground;
            cells = null;
            id = -1;
            float earliest = float.PositiveInfinity;
            bool early = false;
            foreach (TerrainKind candidate in PoolKindsOf(kinds))
            {
                for (int p = 0; p < field.Pools.Count(candidate); p++)
                {
                    IReadOnlyList<Vector2Int> pool = field.Pools.Pool(candidate, p);
                    float share = RoadShareAt(field, Middle(field, pool));
                    bool isEarly = share <= MaxEntranceRoadShare;
                    bool better = isEarly ? !early || pool.Count > cells.Count : !early && share < earliest;
                    if (better)
                    {
                        cells = pool;
                        kind = candidate;
                        id = PoolId(candidate, p);
                        early |= isEarly;
                        earliest = Mathf.Min(earliest, share);
                    }
                }
            }

            return cells != null;
        }

        // How far along the road (0 at its start, 1 at the base) a walker from that point joins it.
        private static float RoadShareAt(BossField field, Vector3 point)
        {
            Vector3[] route = OffRoadRoute.Build(field.Level, point, out float progress);
            float rest = 0f;
            for (int i = 2; i < route.Length; i++)
            {
                rest += Vector3.Distance(route[i - 1], route[i]);
            }

            float total = progress + rest;
            return total > 0f ? progress / total : 0f;
        }

        // The pool cell nearest the middle of the pool.
        internal static Vector3 Middle(BossField field, IReadOnlyList<Vector2Int> cells)
        {
            Vector3 sum = Vector3.zero;
            foreach (Vector2Int cell in cells)
            {
                sum += field.Grid.CellToWorld(cell);
            }

            return BossGeometry.Nearest(field.Grid, cells, sum / Mathf.Max(1, cells.Count), out _);
        }

        // A landing in the field's outer cells would come up behind the border cliffs, out of sight.
        private bool WellInside(Vector3 point)
        {
            Vector2Int cell = _field.Grid.WorldToCell(point);
            Vector2Int size = _field.Grid.Size;
            return cell.x >= EdgeCells && cell.y >= EdgeCells && cell.x < size.x - EdgeCells && cell.y < size.y - EdgeCells;
        }

        internal static Color SplashOf(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Quicksand: return SandSplash;
                case TerrainKind.Lava: return LavaSplash;
                case TerrainKind.Ice: return IceSplash;
                default: return WaterSplash;
            }
        }

        internal static IEnumerable<TerrainKind> PoolKindsOf(PoolKinds kinds)
        {
            if ((kinds & PoolKinds.Quicksand) != 0) yield return TerrainKind.Quicksand;
            if ((kinds & PoolKinds.Water) != 0) yield return TerrainKind.Water;
            if ((kinds & PoolKinds.Bog) != 0) yield return TerrainKind.Bog;
            if ((kinds & PoolKinds.Lava) != 0) yield return TerrainKind.Lava;
            if ((kinds & PoolKinds.Ice) != 0) yield return TerrainKind.Ice;
        }

        private static int PoolId(TerrainKind kind, int index) => (int)kind * 1000 + index;
    }
}
