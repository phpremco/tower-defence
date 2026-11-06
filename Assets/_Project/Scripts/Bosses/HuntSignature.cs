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
    /// Hatred of the Cold (the Cinder Matriarch, Docs/BOSSES.md): she leaves the road to hunt the towers that chill, the
    /// nearest first. A thin heat line runs to the hunted tower and it starts to glow warm; she walks between the pads round
    /// every tower and over the rock ridges (plants burn as she passes), takes a lava pool as a shortcut when one lies
    /// nearer her target, and smashes the tower into a wreck that can be rebuilt. One tower per approach, never more than
    /// about a quarter of the towers; without cold on the field she walks the road. Her trail of lava burns the towers it
    /// touches and the walkers less; from her brood phase she lays eggs in the lava pools that hatch unless shot.
    /// </summary>
    public sealed class HuntSignature : IBossSignature
    {
        private const float ChooseEverySeconds = 0.5f;
        private const float BurnEverySeconds = 0.25f;
        private const float AuraIntensity = 2.6f;
        private const float SmashBurstRadius = 1.6f;
        private const float EggJitter = 0.35f;
        private const float ClutchRetrySeconds = 5f;
        private const float BodyTailShare = 0.45f;
        private const float BodyLengthPerScale = 4.6f;
        private static readonly Color Warm = new Color(1f, 0.45f, 0.12f);
        private static readonly Color Cinders = new Color(1f, 0.5f, 0.15f);

        private readonly ContinentBossBrain _brain;
        private readonly List<ColdHunt.Candidate> _candidates = new List<ColdHunt.Candidate>(32);
        private readonly List<Tower> _candidateTowers = new List<Tower>(32);
        private readonly HashSet<Tower> _unreachable = new HashSet<Tower>();
        private readonly List<float> _levelSlows = new List<float>(4);
        private LavaTrail _trail;
        private LavaTrailView _trailView;
        private TowerAuraView _aura;
        private BroodKeeper _brood;
        private CrackLineView _heatLine;
        private Tower _hunted;
        private float _sinceSmash;
        private float _sinceChoose;
        private float _sinceBurn;
        private float _sinceClutch;
        private float _clutchWait;
        private int _clutches;
        private int _mostTowers;

        public HuntSignature(ContinentBossBrain brain)
        {
            _brain = brain;
            _aura = TowerAuraView.Create(brain.Field.Effects, "HuntedGlow");
            _brood = BroodKeeper.Create(brain.Field);
            _brood.Shot += () => _brain.Record.EggsShot++;
            _brood.Hatched += () => _brain.Record.EggsHatched++;
        }

        private BossField Field => _brain.Field;
        private Enemy Enemy => _brain.Enemy;
        private EnemyView View => _brain.View;
        private BossDefinition Boss => _brain.Boss;
        private HuntPower Hunt => _brain.Boss.Hunt;
        private Vector3 Position => _brain.transform.position;

        /// <summary>The tower she is after right now, or null.</summary>
        public Tower Hunted => _hunted;

        /// <summary>Her trail of lava, for the tests.</summary>
        public LavaTrail Trail => _trail;

        public bool WantsOffStructure => false;

        public void Begin()
        {
            if (_trail == null || !Mathf.Approximately(_trail.Lifetime, Hunt.TrailSeconds))
            {
                _trail = new LavaTrail(Hunt.TrailSeconds, Hunt.TrailSpacing);
                if (_trailView != null)
                {
                    Object.Destroy(_trailView.gameObject);
                }

                _trailView = LavaTrailView.Create(Field.Effects, _trail, Hunt.TrailRadius);
                _trailView.ReachUnder(_brain.transform);
            }

            _trail.Clear();
            _trailView.Clear();
            _trailView.SetEmbers(true);
            _brood.Configure(Hunt.Hatchling, Hunt.HatchlingsPerEgg, Hunt.HatchSeconds);
            _unreachable.Clear();
            _hunted = null;
            _sinceSmash = Hunt.CooldownSeconds;
            _sinceChoose = ChooseEverySeconds;
            _sinceClutch = 0f;
            _clutches = 0;
            _clutchWait = 0f;
            _mostTowers = 0;
        }

        public IEnumerator Entrance() => null;

        public IEnumerator AtStructure(IStructure structure) => null;

        public void OnRoad(float deltaTime)
        {
        }

        public IEnumerator NextAct()
        {
            if (BroodDue())
            {
                return LayEggs();
            }

            _sinceChoose += Time.deltaTime;
            if (_sinceChoose < ChooseEverySeconds)
            {
                return null;
            }

            _sinceChoose = 0f;
            Tower target = ChooseTarget();
            return target != null ? HuntDown(target) : null;
        }

        public void Always(float deltaTime)
        {
            _sinceSmash += deltaTime;
            _sinceClutch += deltaTime;
            bool walking = !Enemy.IsConcealed && !_brain.OnStructure;
            if (walking)
            {
                _trail.Walk(Position - Enemy.Heading * Hunt.TrailBehind);
            }
            else
            {
                _trail.Lift();
            }

            _trail.Step(deltaTime);
            _sinceBurn += deltaTime;
            if (_sinceBurn >= BurnEverySeconds)
            {
                Burn(_sinceBurn);
                _sinceBurn = 0f;
            }
        }

        public void End()
        {
            _aura.Hide();
            _heatLine?.Fade();
            _heatLine = null;
            _trailView.SetEmbers(false);
            _hunted = null;
        }

        // ---------------------------------------------------------------- the hunt

        private Tower ChooseTarget()
        {
            Field.Refresh();
            _mostTowers = Mathf.Max(_mostTowers, Field.Towers.Count);
            if (!ColdHunt.MayHunt(_brain.Record.Smashed.Count, _mostTowers, _sinceSmash, Hunt.CooldownSeconds))
            {
                return null;
            }

            _candidates.Clear();
            _candidateTowers.Clear();
            foreach (Tower tower in Field.Towers)
            {
                _candidateTowers.Add(tower);
                _candidates.Add(new ColdHunt.Candidate(tower.Position, IsCold(tower.Definition), Huntable(tower)));
            }

            int pick = ColdHunt.Choose(Position, _candidates);
            return pick >= 0 ? _candidateTowers[pick] : null;
        }

        private bool IsCold(TowerDefinition definition)
        {
            _levelSlows.Clear();
            for (int i = 0; i <= definition.MaxLevelIndex; i++)
            {
                _levelSlows.Add(definition.GetLevel(i).SlowFactor);
            }

            return ColdHunt.IsCold(definition.SlowFactor, _levelSlows);
        }

        private bool Huntable(Tower tower) => tower != null && tower.IsAlive && !tower.IsUpset && !_unreachable.Contains(tower)
            && Field.Context.Structures.Contains(tower);

        // She smells it (the heat line, the warm glow), walks to it off the road (or swims there through the lava),
        // smashes it and goes back to the road.
        private IEnumerator HuntDown(Tower target)
        {
            _brain.State = "Hunting";
            _brain.Record.Hunts++;
            _hunted = target;
            Enemy.SetDirected(true);
            BossClip smash = BossClip.Of(View, ModelClipRole.Smash, Boss.MechanicClipRate);
            float tail = BodyLengthPerScale * _brain.Scale * BodyTailShare;
            Field.Refresh();
            if (!smash.Exists || !BossRoutes.StrikeSpot(Field, target.Position, smash.Anchor, tail, _brain.BodyHalfWidth, Position, out Vector3 spot, out _))
            {
                _unreachable.Add(target);
                yield return GiveUp();
                yield break;
            }

            yield return Smell(target);
            if (_brain.TryPlanDiveTowards(spot, Hunt.DiveGain, out ContinentBossBrain.DivePlan dive))
            {
                _trail.Lift();
                yield return _brain.Dive(dive, false);
                Field.Refresh();
                if (!BossRoutes.StrikeSpot(Field, target.Position, smash.Anchor, tail, _brain.BodyHalfWidth, Position, out spot, out _))
                {
                    _unreachable.Add(target);
                    yield return GiveUp();
                    yield break;
                }
            }

            bool arrived = false;
            View.UseMoveCycle(ModelClipRole.HuntMove);
            Enemy.SetPace(Hunt.HuntPace);
            yield return _brain.WalkTo(Field.Grid.WorldToCell(spot), true, () => !Huntable(target), value => arrived = value, "Hunting");
            View.UseMoveCycle(ModelClipRole.Move);
            Enemy.SetPace(1f);
            if (!arrived || !Huntable(target))
            {
                if (!arrived && Huntable(target))
                {
                    _unreachable.Add(target);
                }

                yield return GiveUp();
                yield break;
            }

            yield return Smash(target, smash);
            yield return GiveUp();
        }

        // "She has smelled you": she rears and roars while a thin heat line runs to the tower and it starts to glow warm.
        private IEnumerator Smell(Tower target)
        {
            _brain.State = "Smelling";
            _aura.Show(target.Position, Warm, AuraIntensity, false);
            _heatLine?.Fade();
            var points = new List<Vector3> { Position, target.Position };
            _heatLine = CrackLineView.Create(Field.Effects, Field.Draped(points), CrackStyle.Heat);
            BossClip roar = BossClip.Of(View, ModelClipRole.Roar, Boss.MechanicClipRate);
            Enemy.Place(Position, BossGeometry.Flat(target.Position - Position));
            if (roar.Exists)
            {
                View.PlayScripted(ModelClipRole.Roar, roar.Rate);
            }

            float seconds = Mathf.Max(Hunt.HeatLineSeconds, roar.Seconds);
            float elapsed = 0f;
            while (elapsed < seconds && _brain.Alive)
            {
                elapsed += Time.deltaTime;
                _heatLine.OpenTo(_heatLine.Length * Mathf.Clamp01(elapsed / Hunt.HeatLineSeconds));
                yield return null;
            }

            View.ReleaseScript();
            // The thread of heat has told the player; it cools while she sets off.
            _heatLine.Fade();
            _heatLine = null;
            _brain.State = "Hunting";
        }

        // Facing the tower, the blow lands at the clip's moment: the tower becomes a wreck (a rebuild brings it back).
        private IEnumerator Smash(Tower target, BossClip smash)
        {
            _brain.State = "Smashing";
            Enemy.Place(_brain.OnGround(Position), BossGeometry.Flat(target.Position - Position));
            View.PlayScripted(ModelClipRole.Smash, smash.Rate, true);
            yield return _brain.Wait(smash.Moment);
            if (_brain.Alive && Huntable(target))
            {
                int towers = Mathf.Max(1, Field.Towers.Count);
                target.Smash(Position);
                _brain.Record.Smashed.Add(target);
                _brain.Record.MostFieldShare = Mathf.Max(_brain.Record.MostFieldShare, _brain.Record.Smashed.Count / (float)Mathf.Max(towers, _mostTowers));
                _brain.Impact(target.Position, SmashBurstRadius, Cinders);
                Field.Context.BossImpact.Raise(Boss.ImpactKick);
                _sinceSmash = 0f;
            }

            yield return _brain.Wait(smash.Seconds - smash.Moment);
            View.ReleaseScript();
        }

        private IEnumerator GiveUp()
        {
            _aura.Hide();
            _heatLine?.Fade();
            _heatLine = null;
            _hunted = null;
            View.UseMoveCycle(ModelClipRole.Move);
            Enemy.SetPace(1f);
            yield return _brain.BackToRoad();
        }

        // ---------------------------------------------------------------- the trail

        // Every live patch burns what stands on it: structures hard, the walkers less (they are her brood's kin).
        private void Burn(float seconds)
        {
            float structureBurn = Hunt.TowerBurn * Field.Context.Difficulty.StructureDamageMultiplier * seconds;
            StructureRegistry structures = Field.Context.Structures;
            for (int i = 0; i < structures.Count; i++)
            {
                IStructure structure = structures[i];
                if (structure.IsAlive && !structure.IsBase && _trail.Touches(structure.Position, Hunt.TrailRadius))
                {
                    structure.TakeDamage(structureBurn);
                }
            }

            if (Field.Spawner == null)
            {
                return;
            }

            float walkerBurn = Hunt.WalkerBurn * seconds;
            IReadOnlyList<Enemy> living = Field.Spawner.Living;
            for (int i = living.Count - 1; i >= 0; i--)
            {
                Enemy walker = living[i];
                if (walker != Enemy && walker.IsAlive && !walker.Definition.Flies && walker.Definition != Hunt.Egg
                    && _trail.Touches(walker.Position, Hunt.TrailRadius))
                {
                    walker.TakeDamage(walkerBurn);
                }
            }
        }

        // ---------------------------------------------------------------- the brood

        private bool BroodDue() =>
            Hunt.Egg != null && _clutches < Hunt.Clutches && Boss.PhaseAt(Enemy.Health.Fraction) >= Hunt.BroodPhase
            && _sinceClutch >= _clutchWait && Field.Spawner != null;

        // To the bank of the nearest pool, tail over the liquid, and the eggs drop from her sac one by one.
        private IEnumerator LayEggs()
        {
            _sinceClutch = 0f;
            // A trip that finds no bank or no way is tried again soon; only a laid clutch counts.
            _clutchWait = ClutchRetrySeconds;
            _brain.State = "Brooding";
            Enemy.SetDirected(true);
            Field.Refresh();
            BossClip lay = BossClip.Of(View, ModelClipRole.LayEggs, Boss.MechanicClipRate);
            if (!lay.Exists || !NearestBank(out TerrainKind kind, out Vector3 spot, out Vector3 facing, out Vector3 over))
            {
                yield return _brain.BackToRoad();
                yield break;
            }

            bool arrived = false;
            yield return _brain.WalkTo(Field.Grid.WorldToCell(spot), true, null, value => arrived = value, "Brooding");
            if (!arrived || !_brain.Alive)
            {
                yield return _brain.BackToRoad();
                yield break;
            }

            _clutches++;
            _clutchWait = Hunt.ClutchInterval;
            _brain.State = "LayingEggs";
            Enemy.Place(_brain.OnGround(Position), facing);
            View.PlayScripted(ModelClipRole.LayEggs, lay.Rate, true);
            float surface = TerrainLiquidBuilder.SurfaceLevel(kind);
            int eggs = Mathf.Max(1, Hunt.EggsPerClutch);
            float waited = 0f;
            for (int i = 0; i < eggs && _brain.Alive; i++)
            {
                float at = eggs > 1 ? Mathf.Lerp(lay.Cue, lay.Moment, i / (float)(eggs - 1)) : lay.Moment;
                yield return _brain.Wait(at - waited);
                waited = at;
                Vector3 drop = EggPoint(over, facing, i, eggs);
                if (_brood.Lay(Hunt.Egg, new Vector3(drop.x, surface, drop.z), facing) != null)
                {
                    _brain.Record.EggsLaid++;
                    _brain.Impact(drop, 0.5f, Cinders, false);
                }
            }

            yield return _brain.Wait(lay.Seconds - waited);
            View.ReleaseScript();
            yield return _brain.BackToRoad();
        }

        // The eggs spread a little across the pool behind her, each on a liquid cell.
        private Vector3 EggPoint(Vector3 over, Vector3 facing, int index, int count)
        {
            Vector3 across = Vector3.Cross(Vector3.up, facing);
            float side = count > 1 ? (index / (float)(count - 1) - 0.5f) * 2f * EggJitter * 2f : 0f;
            Vector3 point = over + across * side - facing * EggJitter;
            return Field.Climb.KindAt(point) == SurfaceKind.Liquid ? point : over;
        }

        // The nearest bank of any of her pools where she can stand tail over the liquid.
        private bool NearestBank(out TerrainKind kind, out Vector3 spot, out Vector3 facing, out Vector3 over)
        {
            kind = TerrainKind.Lava;
            spot = facing = over = Vector3.zero;
            float bestDistance = float.PositiveInfinity;
            foreach (TerrainKind candidate in ContinentBossBrain.PoolKindsOf(Boss.Dive.Pools))
            {
                for (int p = 0; p < Field.Pools.Count(candidate); p++)
                {
                    IReadOnlyList<Vector2Int> pool = Field.Pools.Pool(candidate, p);
                    if (!BossRoutes.BankSpot(Field, pool, Hunt.SacBehind, _brain.BodyHalfWidth, Position, out Vector3 stand, out Vector3 away, out Vector3 liquid))
                    {
                        continue;
                    }

                    float distance = BossGeometry.FlatDistance(stand, Position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        kind = candidate;
                        spot = stand;
                        facing = away;
                        over = liquid;
                    }
                }
            }

            return !float.IsPositiveInfinity(bestDistance);
        }
    }
}
