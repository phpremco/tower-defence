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
        private static readonly Color Violet = new Color(0.7f, 0.4f, 1f);
        private static readonly Vector2Int NoCell = new Vector2Int(-1, -1);

        private readonly ContinentBossBrain _brain;
        private readonly RecognitionTally<TowerDefinition> _tally = new RecognitionTally<TowerDefinition>();
        private readonly GateDrainer _drainer = new GateDrainer();
        private readonly List<CrackLineView> _cracks = new List<CrackLineView>(RiftCracks);
        private readonly List<Vector3> _plan = new List<Vector3>(64);
        private readonly TowerAuraView _crystals;
        private readonly TowerAuraView _drainHaze;
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
            float elapsed = 0f;
            while (elapsed < Power.RoadRiseSeconds && _brain.Alive)
            {
                elapsed += Time.deltaTime;
                _road.OpenTo(_road.Length * Mathf.Clamp01(elapsed / Power.RoadRiseSeconds));
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
            float blowEvery = Power.BaseBlowSeconds;
            float sinceBlow = 0f;
            while (_brain.Alive && home.IsAlive)
            {
                int phase = Boss.PhaseAt(Enemy.Health.Fraction);
                if (phase != _phase)
                {
                    // On the base it adapts without leaving it: the pulse, the line and the look, no clip.
                    Learn(phase);
                }

                sinceBlow += Time.deltaTime;
                if (sinceBlow >= blowEvery)
                {
                    sinceBlow -= blowEvery;
                    home.TakeDamage(Enemy.Definition.BlockadeDamage * multiplier);
                    Field.Context.StructureHit.Raise(home.Position);
                }

                yield return null;
            }

            _brain.OnStructure = false;
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
