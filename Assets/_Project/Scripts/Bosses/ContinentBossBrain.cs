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
    /// Runs a continent boss from its <see cref="BossDefinition"/>: its entrance (a crack to the colony rig that it smashes),
    /// its signature (Burrow: at each threshold it dives under, runs a crack line round towers and gates to the tower that
    /// hurt it most and throws that tower), and the shared framework every boss uses: walking back to the road round
    /// towers, climbing rocks, gates and the base (a gate sags and takes its weight), breaking fragile decoration, and
    /// diving into pools within the rules. While it acts the creature is directed and its own walk waits; between acts it
    /// walks the road like any creature. Logic only: it moves the creature and tells the views what to show.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class ContinentBossBrain : MonoBehaviour
    {
        private const float BreakEverySeconds = 0.2f;
        private const float BodyRadiusPerScale = 0.9f;
        private const float DiveCheckSeconds = 0.4f;
        private const float EntranceCrackCells = 7f;
        private const float RevealLead = 2.5f;
        private const float MinTravelMargin = 0.5f;
        private const float RoarRate = 1.2f;
        private const float StunWobbleDegrees = 9f;
        private const float StunWobbleSpeed = 7f;
        private const float StunIdleRate = 0.4f;
        private const float GateLoopRate = 0.35f;
        private const float HeightFollow = 6f;
        private const float MaxSlopeTilt = 0.6f;
        private const float RoadSurfaceAhead = 7f;
        private const float DestinationSearchCells = 4f;
        private const float ThrowBurstRadius = 1.4f;
        private const float BreakoutBurstRadius = 1.8f;
        private const float DebrisBurstRadius = 0.6f;
        private const float RumbleRise = 1.5f;
        private const int MaxReplans = 4;

        // Where it comes out of a pool: the bank nearest the road weighs this much more than the far side of the pool.
        private const float RoadPull = 4f;
        private const float RefreshSeconds = 0.5f;

        // README round 28: what the throw flings stands about this far (model units) to the right of the hole.
        private const float ThrowSideCells = 1f;
        private static readonly Color CrystalShards = new Color(0.7f, 0.85f, 1f);
        private static readonly Color Earth = new Color(1f, 0.55f, 0.25f);
        private static readonly Color SandSplash = new Color(0.85f, 0.72f, 0.45f);
        private static readonly Color WaterSplash = new Color(0.55f, 0.85f, 1f);
        private static readonly Color UnderGlow = new Color(1f, 0.35f, 0.6f, 0.8f);
        private static readonly Color Bubbles = new Color(0.85f, 0.9f, 1f, 0.7f);
        private static readonly float[] Turns = { 0f, 25f, -25f, 50f, -50f, 80f, -80f, 120f, -120f, 180f };

        private readonly List<Vector3> _route = new List<Vector3>(64);
        private readonly List<Vector3> _crack = new List<Vector3>(64);
        private readonly DamageTally<Tower> _tally = new DamageTally<Tower>();
        private readonly List<Tower> _thrown = new List<Tower>();
        private BossField _field;
        private Enemy _enemy;
        private EnemyView _view;
        private BossDefinition _boss;
        private ThresholdTracker _burrows;
        private PoolDiveRules _dives;
        private TargetRingView _ring;
        private WakeView _wake;
        private float _sinceBreak;
        private float _sinceDiveCheck;
        private bool _struck;
        private bool _enraged;
        private bool _rocksClosed;
        private Coroutine _life;

        /// <summary>What it is doing right now, for probes and tests.</summary>
        public string State { get; private set; } = "Idle";

        /// <summary>How many times it has gone under at a threshold.</summary>
        public int Burrows { get; private set; }

        /// <summary>How many times it hid in a pool.</summary>
        public int Dives { get; private set; }

        /// <summary>How many rocks, gates or base roofs it climbed.</summary>
        public int Climbs { get; private set; }

        /// <summary>Every tower it has thrown, in order.</summary>
        public IReadOnlyList<Tower> Thrown => _thrown;

        /// <summary>True while it stands on a gate or the base roof (its footprint overlaps the structure by design).</summary>
        public bool OnStructure { get; private set; }

        /// <summary>True while it is under the ground or a liquid.</summary>
        public bool Hidden => _enemy != null && _enemy.IsConcealed;

        public BossDefinition Boss => _boss;

        public void Initialize(BossField field)
        {
            _field = field;
            _enemy = GetComponent<Enemy>();
            _view = GetComponent<EnemyView>();
            _ring = TargetRingView.Create(field.Effects);
            _wake = WakeView.Create(field.Effects, UnderGlow, Bubbles);
            _enemy.DamagedBy += OnDamaged;
            _enemy.StruckWhileHidden += OnStruckWhileHidden;
            _enemy.Deactivated += OnDeactivated;
        }

        /// <summary>Starts the fight afresh for a boss just sent out (pooled bosses come back).</summary>
        public void Begin()
        {
            _boss = _enemy.Definition.Boss;
            _burrows = new ThresholdTracker(_boss.Signature == BossSignature.Burrow ? _boss.Burrow.Thresholds : new float[0]);
            _dives = new PoolDiveRules(_boss.Dive.MaxSeconds);
            _tally.Clear();
            _thrown.Clear();
            Burrows = Dives = Climbs = 0;
            _struck = false;
            _enraged = false;
            OnStructure = false;
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
                _enemy.StruckWhileHidden -= OnStruckWhileHidden;
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

        private void OnStruckWhileHidden(Enemy enemy, float amount) => _struck = true;

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
            State = "Gone";
        }

        private bool Alive => _enemy != null && _enemy.IsAlive;

        private float Scale => _view != null ? _view.ModelScale : 1f;

        private IEnumerator Life()
        {
            if (_boss.Arrival == BossArrival.ColonyRig && _field.HasRig)
            {
                yield return Entrance();
            }

            while (Alive)
            {
                State = "Road";
                if (_burrows.TryCross(_enemy.Health.Fraction))
                {
                    yield return Burrow(true);
                    continue;
                }

                IStructure ledge = StructureAhead();
                if (ledge != null)
                {
                    yield return ClimbStructure(ledge);
                    continue;
                }

                if (TryPlanDive(out DivePlan dive))
                {
                    yield return Dive(dive);
                    continue;
                }

                CheckRage();
                BreakDecorNear(Time.deltaTime);
                yield return null;
            }
        }

        // ---------------------------------------------------------------- entrance

        // The ground cracks open along the field's edge towards the colony rig; the boss breaks out under it, flings the
        // derrick over and roars, then makes for the road.
        private IEnumerator Entrance()
        {
            State = "Entrance";
            _enemy.SetDirected(true);
            _enemy.SetConcealed(true, 0f);
            _field.Refresh();
            Vector3 rig = _field.RigPosition;
            Vector3 towards = Flat(OffRoadRoute.NearestRoadCellWorld(_field.Level, rig) - rig);
            BossClip throwClip = BossClip.Of(_view, ModelClipRole.SurfaceThrow, _boss.MechanicClipRate);
            Vector3 inward = towards;
            Vector3 hole = rig;
            Vector3 pivot = rig;
            // It comes up facing the field, the rig on its right where the throw lands; it turns until its body has room.
            foreach (float turn in Turns)
            {
                inward = Quaternion.Euler(0f, turn, 0f) * towards;
                hole = rig - Vector3.Cross(Vector3.up, inward) * (ThrowSideCells * Scale);
                pivot = hole - inward * throwClip.Anchor;
                if (BossRoutes.ClearOfFootprints(_field, pivot, pivot + inward * throwClip.Forward))
                {
                    break;
                }
            }

            Vector3 origin = EntranceOrigin(rig);
            _crack.Clear();
            _crack.Add(origin);
            _crack.Add(hole);
            _route.Clear();
            _route.Add(origin);
            _route.Add(pivot);
            _enemy.Place(origin, Flat(pivot - origin));
            yield return TravelUnder(_crack, _route, null, false);
            yield return Breakout(pivot, inward, null, true);
            yield return Burrow(false);
        }

        // Where the entrance crack begins: along the field's edge from the rig, on the side that stays outside the field.
        private Vector3 EntranceOrigin(Vector3 rig)
        {
            Vector3 outward = rig;
            outward.y = 0f;
            Vector3 along = Vector3.Cross(Vector3.up, outward.normalized) * (EntranceCrackCells * _field.Grid.CellSize);
            Vector2 half = _field.Level.WorldSize * 0.5f;
            Vector3 first = rig + along;
            return ColonyRigPlacement.OutsideField(first, half, 0f) ? first : rig - along;
        }

        // ---------------------------------------------------------------- burrow (Gravemaw's signature)

        // The signature dive at a threshold goes for the tower that hurt it most; the dive after the entrance (not a
        // signature one) only carries it from the rig to the start of the road, so the fight runs the whole road.
        private IEnumerator Burrow(bool signature)
        {
            State = "Burrow";
            Burrows += signature ? 1 : 0;
            _enemy.SetDirected(true);
            _field.Refresh();
            Tower target = signature ? _tally.Top(StillStanding) : null;
            Vector3? destination = signature ? (Vector3?)null : RoadPoint(_boss.Burrow.ReturnToRoadShare);
            BossClip down = BossClip.Of(_view, ModelClipRole.BurrowDown, _boss.MechanicClipRate);
            BossClip up = BossClip.Of(_view, ModelClipRole.SurfaceThrow, _boss.MechanicClipRate);
            if (!PlanBurrow(target, destination, down, up, out Vector3 heading, out Vector3 pivot, out Vector3 forward, out bool throws))
            {
                // Nowhere to go under without crossing a structure: it roars and keeps walking.
                yield return Roar();
                yield return BackToRoad();
                yield break;
            }

            _enemy.Place(transform.position, heading);
            _view.PlayScripted(ModelClipRole.BurrowDown, down.Rate, true);
            yield return Wait(down.Moment);
            Vector3 hole0 = transform.position + heading * down.Anchor;
            Impact(hole0, BreakoutBurstRadius, Earth);
            _enemy.SetConcealed(true, 0f);
            yield return Wait(down.Seconds - down.Moment);
            _enemy.Place(transform.position + heading * down.Forward, heading);
            _crack.Insert(0, hole0);
            _route.Clear();
            _route.Add(transform.position);
            for (int i = 2; i < _crack.Count - 1; i++)
            {
                _route.Add(_crack[i]);
            }

            _route.Add(pivot);
            yield return TravelUnder(_crack, _route, throws ? target : null, signature, signature ? 1f : _boss.Burrow.ReturnSpeedFactor);
            yield return Breakout(transform.position, _struck ? _enemy.Heading : forward, throws && !_struck ? target : null, false);
        }

        // A point on the main road, a share of the way from its start.
        private Vector3 RoadPoint(float share)
        {
            Vector2Int[] road = _field.Level.MainRoad;
            int index = Mathf.Clamp(Mathf.RoundToInt(share * (road.Length - 1)), 0, road.Length - 1);
            return _field.Level.Road.CentreOf(road[index]);
        }

        private bool StillStanding(Tower tower) => tower != null && tower.IsAlive && !tower.IsUpset && _field.Context.Structures.Contains(tower);

        // Which way it dives, where it travels to and where it comes up: beside the target when it can, else down the road.
        private bool PlanBurrow(Tower target, Vector3? destination, BossClip down, BossClip up, out Vector3 heading, out Vector3 pivot, out Vector3 forward, out bool throws)
        {
            heading = pivot = forward = Vector3.zero;
            throws = false;
            Vector3 position = transform.position;
            Vector3 aim = target != null ? Flat(target.Position - position) : destination.HasValue ? Flat(destination.Value - position) : _enemy.Heading;
            // Of the ways it can dive without its body crossing a structure, it takes the one whose crack line runs the
            // shortest to where it comes up: a short, direct line reads as "it is coming for that tower".
            float best = float.PositiveInfinity;
            var bestCrack = new List<Vector3>();
            foreach (float turn in Turns)
            {
                Vector3 dir = Quaternion.Euler(0f, turn, 0f) * aim;
                Vector3 start = position + dir * down.Forward;
                bool inside = _field.Grid.Contains(_field.Grid.WorldToCell(start)) || destination.HasValue;
                if (!inside || !BossRoutes.ClearOfFootprints(_field, position, start)
                    || !PlanCrack(target, destination, start, up, out Vector3 end, out Vector3 endForward, out bool hit))
                {
                    continue;
                }

                float length = PathLength(_crack) + down.Forward;
                if (length < best)
                {
                    best = length;
                    heading = dir;
                    pivot = end;
                    forward = endForward;
                    throws = hit;
                    bestCrack.Clear();
                    bestCrack.AddRange(_crack);
                }
            }

            _crack.Clear();
            _crack.AddRange(bestCrack);
            return !float.IsPositiveInfinity(best);
        }

        // The crack from where it travels under the ground to where it comes up: beside the target when there is room,
        // else on the road (ahead of it, or by its destination).
        private bool PlanCrack(Tower target, Vector3? destination, Vector3 start, BossClip up, out Vector3 pivot, out Vector3 forward, out bool throws)
        {
            Vector3 position = transform.position;
            pivot = forward = Vector3.zero;
            throws = false;
            float holeAhead = up.Anchor;
            if (target != null && BossRoutes.SurfaceBeside(_field, target, start, holeAhead, up.Forward, out Vector3 hole, out pivot, out forward)
                && BossRoutes.Crack(_field, start, hole, _boss.Burrow.QuicksandFactor, _crack))
            {
                throws = true;
                return true;
            }

            Vector3 from = destination ?? start;
            Vector3 along = _field.Level.Road.DirectionAt(_field.Grid.WorldToCell(destination ?? position));
            along = along.sqrMagnitude > 0.01f ? along : _enemy.Heading;
            float ahead = (destination.HasValue ? DestinationSearchCells : RoadSurfaceAhead) * _field.Grid.CellSize;
            if (BossRoutes.SurfaceOnRoad(_field, from, along, ahead, up.Forward, out pivot))
            {
                forward = along;
                Vector3 roadHole = pivot + along * holeAhead;
                return BossRoutes.Crack(_field, start, roadHole, _boss.Burrow.QuicksandFactor, _crack);
            }

            return false;
        }

        // Under the ground along the route, the crack opening ahead of it; the target is ringed for the last seconds; an
        // enraged boss sends creatures up out of the crack. A mine going off on it ends the trip where it is.
        private IEnumerator TravelUnder(List<Vector3> crack, List<Vector3> route, Tower target, bool signature, float speedFactor = 1f)
        {
            State = signature ? "Burrowed" : "Arriving";
            _enemy.SetConcealed(true, 0f);
            _view.LoopScripted(ModelClipRole.BurrowTravel);
            _struck = false;
            CrackLineView line = CrackLineView.Create(_field.Effects, _field.Draped(crack));
            BurrowPower power = _boss.Burrow;
            float length = PathLength(route);
            float speed = power.TravelSpeed * speedFactor * (_enraged ? power.EnragedSpeed : 1f);
            // The warning must have its full time: a short trip goes slower rather than surprising the player.
            float minimum = power.TelegraphSeconds + MinTravelMargin;
            if (target != null && length / speed < minimum)
            {
                speed = length / minimum;
            }

            float travelled = 0f;
            float total = length / Mathf.Max(0.01f, speed);
            int minions = signature && _enraged ? power.MinionsPerCrack : 0;
            int sent = 0;
            bool ringed = false;
            while (travelled < length && Alive)
            {
                if (_struck && signature)
                {
                    break;
                }

                float dt = Time.deltaTime;
                Vector3 at = PointAlong(route, travelled, out Vector3 dir);
                float factor = _field.Climb.KindAt(at) == SurfaceKind.Quicksand ? power.QuicksandFactor : 1f;
                travelled = Mathf.Min(length, travelled + speed * factor * dt);
                at = PointAlong(route, travelled, out dir);
                _enemy.Place(OnGround(at), dir);
                line.OpenTo(Mathf.Min(line.Length, travelled + RevealLead + (line.Length - length)));
                float remaining = (length - travelled) / Mathf.Max(0.01f, speed);
                _field.Context.BossRumble = Mathf.Clamp01(RumbleRise * (1f - remaining / Mathf.Max(0.01f, total)) + 0.25f);
                if (target != null && !ringed && remaining <= power.TelegraphSeconds)
                {
                    ringed = true;
                    _ring.Show(target.Position, remaining);
                }

                if (sent < minions && travelled >= length * (sent + 1f) / (minions + 1f))
                {
                    _field.SpawnFromCrack?.Invoke(power.CrackMinion, BossRoutes.BesideCrack(at, dir, sent % 2 == 0 ? 1 : -1));
                    sent++;
                }

                yield return null;
            }

            line.OpenTo(line.Length);
            line.Fade();
        }

        // Up out of the ground: the burst of earth, and at the throw moment the tower beside it (or the rig) is flung.
        private IEnumerator Breakout(Vector3 pivot, Vector3 forward, Tower target, bool rig)
        {
            State = "Surfacing";
            BossClip up = BossClip.Of(_view, ModelClipRole.SurfaceThrow, _boss.MechanicClipRate);
            _enemy.Place(OnGround(pivot), forward);
            _view.PlayScripted(ModelClipRole.SurfaceThrow, up.Rate, true);
            yield return Wait(up.Cue);
            Vector3 hole = pivot + forward * up.Anchor;
            Impact(hole, BreakoutBurstRadius, Earth);
            _enemy.SetConcealed(false);
            _field.Context.BossRumble = 0f;
            yield return Wait(up.Moment - up.Cue);
            _ring.Hide();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (target != null && StillStanding(target))
            {
                target.Toss(right, _boss.Burrow.ThrowHealthShare, _boss.Burrow.ThrowSilenceSeconds);
                _thrown.Add(target);
                Impact(target.Position, ThrowBurstRadius, Earth);
                _field.Context.BossImpact.Raise(_boss.ImpactKick);
            }
            else if (rig)
            {
                _field.Rig?.Smash(hole);
                Impact(_field.RigPosition, BreakoutBurstRadius, Earth);
                _field.Context.BossImpact.Raise(_boss.ImpactKick);
            }

            yield return Wait(up.Seconds - up.Moment);
            _enemy.Place(OnGround(pivot + forward * up.Forward), forward);
            _view.ReleaseScript();
            if (_struck)
            {
                yield return Stunned();
            }
            else if (rig)
            {
                yield return Roar();
            }

            _struck = false;
            if (!rig)
            {
                yield return BackToRoad();
            }
        }

        // A mine went off on it while it burrowed: it stands dazed, in full view and in full reach of every tower.
        private IEnumerator Stunned()
        {
            State = "Stunned";
            _view.LoopScripted(ModelClipRole.Idle, StunIdleRate);
            float stunned = 0f;
            while (stunned < _boss.Burrow.StunSeconds && Alive)
            {
                stunned += Time.deltaTime;
                _view.Tilt = Quaternion.AngleAxis(Mathf.Sin(stunned * StunWobbleSpeed) * StunWobbleDegrees, _enemy.Heading);
                yield return null;
            }

            _view.Tilt = Quaternion.identity;
            _view.ReleaseScript();
        }

        private IEnumerator Roar()
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

        // Below its last threshold it rages: faster, and its crack lines send creatures up.
        private void CheckRage()
        {
            if (_enraged || !_burrows.Done || _enemy.Health.Fraction > _burrows.Last || _boss.Signature != BossSignature.Burrow)
            {
                return;
            }

            _enraged = true;
            _enemy.SetPace(_boss.Burrow.EnragedSpeed);
        }

        // ---------------------------------------------------------------- walking off the road and climbing

        private IEnumerator BackToRoad()
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

        private void JoinRoad()
        {
            Vector3[] road = OffRoadRoute.Build(_field.Level, transform.position, out float progress);
            road[0] = transform.position;
            _enemy.Reroute(road, progress);
            State = "Road";
        }

        // Walks a planned route at its own pace, following the ground's height with a tilt and breaking fragile decor. A
        // rock straight ahead, as far off as the climb clip's ledge, is climbed; then (or when a tower went up on the way,
        // or a rock it cannot climb is next) the caller plans again from where it stands. It never walks through a rock.
        private IEnumerator WalkRoute(List<Vector3> route, System.Action<bool> replan)
        {
            State = "Walking";
            replan(false);
            BossClip up = BossClip.Of(_view, ModelClipRole.ClimbUp, _boss.Climb.ClipRate);
            float sinceRefresh = 0f;
            for (int i = 1; i < route.Count && Alive; i++)
            {
                Vector3 target = route[i];
                while (Alive)
                {
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

            State = "Climbing";
            Climbs++;
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
            done(true);
        }

        // A gate (or the base) in front: it climbs onto it, weighs it down and strikes it from above until it falls (or a
        // burrow is due), then climbs down the far side.
        private IEnumerator ClimbStructure(IStructure structure)
        {
            State = "ClimbingGate";
            Climbs++;
            _enemy.SetDirected(true);
            BossClip up = BossClip.Of(_view, ModelClipRole.ClimbUp, _boss.Climb.ClipRate);
            BossClip down = BossClip.Of(_view, ModelClipRole.ClimbDown, _boss.Climb.ClipRate);
            Vector3 dir = Flat(structure.Position - transform.position);
            Vector3 start = transform.position;
            _enemy.Place(start, dir);
            _view.PlayScripted(ModelClipRole.ClimbUp, up.Rate, true);
            yield return Wait(up.Seconds);
            float height = structure.IsBase ? ClimbLayer.BaseHeight : ClimbLayer.GateHeight;
            Vector3 top = start + dir * up.Forward;
            _enemy.Place(new Vector3(top.x, height, top.z), dir);
            _view.LoopScripted(ModelClipRole.ClimbLoop, GateLoopRate);
            OnStructure = true;
            var gate = structure as Tower;
            gate?.SetBurden(1f);
            EnemyDefinition kind = _enemy.Definition;
            float blowEvery = kind.AttackInterval;
            float sinceBlow = 0f;
            float multiplier = _field.Context.Difficulty.StructureDamageMultiplier;
            while (Alive && structure.IsAlive && !_burrows.IsDue(_enemy.Health.Fraction))
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

            gate?.SetBurden(0f);
            if (!Alive)
            {
                OnStructure = false;
                yield break;
            }

            _view.PlayScripted(ModelClipRole.ClimbDown, down.Rate, true);
            yield return Wait(down.Seconds);
            OnStructure = false;
            Vector3 land = top + dir * down.Forward;
            _enemy.Place(new Vector3(land.x, 0f, land.z), dir);
            _view.ReleaseScript();
            JoinRoad();
        }

        // A living gate or the base straight ahead, as far ahead as the climb clip reaches.
        private IStructure StructureAhead()
        {
            if (!_boss.Climb.ClimbsGates || _enemy.IsDirected)
            {
                return null;
            }

            BossClip up = BossClip.Of(_view, ModelClipRole.ClimbUp, _boss.Climb.ClipRate);
            if (!up.Exists)
            {
                return null;
            }

            IStructure structure = _field.Context.Structures.FindBlocker(transform.position, up.Anchor);
            if (structure == null)
            {
                return null;
            }

            Vector3 to = Flat(structure.Position - transform.position);
            return Vector3.Dot(to, _enemy.Heading) > 0.5f ? structure : null;
        }

        private void BreakDecorNear(float dt)
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

        private readonly struct DivePlan
        {
            public DivePlan(int pool, TerrainKind kind, Vector3 heading, Vector3 under, Vector3 exit, Vector3 exitHeading)
            {
                Pool = pool;
                Kind = kind;
                Heading = heading;
                Under = under;
                Exit = exit;
                ExitHeading = exitHeading;
            }

            public int Pool { get; }
            public TerrainKind Kind { get; }
            public Vector3 Heading { get; }
            public Vector3 Under { get; }
            public Vector3 Exit { get; }
            public Vector3 ExitHeading { get; }
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
                    int id = (int)kind * 1000 + p;
                    if (!_dives.CanUse(id, phase))
                    {
                        continue;
                    }

                    IReadOnlyList<Vector2Int> cells = _field.Pools.Pool(kind, p);
                    Vector3 nearest = Nearest(cells, position, out float distance);
                    if (distance > dive.Reach + into.Forward * 0.5f || Vector3.Dot(Flat(nearest - position), _enemy.Heading) < -0.2f)
                    {
                        continue;
                    }

                    _field.Refresh();
                    if (!TryExit(cells, nearest, burst, out Vector3 exit, out Vector3 exitHeading))
                    {
                        continue;
                    }

                    Vector3 heading = Flat(nearest - position);
                    Vector3 under = Nearest(cells, position + heading * into.Forward, out _);
                    _dives.MarkUsed(id, phase);
                    plan = new DivePlan(id, kind, heading, under, exit, exitHeading);
                    return true;
                }
            }

            return false;
        }

        // Where it comes up: the pool cell, as far as can be from where it went in, whose bank landing lies clear and
        // nearest the road (BOSSES.md: "at the bank nearest the road").
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
                    if (!BossRoutes.Clear(_field, land, land) || !BossRoutes.ClearOfFootprints(_field, at, land))
                    {
                        continue;
                    }

                    Vector3 road = OffRoadRoute.NearestRoadCellWorld(_field.Level, land);
                    float score = (at - entry).magnitude - (road - land).magnitude * RoadPull;
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

        // Into the pool from the bank, hidden under the surface (only blasts reach it, and it heals a little) for no more
        // than its limit, and out at the far bank with a burst that splashes the structures there.
        private IEnumerator Dive(DivePlan plan)
        {
            State = "Diving";
            Dives++;
            DivePower dive = _boss.Dive;
            _enemy.SetDirected(true);
            BossClip into = BossClip.Of(_view, ModelClipRole.DiveIn, _boss.MechanicClipRate);
            BossClip burst = BossClip.Of(_view, ModelClipRole.SurfaceBurst, _boss.MechanicClipRate);
            float surface = TerrainLiquidBuilder.SurfaceLevel(plan.Kind);
            Color splash = plan.Kind == TerrainKind.Quicksand ? SandSplash : WaterSplash;
            _enemy.Place(transform.position, plan.Heading);
            _view.PlayScripted(ModelClipRole.DiveIn, into.Rate, true);
            yield return Wait(into.Moment);
            Impact(transform.position + plan.Heading * into.Forward * 0.5f, BreakoutBurstRadius, splash, false);
            _enemy.SetConcealed(true, dive.SplashShare);
            yield return Wait(into.Seconds - into.Moment);
            _view.SetHidden(true);
            State = "Submerged";
            Vector3 from = new Vector3(plan.Under.x, surface, plan.Under.z);
            Vector3 to = new Vector3(plan.Exit.x, surface, plan.Exit.z);
            float distance = Vector3.Distance(from, to);
            float seconds = _dives.SecondsFor(distance, dive.SwimSpeed);
            _wake.Show(true, from);
            float elapsed = 0f;
            while (elapsed < seconds && Alive)
            {
                float dt = Time.deltaTime;
                elapsed = Mathf.Min(seconds, elapsed + dt);
                Vector3 at = Vector3.Lerp(from, to, seconds > 0f ? elapsed / seconds : 1f);
                _enemy.Place(at, Flat(to - from));
                _wake.Follow(at);
                _enemy.Health.Heal(_enemy.Health.Max * dive.RegenPerSecond * dt);
                yield return null;
            }

            _wake.Show(false, to);
            if (!Alive)
            {
                yield break;
            }

            State = "Bursting";
            _enemy.Place(to, plan.ExitHeading);
            _view.SetHidden(false);
            _view.PlayScripted(ModelClipRole.SurfaceBurst, burst.Rate, true);
            yield return Wait(burst.Moment);
            Vector3 land = to + plan.ExitHeading * burst.Forward;
            _enemy.SetConcealed(false);
            Impact(to + plan.ExitHeading * (burst.Forward * 0.5f), BreakoutBurstRadius, splash);
            _field.Context.Structures.DamageInRadius(land, dive.BurstRadius, dive.BurstDamage * _field.Context.Difficulty.StructureDamageMultiplier);
            yield return Wait(burst.Seconds - burst.Moment);
            _enemy.Place(new Vector3(land.x, 0f, land.z), plan.ExitHeading);
            _view.ReleaseScript();
            yield return BackToRoad();
        }

        // ---------------------------------------------------------------- helpers

        private void Impact(Vector3 at, float radius, Color colour, bool kick = true)
        {
            ProjectilePool projectiles = _field.Context.Projectiles;
            projectiles.PlayBurst(new Vector3(at.x, 0f, at.z), radius, kick ? projectiles.Vfx.Mine : projectiles.Vfx.Death, colour);
        }

        private IEnumerator Wait(float seconds)
        {
            float waited = 0f;
            while (waited < seconds && Alive)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator WaitBreaking(float seconds)
        {
            float waited = 0f;
            while (waited < seconds && Alive)
            {
                waited += Time.deltaTime;
                BreakDecorNear(Time.deltaTime);
                yield return null;
            }
        }

        private static IEnumerable<TerrainKind> PoolKindsOf(PoolKinds kinds)
        {
            if ((kinds & PoolKinds.Quicksand) != 0) yield return TerrainKind.Quicksand;
            if ((kinds & PoolKinds.Water) != 0) yield return TerrainKind.Water;
            if ((kinds & PoolKinds.Bog) != 0) yield return TerrainKind.Bog;
            if ((kinds & PoolKinds.Lava) != 0) yield return TerrainKind.Lava;
            if ((kinds & PoolKinds.Ice) != 0) yield return TerrainKind.Ice;
        }

        private Vector3 Nearest(IReadOnlyList<Vector2Int> cells, Vector3 position, out float distance)
        {
            Vector3 best = position;
            float bestSqr = float.PositiveInfinity;
            foreach (Vector2Int cell in cells)
            {
                Vector3 at = _field.Grid.CellToWorld(cell);
                float sqr = (at - new Vector3(position.x, 0f, position.z)).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = at;
                }
            }

            distance = Mathf.Sqrt(bestSqr);
            return best;
        }

        private static float PathLength(List<Vector3> points)
        {
            float total = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                total += Vector3.Distance(Flat3(points[i - 1]), Flat3(points[i]));
            }

            return total;
        }

        private static Vector3 PointAlong(List<Vector3> points, float distance, out Vector3 dir)
        {
            dir = Vector3.forward;
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 a = Flat3(points[i - 1]);
                Vector3 b = Flat3(points[i]);
                float span = Vector3.Distance(a, b);
                if (span <= 0.0001f)
                {
                    continue;
                }

                dir = (b - a) / span;
                if (distance <= span)
                {
                    return a + dir * distance;
                }

                distance -= span;
            }

            return Flat3(points[points.Count - 1]);
        }

        private Vector3 OnGround(Vector3 at) => new Vector3(at.x, _field.GroundAt(at), at.z);

        private static Vector3 Flat3(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }
    }
}
