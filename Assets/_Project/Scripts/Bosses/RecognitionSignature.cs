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
    /// Recognition (the Sealed One, Docs/BOSSES.md). It does not come to you: the map comes to it. The ground at the centre
    /// of the field rumbles and cracks open, a new road of violet light rises from there to the base, and it rises out of
    /// the rift and hauls itself along that road (ploughing a furrow) round the towers, onto the base. It learns: at every
    /// phase change it adapts, and the tower type that dealt it the most damage in the phase before lands only a share of
    /// its hits in the next (ORA names it, the boss bar shows its icon dimmed, the sealed crystals at the rift take its
    /// colour). Each phase drains one gate's gun (the gate keeps blocking) and gives it back at the phase's end. Its glow
    /// changes with every phase; its last breath is the white release (the art's death look).
    /// </summary>
    public sealed class RecognitionSignature : IBossSignature
    {
        private const int CentreSearchCells = 10;
        private const int RiftCracks = 6;
        private const float RiftCrackLength = 3.2f;
        private const float RiftBurstRadius = 2.6f;
        private const float CrystalIntensity = 4.5f;
        private const float DrainIntensity = 3.5f;
        private const float ArriveMargin = 1.5f;
        private const float BaseBurstRadius = 2.2f;
        // After a haul that found no way for its body it walks the road this long before it looks for one again.
        private const float HaulRetrySeconds = 2f;
        // Cells of the old road either side of the point nearest the rift that sink as the new road rises.
        private const int RoadSinkReach = 9;
        private static readonly Color Violet = new Color(0.7f, 0.4f, 1f);
        private static readonly Color RoofDust = new Color(0.55f, 0.5f, 0.48f);
        // OnBase_Loop's hand strikes (README round 29) and its frame rate.
        private static readonly float[] OnBaseStrikeFrames = { 34f, 82f };
        private const float ClipFramesPerSecond = 30f;
        private const float StrikeKickShare = 0.4f;
        private static readonly Vector2Int NoCell = new Vector2Int(-1, -1);

        private readonly ContinentBossBrain _brain;
        private readonly RecognitionTally<TowerDefinition> _tally = new RecognitionTally<TowerDefinition>();
        private readonly GateDrainer _drainer = new GateDrainer();
        private readonly List<CrackLineView> _cracks = new List<CrackLineView>(RiftCracks);
        private readonly List<Vector3> _plan = new List<Vector3>(64);
        private readonly TowerAuraView _crystals;
        private readonly TowerAuraView _drainHaze;
        private SealCrystalsView _seal;
        private MeshRenderer _sink;
        private readonly System.Func<object, float> _scale;
        private CrackLineView _road;
        private CrackLineView _furrow;
        private Vector3 _furrowFrom;
        private int _phase;
        private bool _onBase;
        private float _sinceHaulFailed = HaulRetrySeconds;

        public RecognitionSignature(ContinentBossBrain brain)
        {
            _brain = brain;
            _crystals = TowerAuraView.Create(brain.Field.Effects, "SealedCrystals");
            _drainHaze = TowerAuraView.Create(brain.Field.Effects, "DrainedGate");
            _scale = source => source is Tower tower ? _tally.ScaleFor(tower.Definition, Power.DampenedShare) : 1f;
            brain.Enemy.DamagedBy += OnDamaged;
        }

        private BossField Field => _brain.Field;
        private Enemy Enemy => _brain.Enemy;
        private EnemyView View => _brain.View;
        private BossDefinition Boss => _brain.Boss;
        private RecognitionPower Power => _brain.Boss.Recognition;
        private Vector3 Position => _brain.transform.position;

        /// <summary>What it has learned this fight, for the tests.</summary>
        public RecognitionTally<TowerDefinition> Tally => _tally;

        /// <summary>The gate drained now, for the tests.</summary>
        public GateDrainer Drainer => _drainer;

        public bool WantsOffStructure => false;

        public void Begin()
        {
            _tally.Clear();
            DrainedLook.Apply(_drainer.Drained, false);
            _drainer.Release();
            _phase = 0;
            _sinceHaulFailed = HaulRetrySeconds;
            _onBase = false;
            Enemy.SourceScale = _scale;
            Field.Context.BossDampened = null;
            View.SetEmissionSet(Power.LookFor(0));
            _crystals.Hide();
            _drainHaze.Hide();
        }

        // The map comes to it: the centre of the field rumbles and cracks, the new road rises to the base, and it comes up.
        public IEnumerator Entrance()
        {
            _brain.State = "Entrance";
            Enemy.SetDirected(true);
            Enemy.SetConcealed(true, 0f);
            View.SetHidden(true);
            Field.Refresh();
            Vector3 centre = CentreSpot();
            Vector3 home = HomePosition();
            Enemy.Place(centre, BossGeometry.Flat(home - centre));
            PlanRoad(centre);
            if (_seal == null)
            {
                _seal = SealCrystalsView.Create(Field.Effects, centre, at => Field.Climb.KindAt(at) == SurfaceKind.Open || Field.Climb.KindAt(at) == SurfaceKind.Fragile, Field.GroundAt);
            }

            for (int i = 0; i < RiftCracks; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 360f / RiftCracks + 15f, 0f) * Vector3.forward;
                _cracks.Add(CrackLineView.Create(Field.Effects, Field.Draped(new List<Vector3> { centre, centre + dir * RiftCrackLength }), CrackStyle.Seal));
            }

            _road = CrackLineView.Create(Field.Effects, Field.Draped(_plan), CrackStyle.Seal);
            yield return Rumble(centre);
            yield return RaiseRoad();
            yield return Rise(centre);
            _furrowFrom = Position;
            DrainNextGate();
        }

        public IEnumerator NextAct()
        {
            int phase = Boss.PhaseAt(Enemy.Health.Fraction);
            if (phase != _phase)
            {
                return Adapt(phase);
            }

            if (_onBase)
            {
                return null;
            }

            IStructure home = Field.Context.Base;
            if (home == null || !home.IsAlive)
            {
                return null;
            }

            BossClip rise = BossClip.Of(View, ModelClipRole.RiseOntoBase, Boss.MechanicClipRate);
            float reach = rise.Exists ? rise.Anchor : 0f;
            if (BossGeometry.FlatDistance(Position, home.Position) <= reach + ArriveMargin)
            {
                return RiseOntoBase(home, rise);
            }

            return _sinceHaulFailed >= HaulRetrySeconds ? Haul(home, reach) : null;
        }

        public IEnumerator AtStructure(IStructure structure) => null;

        public void OnRoad(float deltaTime)
        {
        }

        public void Always(float deltaTime)
        {
            _sinceHaulFailed += deltaTime;
            if (_furrow != null && !Enemy.IsConcealed)
            {
                _furrow.OpenTo(Mathf.Min(_furrow.Length, BossGeometry.FlatDistance(_furrowFrom, Position) + 0.5f));
            }
        }

        public void End()
        {
            // Its release: the crystals go white; a fight that ends otherwise leaves them dormant.
            _seal?.Tint(_brain.Alive ? SealCrystalsView.Dormant : SealCrystalsView.Released, false);
            DrainedLook.Apply(_drainer.Drained, false);
            _drainer.Release();
            Enemy.SourceScale = null;
            Field.Context.BossDampened = null;
            Field.Context.BossRumble = 0f;
            _drainHaze.Hide();
            _crystals.Hide();
            _road?.Fade();
            _road = null;
            _furrow?.Fade();
            _furrow = null;
            foreach (CrackLineView crack in _cracks)
            {
                crack?.Fade();
            }

            _cracks.Clear();
        }

        private void OnDamaged(Enemy enemy, object source, float amount)
        {
            if (source is Tower tower)
            {
                _tally.Add(tower.Definition, amount);
            }
        }

        // ---------------------------------------------------------------- the rift

        private IEnumerator Rumble(Vector3 centre)
        {
            float elapsed = 0f;
            while (elapsed < Power.RiftSeconds && _brain.Alive)
            {
                elapsed += Time.deltaTime;
                float share = Mathf.Clamp01(elapsed / Power.RiftSeconds);
                Field.Context.BossRumble = share;
                _crystals.Show(centre, Violet, CrystalIntensity * share, true);
                foreach (CrackLineView crack in _cracks)
                {
                    crack.OpenTo(crack.Length * share);
                }

                yield return null;
            }
        }

        private IEnumerator RaiseRoad()
        {
            SinkOldRoad();
            float elapsed = 0f;
            while (elapsed < Power.RoadRiseSeconds && _brain.Alive)
            {
                elapsed += Time.deltaTime;
                _road.OpenTo(_road.Length * Mathf.Clamp01(elapsed / Power.RoadRiseSeconds));
                if (_sink != null)
                {
                    _sink.sharedMaterial.SetFloat(GroundMark.Progress, Mathf.Clamp01(elapsed / Power.RoadRiseSeconds));
                }
                yield return null;
            }
        }

        // Out of the rift (its Rise clip): the seal breaks at the clip's moment with a burst and one kick; it can be shot once up.
        private IEnumerator Rise(Vector3 centre)
        {
            _brain.State = "Rising";
            View.SetHidden(false);
            BossClip rise = BossClip.Of(View, ModelClipRole.Emerge, Boss.MechanicClipRate);
            if (rise.Exists)
            {
                View.PlayScripted(ModelClipRole.Emerge, rise.Rate, true);
            }

            yield return _brain.Wait(rise.Moment);
            Field.Context.BossRumble = 0f;
            Field.BreakDecor(centre, RiftBurstRadius);
            _brain.Impact(centre, RiftBurstRadius, Violet);
            Field.Context.BossImpact.Raise(Boss.ImpactKick);
            Enemy.SetConcealed(false);
            yield return _brain.Wait(rise.Seconds - rise.Moment);
            View.ReleaseScript();
            _brain.Record.RoseFromRift = true;
            _crystals.Show(centre, Violet, CrystalIntensity, false);
            // The rift has opened: its cracks die down, the new road stays lit.
            foreach (CrackLineView crack in _cracks)
            {
                crack?.Fade();
            }

            _cracks.Clear();
        }

        // The open cell nearest the middle of the field where its whole body comes up clear of every tower.
        private Vector3 CentreSpot()
        {
            GridMap grid = Field.Grid;
            Vector2Int middle = new Vector2Int(grid.Size.x / 2, grid.Size.y / 2);
            Vector3 best = grid.CellToWorld(middle);
            for (int ring = 0; ring <= CentreSearchCells; ring++)
            {
                for (int x = -ring; x <= ring; x++)
                {
                    for (int y = -ring; y <= ring; y++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != ring)
                        {
                            continue;
                        }

                        Vector2Int cell = middle + new Vector2Int(x, y);
                        Vector3 at = grid.CellToWorld(cell);
                        if (grid.Contains(cell) && Field.Climb.KindAt(cell) == SurfaceKind.Open && BossRoutes.BodyOverlaps(Field, at, at, _brain.BodyHalfWidth, NoCell) == 0)
                        {
                            return _brain.OnGround(at);
                        }
                    }
                }
            }

            return _brain.OnGround(best);
        }

        // The new road: the way its body can haul from the rift to the base round the towers, or straight when there is none.
        private void PlanRoad(Vector3 from)
        {
            Vector3 home = HomePosition();
            Vector3 goal = from + BossGeometry.Flat(home - from) * Mathf.Max(0f, BossGeometry.FlatDistance(from, home) - 3f);
            if (!BossRoutes.WalkTo(Field, from, Field.Grid.WorldToCell(goal), Boss.Climb.ClimbsRocks, 1, _plan))
            {
                _plan.Clear();
                _plan.Add(from);
                _plan.Add(goal);
            }
        }

        // README round 29 (RoadSink): where the new road leaves the old one, the old road tears and darkens, the middle first,
        // violet seeping from its deepest cracks. Only the stretch nearest the rift: the walkers still use the old road.
        private void SinkOldRoad()
        {
            Material material = GroundMark.MaterialFor(BossFxArt.Mark.RoadSink);
            EnemyPath[] paths = Field.Level.Paths;
            if (material == null || paths == null || paths.Length == 0 || paths[0].Waypoints == null || paths[0].Waypoints.Length < 2)
            {
                return;
            }

            var dense = new List<Vector3>();
            Vector2Int[] waypoints = paths[0].Waypoints;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Vector3 a = Field.Grid.CellToWorld(waypoints[i]);
                Vector3 b = Field.Grid.CellToWorld(waypoints[i + 1]);
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b)));
                for (int s = 0; s < steps; s++)
                {
                    dense.Add(Vector3.Lerp(a, b, s / (float)steps));
                }
            }

            dense.Add(Field.Grid.CellToWorld(waypoints[waypoints.Length - 1]));
            int nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < dense.Count; i++)
            {
                float distance = BossGeometry.FlatDistance(dense[i], Position);
                if (distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }

            int from = Mathf.Max(0, nearest - RoadSinkReach);
            int to = Mathf.Min(dense.Count - 1, nearest + RoadSinkReach);
            if (to - from < 2)
            {
                return;
            }

            float width = Field.Level.RoadWidth * Field.Grid.CellSize;
            material.SetFloat(GroundMark.Progress, 0f);
            _sink = GroundMark.Strip(Field.Effects, "RoadSink", Field.Draped(dense.GetRange(from, to - from + 1)), width,
                BossFxArt.Active.TileLength(BossFxArt.Mark.RoadSink) * width / BossFxArt.Active.Width(BossFxArt.Mark.RoadSink), material, false);
        }

        private Vector3 HomePosition() => Field.Context.Base != null ? Field.Context.Base.Position : LevelVisualBuilder.BasePosition(Field.Level);

        // ---------------------------------------------------------------- hauling and the base

        // Along the new road to the base, round the towers, its lower body ploughing a furrow; a phase change stops it.
        private IEnumerator Haul(IStructure home, float reach)
        {
            _brain.State = "Hauling";
            Enemy.SetDirected(true);
            Vector3 dir = BossGeometry.Flat(home.Position - Position);
            Vector3 stand = home.Position - dir * reach;
            Vector2Int goal = Field.Grid.WorldToCell(stand);
            int phase = _phase;
            _furrowFrom = Position;
            Field.Refresh();
            if (BossRoutes.WalkTo(Field, Position, goal, Boss.Climb.ClimbsRocks, 1, _plan) || BossRoutes.WalkTo(Field, Position, goal, Boss.Climb.ClimbsRocks, 0, _plan))
            {
                _furrow?.Fade();
                _furrow = CrackLineView.Create(Field.Effects, Field.Draped(_plan), CrackStyle.Furrow);
            }

            bool arrived = false;
            yield return _brain.WalkTo(goal, true, () => Boss.PhaseAt(Enemy.Health.Fraction) != phase, value => arrived = value, "Hauling");
            if (!arrived && Boss.PhaseAt(Enemy.Health.Fraction) == phase && _brain.Alive)
            {
                // No way for its body between the towers: it takes the road like any creature for a while and fights through.
                _sinceHaulFailed = 0f;
                _brain.JoinRoad();
            }
        }

        // It pulls itself up onto the base (the base takes the crush at the clip's moment) and stays on it, striking it.
        private IEnumerator RiseOntoBase(IStructure home, BossClip rise)
        {
            _brain.State = "RisingOntoBase";
            _onBase = true;
            Enemy.SetDirected(true);
            Vector3 dir = BossGeometry.Flat(home.Position - Position);
            Enemy.Place(_brain.OnGround(home.Position - dir * rise.Anchor), dir);
            float multiplier = Field.Context.Difficulty.StructureDamageMultiplier;
            if (rise.Exists)
            {
                View.PlayScripted(ModelClipRole.RiseOntoBase, rise.Rate, true);
            }

            yield return _brain.Wait(rise.Moment);
            if (_brain.Alive && home.IsAlive)
            {
                home.TakeDamage(Power.CrushDamage * multiplier);
                Field.Context.StructureHit.Raise(home.Position);
                _brain.Impact(home.Position, BaseBurstRadius, Violet);
                Field.Context.BossImpact.Raise(Boss.ImpactKick);
                _brain.Record.BaseCrushes++;
            }

            yield return _brain.Wait(rise.Seconds - rise.Moment);
            _brain.OnStructure = true;
            _brain.State = "OnBase";
            LieOnBase(home, dir);
            BossClip adapt = BossClip.Of(View, ModelClipRole.Adapt, Boss.MechanicClipRate);
            float blowEvery = Power.BaseBlowSeconds;
            float sinceBlow = 0f;
            int learning = -1;
            float learnIn = 0f;
            float loopIn = -1f;
            float loopClock = 0f;
            while (_brain.Alive && home.IsAlive)
            {
                float dt = Time.deltaTime;
                int phase = Boss.PhaseAt(Enemy.Health.Fraction);
                if (phase != _phase && learning < 0)
                {
                    // On the base it adapts without leaving it: its Adapt pulse over the roof, then back to its loop.
                    learning = phase;
                    learnIn = adapt.Moment;
                    loopIn = adapt.Seconds;
                    if (adapt.Exists)
                    {
                        View.PlayScripted(ModelClipRole.Adapt, adapt.Rate, true);
                    }
                }

                if (learning >= 0 && (learnIn -= dt) <= 0f)
                {
                    Learn(learning);
                    learning = -1;
                }

                if (loopIn >= 0f && (loopIn -= dt) < 0f)
                {
                    LoopOnBase();
                    loopClock = 0f;
                }

                ModelClip loop = View.ClipOf(ModelClipRole.OnBaseLoop);
                if (loop != null && loop.Length > 0f)
                {
                    // Its own loop slams a hand on the roof twice (README round 29: frames 34 and 82): the base takes each blow
                    // with dust and a short kick there, as hard per second as the timed blows of the stand-in.
                    if (learning < 0 && loopIn < 0f)
                    {
                        float before = loopClock;
                        loopClock += dt * Power.OnBaseLoopRate;
                        float seconds = loop.Length / Power.OnBaseLoopRate;
                        float blow = Enemy.Definition.BlockadeDamage * multiplier * (seconds / OnBaseStrikeFrames.Length) / blowEvery;
                        foreach (float frame in OnBaseStrikeFrames)
                        {
                            if (Crossed(before, loopClock, frame / ClipFramesPerSecond, loop.Length))
                            {
                                Strike(home, blow);
                            }
                        }
                    }
                }
                else
                {
                    sinceBlow += dt;
                    if (sinceBlow >= blowEvery)
                    {
                        sinceBlow -= blowEvery;
                        home.TakeDamage(Enemy.Definition.BlockadeDamage * multiplier);
                        Field.Context.StructureHit.Raise(home.Position);
                    }
                }

                yield return null;
            }

            _brain.OnStructure = false;
        }

        // Its own OnBaseLoop is drawn from the rise's pivot over the base. Until the art has one, it climbs onto the roof
        // and stands there in the stand-in loop of its data: the rise's last frame no longer freezes.
        private void LieOnBase(IStructure home, Vector3 dir)
        {
            if (View.ClipOf(ModelClipRole.OnBaseLoop) == null)
            {
                Vector3 roof = home.Position;
                Enemy.Place(new Vector3(roof.x, ClimbLayer.BaseHeight, roof.z), dir);
            }

            LoopOnBase();
        }

        // True when a looping clip's clock passed a moment of the loop between two frames.
        private static bool Crossed(float before, float after, float moment, float length)
        {
            float from = Mathf.Repeat(before, length);
            float span = after - before;
            float to = from + span;
            return (moment > from && moment <= to) || (moment + length > from && moment + length <= to);
        }

        private void Strike(IStructure home, float damage)
        {
            home.TakeDamage(damage);
            Field.Context.StructureHit.Raise(home.Position);
            _brain.Impact(home.Position, BaseBurstRadius * 0.6f, RoofDust, false);
            Field.Context.BossImpact.Raise(Boss.ImpactKick * StrikeKickShare);
        }

        private void LoopOnBase()
        {
            bool own = View.ClipOf(ModelClipRole.OnBaseLoop) != null;
            View.LoopScripted(own ? ModelClipRole.OnBaseLoop : Power.OnBaseStandIn, Power.OnBaseLoopRate);
        }

        // ---------------------------------------------------------------- recognition

        // It pulses (its Adapt clip): at the pulse it has learned the weapon that hurt it most.
        private IEnumerator Adapt(int phase)
        {
            _brain.State = "Adapting";
            Enemy.SetDirected(true);
            BossClip adapt = BossClip.Of(View, ModelClipRole.Adapt, Boss.MechanicClipRate);
            if (adapt.Exists)
            {
                View.PlayScripted(ModelClipRole.Adapt, adapt.Rate, true);
            }

            yield return _brain.Wait(adapt.Moment);
            Learn(phase);
            yield return _brain.Wait(adapt.Seconds - adapt.Moment);
            View.ReleaseScript();
            Enemy.SetDirected(false);
        }

        private void Learn(int phase)
        {
            _phase = phase;
            TowerDefinition learned = _tally.Learn();
            Field.Context.BossDampened = learned;
            View.SetEmissionSet(Power.LookFor(phase));
            _brain.Impact(Position, BaseBurstRadius, Violet, false);
            if (learned != null)
            {
                _brain.Record.Learned.Add(learned);
                Field.Context.BossAdapted.Raise(string.Format(Power.AdaptLine, learned.DisplayName));
                // The sealed crystals at the rift take the colour of the weapon it has learned.
                _crystals.Show(_crystals.transform.position, learned.Color, CrystalIntensity, false);
                _seal?.Tint(learned.Color, true);
            }

            DrainNextGate();
        }

        // The last gate gets its gun back; another gate's gun goes dark for this phase.
        private void DrainNextGate()
        {
            if (!Power.DrainsGates)
            {
                return;
            }

            Tower previous = _drainer.Drained;
            Tower gate = GateDrainer.Choose(Field.Context.Structures, Position, previous);
            _drainer.Drain(gate);
            DrainedLook.Apply(previous, false);
            DrainedLook.Apply(gate, true);
            if (gate == null)
            {
                _drainHaze.Hide();
                return;
            }

            _brain.Record.Drained.Add(gate);
            _drainHaze.Show(gate.Position, Violet, DrainIntensity, true);
            CrackLineView thread = CrackLineView.Create(Field.Effects, Field.Draped(new List<Vector3> { Position, gate.Position }), CrackStyle.Drain);
            thread.OpenTo(thread.Length);
            thread.Fade();
        }
    }
}
