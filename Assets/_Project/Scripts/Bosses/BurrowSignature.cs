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
    /// Burrow (Gravemaw, Docs/BOSSES.md): its entrance through a crack to the colony rig, which it smashes; at each
    /// threshold it dives under, runs a crack line round towers and gates to the tower that hurt it most and throws that
    /// tower; a mine going off on it while it burrows leaves it dazed on the surface; below its last threshold it rages,
    /// faster, and its cracks send creatures up.
    /// </summary>
    public sealed class BurrowSignature : IBossSignature
    {
        private const float EntranceCrackCells = 7f;
        private const float RevealLead = 2.5f;
        private const float MinTravelMargin = 0.5f;
        private const float StunWobbleDegrees = 9f;
        private const float StunWobbleSpeed = 7f;
        private const float StunIdleRate = 0.4f;
        private const float RoadSurfaceAhead = 7f;
        private const float DestinationSearchCells = 4f;
        private const float ThrowBurstRadius = 1.4f;
        private const float BreakoutBurstRadius = 1.8f;
        private const float RumbleRise = 1.5f;
        // An old crack the new one would touch cools away this fast, before the new one opens over that ground.
        private const float CrossedFadeSeconds = 0.8f;
        // How far either side of its line a crack's footprint reaches beyond its painted half width.
        private const float CrackClearanceMargin = 0.5f;
        // The planned line must start this far ahead of the dive hole along the dive, else the hole replaces its start.
        private const float MinStartLead = 0.3f;
        private const float RigDustRadius = 3.4f;
        private const float RigSparkRadius = 1.8f;
        private const float RigSparkHeight = 1.6f;
        private const float RigSecondDustShare = 0.7f;
        // The debris flies up between the hole and the derrick.
        private const float RigDebrisShare = 0.5f;
        private static readonly Color RigDust = new Color(0.55f, 0.48f, 0.4f);
        private static readonly Color RigSparks = new Color(1f, 0.6f, 0.2f);

        // README round 28: what the throw flings stands about this far (model units) to the right of the hole.
        private const float ThrowSideCells = 1f;
        private static readonly Color Earth = new Color(1f, 0.55f, 0.25f);
        private static readonly float[] Turns = { 0f, 25f, -25f, 50f, -50f, 80f, -80f, 120f, -120f, 180f };

        private readonly ContinentBossBrain _brain;
        private readonly List<Vector3> _route = new List<Vector3>(64);
        private readonly List<Vector3> _crack = new List<Vector3>(64);
        // The cracks still on the field and the lines they were laid along: a new one keeps off them, or they go first.
        private readonly List<CrackLineView> _lines = new List<CrackLineView>(4);
        private readonly List<List<Vector3>> _linePaths = new List<List<Vector3>>(4);
        private readonly HashSet<Vector2Int> _scarred = new HashSet<Vector2Int>();
        private ThresholdTracker _burrows = new ThresholdTracker(new float[0]);
        private bool _struck;
        private bool _enraged;

        public BurrowSignature(ContinentBossBrain brain)
        {
            _brain = brain;
            // The brain and its creature live and die together, so the subscription lives as long as the signature.
            brain.Enemy.StruckWhileHidden += (enemy, amount) => _struck = true;
        }

        private BossField Field => _brain.Field;
        private Enemy Enemy => _brain.Enemy;
        private EnemyView View => _brain.View;
        private BossDefinition Boss => _brain.Boss;
        private bool Alive => _brain.Alive;
        private Vector3 Position => _brain.transform.position;

        public bool WantsOffStructure => _burrows.IsDue(Enemy.Health.Fraction);

        public void Begin()
        {
            _burrows = new ThresholdTracker(Boss.Burrow.Thresholds);
            _struck = false;
            _enraged = false;
        }

        public IEnumerator Entrance() => RigEntrance();

        public IEnumerator NextAct() => _burrows.TryCross(Enemy.Health.Fraction) ? Burrow(true) : null;

        public IEnumerator AtStructure(IStructure structure) => null;

        public void OnRoad(float deltaTime) => CheckRage();

        public void Always(float deltaTime)
        {
        }

        public void End()
        {
        }

        // ---------------------------------------------------------------- entrance

        // The ground cracks open along the field's edge towards the colony rig; the boss breaks out under it, flings the
        // derrick over and roars, then makes for the road.
        private IEnumerator RigEntrance()
        {
            _brain.State = "Entrance";
            Enemy.SetDirected(true);
            Enemy.SetConcealed(true, 0f);
            Field.Refresh();
            Vector3 rig = Field.RigPosition;
            Vector3 towards = BossGeometry.Flat(OffRoadRoute.NearestRoadCellWorld(Field.Level, rig) - rig);
            BossClip throwClip = BossClip.Of(View, ModelClipRole.SurfaceThrow, Boss.MechanicClipRate);
            Vector3 inward = towards;
            Vector3 hole = rig;
            Vector3 pivot = rig;
            // It comes up facing the field, the rig on its right where the throw lands; it turns until its body has room.
            foreach (float turn in Turns)
            {
                inward = Quaternion.Euler(0f, turn, 0f) * towards;
                hole = rig - Vector3.Cross(Vector3.up, inward) * (ThrowSideCells * _brain.Scale);
                pivot = hole - inward * throwClip.Anchor;
                if (BossRoutes.ClearOfFootprints(Field, pivot, pivot + inward * throwClip.Forward))
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
            Enemy.Place(origin, BossGeometry.Flat(pivot - origin));
            yield return TravelUnder(_crack, _route, null, false);
            yield return Breakout(pivot, inward, null, true);
            yield return Burrow(false);
        }

        // Where the entrance crack begins: along the field's edge from the rig, on the side that stays outside the field.
        private Vector3 EntranceOrigin(Vector3 rig)
        {
            Vector3 outward = rig;
            outward.y = 0f;
            Vector3 along = Vector3.Cross(Vector3.up, outward.normalized) * (EntranceCrackCells * Field.Grid.CellSize);
            Vector2 half = Field.Level.WorldSize * 0.5f;
            Vector3 first = rig + along;
            return ColonyRigPlacement.OutsideField(first, half, 0f) ? first : rig - along;
        }

        // ---------------------------------------------------------------- burrow

        // The signature dive at a threshold goes for the tower that hurt it most; the dive after the entrance (not a
        // signature one) only carries it from the rig to the start of the road, so the fight runs the whole road.
        private IEnumerator Burrow(bool signature)
        {
            _brain.State = "Burrow";
            if (signature)
            {
                _brain.Record.Burrows++;
            }

            Enemy.SetDirected(true);
            Field.Refresh();
            Tower target = signature ? _brain.Tally.Top(StillStanding) : null;
            MarkScarred();
            Vector3? destination = signature ? (Vector3?)null : RoadPoint(Boss.Burrow.ReturnToRoadShare);
            BossClip down = BossClip.Of(View, ModelClipRole.BurrowDown, Boss.MechanicClipRate);
            BossClip up = BossClip.Of(View, ModelClipRole.SurfaceThrow, Boss.MechanicClipRate);
            if (!PlanBurrow(target, destination, down, up, out Vector3 heading, out Vector3 pivot, out Vector3 forward, out bool throws))
            {
                // Nowhere to go under without crossing a structure: it roars and keeps walking.
                yield return _brain.Roar();
                yield return _brain.BackToRoad();
                yield break;
            }

            // An old crack the new line still meets cools away during the dive, before the new one opens there.
            FadeCrossed(_crack);
            Enemy.Place(Position, heading);
            View.PlayScripted(ModelClipRole.BurrowDown, down.Rate, true);
            yield return _brain.Wait(down.Moment);
            Vector3 hole0 = Position + heading * down.Anchor;
            _brain.Impact(hole0, BreakoutBurstRadius, Earth);
            Enemy.SetConcealed(true, 0f);
            yield return _brain.Wait(down.Seconds - down.Moment);
            Enemy.Place(Position + heading * down.Forward, heading);
            // The hole opens the line; when the planned start lies behind it the strip would double back, so the hole
            // takes the start's place instead.
            bool replaced = Vector3.Dot(BossGeometry.Flat(_crack[0] - hole0), heading) < MinStartLead;
            if (replaced)
            {
                _crack[0] = hole0;
            }
            else
            {
                _crack.Insert(0, hole0);
            }

            CrackPath.RemoveBacktracks(_crack, MinStartLead);
            FadeCrossed(_crack);
            _route.Clear();
            _route.Add(Position);
            for (int i = 1; i < _crack.Count - 1; i++)
            {
                // The first point at or behind where it now stands under the ground is already passed.
                if (i == 1 && Vector3.Dot(BossGeometry.Flat(_crack[1] - Position), heading) <= MinStartLead)
                {
                    continue;
                }

                _route.Add(_crack[i]);
            }

            // The crack is routed round every footprint up to its hole, but the pivot lies a little short of the hole
            // along the surfacing line: a straight cut from the last crack bend to the pivot can pass under the target
            // tower itself. Then it runs on to the hole first (the surfacing line from there back to the pivot is clear).
            if (!BossRoutes.ClearOfFootprints(Field, _route[_route.Count - 1], pivot))
            {
                _route.Add(_crack[_crack.Count - 1]);
            }

            _route.Add(pivot);
            yield return TravelUnder(_crack, _route, throws ? target : null, signature, signature ? 1f : Boss.Burrow.ReturnSpeedFactor);
            yield return Breakout(Position, _struck ? Enemy.Heading : forward, throws && !_struck ? target : null, false);
        }

        // A point on the main road, a share of the way from its start.
        private Vector3 RoadPoint(float share)
        {
            Vector2Int[] road = Field.Level.MainRoad;
            int index = Mathf.Clamp(Mathf.RoundToInt(share * (road.Length - 1)), 0, road.Length - 1);
            return Field.Level.Road.CentreOf(road[index]);
        }

        private bool StillStanding(Tower tower) => tower != null && tower.IsAlive && !tower.IsUpset && Field.Context.Structures.Contains(tower);

        // Which way it dives, where it travels to and where it comes up: beside the target when it can, else down the road.
        private bool PlanBurrow(Tower target, Vector3? destination, BossClip down, BossClip up, out Vector3 heading, out Vector3 pivot, out Vector3 forward, out bool throws)
        {
            heading = pivot = forward = Vector3.zero;
            throws = false;
            Vector3 position = Position;
            Vector3 aim = target != null ? BossGeometry.Flat(target.Position - position) : destination.HasValue ? BossGeometry.Flat(destination.Value - position) : Enemy.Heading;
            // Of the ways it can dive without its body crossing a structure, it takes the one whose crack line runs the
            // shortest to where it comes up: a short, direct line reads as "it is coming for that tower".
            float best = float.PositiveInfinity;
            var bestCrack = new List<Vector3>();
            foreach (float turn in Turns)
            {
                Vector3 dir = Quaternion.Euler(0f, turn, 0f) * aim;
                Vector3 start = position + dir * down.Forward;
                bool inside = Field.Grid.Contains(Field.Grid.WorldToCell(start)) || destination.HasValue;
                if (!inside || !BossRoutes.ClearOfFootprints(Field, position, start)
                    || !PlanCrack(target, destination, start, up, out Vector3 end, out Vector3 endForward, out bool hit))
                {
                    continue;
                }

                float length = BossGeometry.PathLength(_crack) + down.Forward;
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

        // The crack from where it travels under the ground to where it comes up: beside the target when there is room for
        // its whole body clear of the other towers, else on the road (ahead of it, or by its destination).
        private bool PlanCrack(Tower target, Vector3? destination, Vector3 start, BossClip up, out Vector3 pivot, out Vector3 forward, out bool throws)
        {
            Vector3 position = Position;
            pivot = forward = Vector3.zero;
            throws = false;
            float holeAhead = up.Anchor;
            float halfWidth = _brain.BodyHalfWidth;
            if (target != null && BossRoutes.SurfaceBeside(Field, target, start, holeAhead, up.Forward, halfWidth, out Vector3 hole, out pivot, out forward)
                && BossRoutes.Crack(Field, start, hole, Boss.Burrow.QuicksandFactor, _crack, _scarred))
            {
                throws = true;
                return true;
            }

            Vector3 from = destination ?? start;
            Vector3 along = Field.Level.Road.DirectionAt(Field.Grid.WorldToCell(destination ?? position));
            along = along.sqrMagnitude > 0.01f ? along : Enemy.Heading;
            float ahead = (destination.HasValue ? DestinationSearchCells : RoadSurfaceAhead) * Field.Grid.CellSize;
            if (BossRoutes.SurfaceOnRoad(Field, from, along, ahead, up.Forward, out pivot))
            {
                forward = along;
                Vector3 roadHole = pivot + along * holeAhead;
                return BossRoutes.Crack(Field, start, roadHole, Boss.Burrow.QuicksandFactor, _crack, _scarred);
            }

            return false;
        }

        // Under the ground along the route, the crack opening ahead of it; the target is ringed for the last seconds; an
        // enraged boss sends creatures up out of the crack. A mine going off on it ends the trip where it is.
        private IEnumerator TravelUnder(List<Vector3> crack, List<Vector3> route, Tower target, bool signature, float speedFactor = 1f)
        {
            _brain.State = signature ? "Burrowed" : "Arriving";
            Enemy.SetConcealed(true, 0f);
            View.LoopScripted(ModelClipRole.BurrowTravel);
            _struck = false;
            CrackLineView line = CrackLineView.Create(Field.Effects, Field.Draped(crack), CrackStyle.Burrow, IsFootprint);
            _lines.Add(line);
            _linePaths.Add(new List<Vector3>(crack));
            BurrowPower power = Boss.Burrow;
            float length = BossGeometry.PathLength(route);
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
                Vector3 at = BossGeometry.PointAlong(route, travelled, out Vector3 dir);
                float factor = Field.Climb.KindAt(at) == SurfaceKind.Quicksand ? power.QuicksandFactor : 1f;
                travelled = Mathf.Min(length, travelled + speed * factor * dt);
                at = BossGeometry.PointAlong(route, travelled, out dir);
                Enemy.Place(_brain.OnGround(at), dir);
                line.OpenTo(Mathf.Min(line.Length, travelled + RevealLead + (line.Length - length)));
                float remaining = (length - travelled) / Mathf.Max(0.01f, speed);
                Field.Context.BossRumble = Mathf.Clamp01(RumbleRise * (1f - remaining / Mathf.Max(0.01f, total)) + 0.25f);
                if (target != null && !ringed && remaining <= power.TelegraphSeconds)
                {
                    ringed = true;
                    _brain.Ring.Show(target.Position, remaining);
                }

                if (sent < minions && travelled >= length * (sent + 1f) / (minions + 1f))
                {
                    Field.Spawner?.SpawnFromCrack(power.CrackMinion, BossRoutes.BesideCrack(at, dir, sent % 2 == 0 ? 1 : -1));
                    sent++;
                }

                yield return null;
            }

            line.OpenTo(line.Length);
            line.Fade();
        }

        private bool IsFootprint(Vector3 at) => Field.Climb.IsFootprint(Field.Grid.WorldToCell(at));

        // The ground under every crack still on the field (the faded ones dropped), for the next line to keep off.
        private void MarkScarred()
        {
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                if (_lines[i] == null)
                {
                    _lines.RemoveAt(i);
                    _linePaths.RemoveAt(i);
                }
            }

            _scarred.Clear();
            foreach (List<Vector3> path in _linePaths)
            {
                CrackPath.Cover(path, CrackStyle.Burrow.MarkWidth * 0.5f + CrackClearanceMargin, Field.Grid.CellSize, Field.Grid.WorldToCell, _scarred);
            }
        }

        // Where the new line still meets an old crack (it could not keep off it), the old one cools away first.
        private void FadeCrossed(List<Vector3> crack)
        {
            float clearance = CrackStyle.Burrow.MarkWidth + CrackClearanceMargin;
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i] != null && CrackPath.Distance(_linePaths[i], crack) < clearance)
                {
                    _lines[i].Fade(CrossedFadeSeconds);
                }
            }
        }

        // Up out of the ground: the burst of earth, and at the throw moment the tower beside it (or the rig) is flung.
        private IEnumerator Breakout(Vector3 pivot, Vector3 forward, Tower target, bool rig)
        {
            _brain.State = "Surfacing";
            BossClip up = BossClip.Of(View, ModelClipRole.SurfaceThrow, Boss.MechanicClipRate);
            Enemy.Place(_brain.OnGround(pivot), forward);
            View.PlayScripted(ModelClipRole.SurfaceThrow, up.Rate, true);
            yield return _brain.Wait(up.Cue);
            Vector3 hole = pivot + forward * up.Anchor;
            _brain.Impact(hole, BreakoutBurstRadius, Earth, !rig);
            if (rig)
            {
                WreckRig(hole);
            }
            Enemy.SetConcealed(false);
            Field.Context.BossRumble = 0f;
            yield return _brain.Wait(up.Moment - up.Cue);
            _brain.Ring.Hide();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (target != null && StillStanding(target))
            {
                target.Toss(right, Boss.Burrow.ThrowHealthShare, Boss.Burrow.ThrowSilenceSeconds);
                _brain.Record.Thrown.Add(target);
                _brain.Impact(target.Position, ThrowBurstRadius, Earth);
                Field.Context.BossImpact.Raise(Boss.ImpactKick);
            }

            yield return _brain.Wait(up.Seconds - up.Moment);
            Enemy.Place(_brain.OnGround(pivot + forward * up.Forward), forward);
            View.ReleaseScript();
            if (_struck)
            {
                yield return Stunned();
            }
            else if (rig)
            {
                yield return _brain.Roar();
            }

            _struck = false;
            if (!rig)
            {
                yield return _brain.BackToRoad();
            }
        }

        // It bursts up under the derrick: one kick, and in the dust, sparks and debris the derrick becomes its wreck (the
        // view swaps the model under the cloud; it never tips the intact rig over).
        private void WreckRig(Vector3 hole)
        {
            Vector3 rig = Field.RigPosition;
            ProjectilePool projectiles = Field.Context.Projectiles;
            projectiles.PlayBurst(new Vector3(rig.x, 0f, rig.z), RigDustRadius, projectiles.Vfx.Death, RigDust);
            projectiles.PlayBurst(rig + Vector3.up * RigSparkHeight, RigSparkRadius, projectiles.Vfx.Mine, RigSparks);
            projectiles.PlayBurst(Vector3.Lerp(hole, rig, RigDebrisShare), BreakoutBurstRadius, projectiles.Vfx.Default, Earth);
            Field.Context.BossImpact.Raise(Boss.ImpactKick);
            if (Field.Rig == null)
            {
                return;
            }

            Field.Rig.Smash(hole);
            if (Field.Rig.CollapseMoment > 0f)
            {
                _brain.StartCoroutine(RigCollapse(Field.Rig.CollapseMoment));
            }
        }

        // The derrick's top hits the ground in its collapse clip: sparks off the steel and a second, lower dust cloud (no
        // second kick: the one at the break-out is the impact).
        private IEnumerator RigCollapse(float moment)
        {
            float waited = 0f;
            while (waited < moment)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Vector3 rig = Field.RigPosition;
            ProjectilePool projectiles = Field.Context.Projectiles;
            projectiles.PlayBurst(new Vector3(rig.x, 0f, rig.z), RigDustRadius * RigSecondDustShare, projectiles.Vfx.Death, RigDust);
            projectiles.PlayBurst(rig + Vector3.up * (RigSparkHeight * RigSecondDustShare), RigSparkRadius, projectiles.Vfx.Mine, RigSparks);
        }

        // A mine went off on it while it burrowed: it stands dazed, in full view and in full reach of every tower.
        private IEnumerator Stunned()
        {
            _brain.State = "Stunned";
            View.LoopScripted(ModelClipRole.Idle, StunIdleRate);
            float stunned = 0f;
            while (stunned < Boss.Burrow.StunSeconds && Alive)
            {
                stunned += Time.deltaTime;
                View.Tilt = Quaternion.AngleAxis(Mathf.Sin(stunned * StunWobbleSpeed) * StunWobbleDegrees, Enemy.Heading);
                yield return null;
            }

            View.Tilt = Quaternion.identity;
            View.ReleaseScript();
        }

        // Below its last threshold it rages: faster, and its crack lines send creatures up.
        private void CheckRage()
        {
            if (_enraged || !_burrows.Done || Enemy.Health.Fraction > _burrows.Last)
            {
                return;
            }

            _enraged = true;
            Enemy.SetPace(Boss.Burrow.EnragedSpeed);
        }
    }
}
