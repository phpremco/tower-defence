using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Towers;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Overgrowth (Rotbloom, Docs/BOSSES.md): every few seconds it opens its petals (a sweet rising sound, the target pads
    /// glow green) and fires a volley of spore pods at the fullest cluster of towers within reach: a few pads at most, and
    /// never more than about a quarter of the towers overgrown at once. A pod is a small flyer the air towers can shoot
    /// down; one that lands overgrows its pad: nothing can be built there and the tower on it fires at a share of its rate
    /// while vines grow over it, until a blast on the pad or the hero's repair burns them off. Rooted, it creeps along the
    /// road on growing roots; below its uproot phase it tears itself out and walks, leaving rot behind it. Its bog dives
    /// are the shared body's.
    /// </summary>
    public sealed class OvergrowthSignature : IBossSignature
    {
        private const float HazeIntensity = 3.2f;
        // The spore sheet: about 2.5 m on the pad, its pivot a fifth up the frame (README round 29).
        private const float SporeCloudSize = 2.5f;
        private const float SporeCloudLift = 0.75f;
        private const float RetrySeconds = 1f;
        private const float BurstRadius = 0.9f;
        private const float RootsAhead = 2.4f;
        private const int CreepRoots = 4;
        private const float RotRadius = 0.8f;
        private const float RotSpacing = 0.8f;
        private const float RotBehind = 1.4f;
        private const float UprootKick = 0.6f;
        private static readonly Color Haze = new Color(0.55f, 1f, 0.3f);
        private static readonly Color Spores = new Color(0.6f, 1f, 0.35f);
        private static readonly Color BogSplash = new Color(0.45f, 0.6f, 0.25f);
        private static readonly Color Rot = new Color(0.35f, 0.75f, 0.12f) * 1.4f;
        private static readonly Color RotSpores = new Color(0.55f, 0.9f, 0.3f, 0.8f);

        private struct Flight
        {
            public Enemy Pod;
            public int Id;
            public Vector3 From;
            public Vector3 To;
            public float Elapsed;
            public float Seconds;
            public TowerAuraView Haze;
        }

        private readonly ContinentBossBrain _brain;
        private readonly OvergrowthPads _pads = new OvergrowthPads();
        private readonly SporeVolley _volley = new SporeVolley();
        private readonly List<Flight> _flights = new List<Flight>(4);
        private readonly List<TowerAuraView> _hazes = new List<TowerAuraView>(4);
        private readonly List<VinesView> _vines = new List<VinesView>(8);
        private readonly Dictionary<Vector2Int, VinesView> _padVines = new Dictionary<Vector2Int, VinesView>();
        private readonly Dictionary<Vector2Int, Tower> _padTowers = new Dictionary<Vector2Int, Tower>();
        private readonly List<Vector2Int> _cleaned = new List<Vector2Int>(4);
        private readonly List<Vector3> _towerPositions = new List<Vector3>(32);
        private readonly List<bool> _eligible = new List<bool>(32);
        private readonly List<int> _picked = new List<int>(4);
        private readonly List<Tower> _targets = new List<Tower>(4);
        private readonly RootsView[] _creepRoots = new RootsView[CreepRoots];
        private readonly LavaTrail _rot;
        private readonly LavaTrailView _rotView;
        private readonly FlipbookFx _spores;
        private float _sinceVolley;
        private bool _uprooted;
        private float _crept;
        private int _nextRoot;
        private Vector3 _lastPosition;

        public OvergrowthSignature(ContinentBossBrain brain)
        {
            _brain = brain;
            _rot = new LavaTrail(Power.RotSeconds, RotSpacing);
            _rotView = LavaTrailView.Create(brain.Field.Effects, _rot, RotRadius, Rot, RotSpores, "RotTrail");
            _spores = FlipbookFx.Create(brain.Field.Effects, "SporeCloud", BossFxArt.Sheet.SporeCloud, false);
            for (int i = 0; i < CreepRoots; i++)
            {
                _creepRoots[i] = RootsView.Create(brain.Field.Effects, Power.Roots, Power.RootStages, "CreepRoots");
            }

            brain.Field.Context.Projectiles.Exploded += OnExploded;
        }

        private BossField Field => _brain.Field;
        private Enemy Enemy => _brain.Enemy;
        private EnemyView View => _brain.View;
        private BossDefinition Boss => _brain.Boss;
        private OvergrowthPower Power => _brain.Boss.Overgrowth;
        private Vector3 Position => _brain.transform.position;

        /// <summary>The overgrown pads, for the tests.</summary>
        public OvergrowthPads Pads => _pads;

        /// <summary>The pods of its volleys, for the tests.</summary>
        public SporeVolley Volley => _volley;

        /// <summary>The towers standing overgrown now.</summary>
        public int OvergrownTowers => _padTowers.Count;

        public bool WantsOffStructure => false;

        public void Begin()
        {
            ClearField();
            _volley.Clear();
            _rot.Clear();
            _rotView.Clear();
            _uprooted = false;
            // The first volley comes sooner than the rest: it shows the player at once what the flower does.
            _sinceVolley = Power.Interval * 0.5f;
            _crept = 0f;
            _lastPosition = Position;
            Enemy.SetPace(Power.RootedPace);
            View.UseMoveCycle(ModelClipRole.Rooted);
        }

        public IEnumerator Entrance() => null;

        public IEnumerator AtStructure(IStructure structure) => null;

        public IEnumerator NextAct()
        {
            if (!_uprooted && Boss.PhaseAt(Enemy.Health.Fraction) >= Power.UprootPhase)
            {
                return Uproot();
            }

            if (_sinceVolley < Power.Interval || Enemy.IsConcealed || _brain.OnStructure || Field.Spawner == null || Power.Pod == null)
            {
                return null;
            }

            if (!ChooseTargets())
            {
                _sinceVolley = Power.Interval - RetrySeconds;
                return null;
            }

            return Bloom();
        }

        // Rooted, fresh roots grow out ahead of it along the road as it creeps.
        public void OnRoad(float deltaTime)
        {
            Vector3 position = Position;
            float moved = BossGeometry.FlatDistance(position, _lastPosition);
            _lastPosition = position;
            if (_uprooted)
            {
                return;
            }

            _crept += moved;
            if (_crept < Power.RootSpacing)
            {
                return;
            }

            _crept = 0f;
            Vector3 ahead = position + Enemy.Heading * RootsAhead;
            _creepRoots[_nextRoot].Show(0, new Vector3(ahead.x, Field.GroundAt(ahead), ahead.z), Enemy.Heading);
            _nextRoot = (_nextRoot + 1) % CreepRoots;
        }

        public void Always(float deltaTime)
        {
            _sinceVolley += deltaTime;
            Fly(deltaTime);
            if (_uprooted && !Enemy.IsConcealed && !_brain.OnStructure)
            {
                Vector3 behind = Position - Enemy.Heading * RotBehind;
                _rot.Walk(new Vector3(behind.x, Field.GroundAt(behind), behind.z));
            }
            else
            {
                _rot.Lift();
            }

            _rot.Step(deltaTime);
        }

        // Its vines wither with it and its pods fall out of the air: what it laid on the field goes with it.
        public void End()
        {
            ClearField();
            _rotView.SetEmbers(false);
        }

        // ---------------------------------------------------------------- the volley

        // The fullest cluster within reach, never past a quarter of the towers overgrown with the pods already in the air.
        private bool ChooseTargets()
        {
            Field.Refresh();
            _targets.Clear();
            _towerPositions.Clear();
            _eligible.Clear();
            IReadOnlyList<Tower> towers = Field.Towers;
            for (int i = 0; i < towers.Count; i++)
            {
                Tower tower = towers[i];
                _towerPositions.Add(tower.Position);
                _eligible.Add(tower.IsAlive && !tower.IsUpset && !_pads.Contains(Field.Grid.WorldToCell(tower.Position)) && !Targeted(tower));
            }

            OvergrowthTargets.Choose(_towerPositions, _eligible, Position, Power.Reach, Power.ClusterRadius, Power.MaxPads, _padTowers.Count + _volley.Flying, _picked);
            foreach (int index in _picked)
            {
                _targets.Add(towers[index]);
            }

            return _targets.Count > 0;
        }

        private bool Targeted(Tower tower)
        {
            Vector2Int cell = Field.Grid.WorldToCell(tower.Position);
            foreach (Flight flight in _flights)
            {
                if (_volley.PadOf(flight.Id) == cell)
                {
                    return true;
                }
            }

            return false;
        }

        // The petals open with the sweet sound and the pads glow green; the pods leave the throat one by one, timed so they
        // land as the warning runs out.
        private IEnumerator Bloom()
        {
            _brain.State = "Blooming";
            _sinceVolley = 0f;
            _brain.Record.Volleys++;
            Enemy.SetDirected(true);
            Vector3 centre = Vector3.zero;
            foreach (Tower target in _targets)
            {
                centre += target.Position;
            }

            centre /= _targets.Count;
            Enemy.Place(_brain.OnGround(Position), BossGeometry.Flat(centre - Position));
            var hazes = new List<TowerAuraView>(_targets.Count);
            foreach (Tower target in _targets)
            {
                TowerAuraView haze = FreeHaze();
                haze.Show(target.Position, Haze, HazeIntensity, true);
                hazes.Add(haze);
            }

            Field.Context.BossBloomed.Raise(Position);
            BossClip clip = BossClip.Of(View, ModelClipRole.SporeVolley, Boss.MechanicClipRate);
            ModelClip art = View.ClipOf(ModelClipRole.SporeVolley);
            string socket = art != null ? art.Socket : string.Empty;
            if (clip.Exists)
            {
                View.PlayScripted(ModelClipRole.SporeVolley, clip.Rate, true);
            }

            float waited = 0f;
            int count = _targets.Count;
            for (int i = 0; i < count && _brain.Alive; i++)
            {
                float shotAt = clip.Exists ? (count > 1 ? Mathf.Lerp(clip.Cue, clip.Moment, i / (float)(count - 1)) : clip.Moment) : i * 0.2f;
                yield return _brain.Wait(shotAt - waited);
                waited = Mathf.Max(waited, shotAt);
                if (!_brain.Alive)
                {
                    break;
                }

                Vector3 pad = Field.Grid.CellToWorld(Field.Grid.WorldToCell(_targets[i].Position));
                pad.y = Field.GroundAt(pad);
                Launch(View.SocketPosition(socket), pad, Mathf.Max(Power.MinFlightSeconds, Power.WarnSeconds - waited), hazes[i]);
            }

            yield return _brain.Wait(clip.Seconds - waited);
            View.ReleaseScript();
            Enemy.SetDirected(false);
        }

        private void Launch(Vector3 from, Vector3 pad, float seconds, TowerAuraView haze)
        {
            Enemy pod = Field.Spawner.SpawnAt(Power.Pod, from);
            if (pod == null)
            {
                haze.Hide();
                return;
            }

            pod.SetDirected(true);
            pod.Place(from, BossGeometry.Flat(pad - from));
            pod.GetComponent<EnemyView>()?.SkipSpawn();
            pod.Killed += OnPodKilled;
            _flights.Add(new Flight
            {
                Pod = pod,
                Id = _volley.Launch(Field.Grid.WorldToCell(pad)),
                From = from,
                To = pad,
                Seconds = seconds,
                Haze = haze,
            });
        }

        // Each pod on its arc; one that gets there whole overgrows its pad.
        private void Fly(float deltaTime)
        {
            for (int i = _flights.Count - 1; i >= 0; i--)
            {
                Flight flight = _flights[i];
                flight.Elapsed += deltaTime;
                float t = flight.Elapsed / Mathf.Max(0.01f, flight.Seconds);
                Vector3 at = SporeVolley.Arc(flight.From, flight.To, Power.ArcHeight, t);
                if (flight.Pod.IsAlive)
                {
                    flight.Pod.Place(at, BossGeometry.Flat(flight.To - flight.From));
                }

                if (t < 1f)
                {
                    _flights[i] = flight;
                    continue;
                }

                _flights.RemoveAt(i);
                flight.Haze.Hide();
                flight.Pod.Killed -= OnPodKilled;
                if (flight.Pod.IsAlive && _volley.Land(flight.Id))
                {
                    _brain.Impact(flight.To, BurstRadius, Spores, false);
                    Overgrow(_volley.PadOf(flight.Id), flight.To);
                }

                Field.Spawner?.Withdraw(flight.Pod);
            }
        }

        private void OnPodKilled(Enemy pod)
        {
            pod.Killed -= OnPodKilled;
            for (int i = 0; i < _flights.Count; i++)
            {
                if (_flights[i].Pod != pod)
                {
                    continue;
                }

                _volley.Shoot(_flights[i].Id);
                _flights[i].Haze.Hide();
                _flights.RemoveAt(i);
                _brain.Record.SporesShot++;
                return;
            }
        }

        // ---------------------------------------------------------------- overgrown pads

        private void Overgrow(Vector2Int cell, Vector3 at)
        {
            if (!_pads.Add(cell, at))
            {
                return;
            }

            // The pod bursts on the pad: a cloud of spores falls, spreads and thins over it (README round 29).
            _spores?.Play(at + Vector3.up * SporeCloudLift, SporeCloudSize, 0f, Color.white);
            Field.Context.ClosedPads.Add(cell);
            Field.Refresh();
            foreach (Tower tower in Field.Towers)
            {
                if (tower.IsAlive && Field.Grid.WorldToCell(tower.Position) == cell)
                {
                    tower.Overgrow(Power.FireRate);
                    tower.OvergrowthChanged += OnOvergrowthChanged;
                    _padTowers[cell] = tower;
                    break;
                }
            }

            VinesView vines = FreeVines();
            vines.Grow(at, Power.GrowSeconds);
            _padVines[cell] = vines;
            _brain.Record.PadsOvergrown++;
            int towers = Mathf.Max(1, Field.Towers.Count);
            _brain.Record.MostFieldShare = Mathf.Max(_brain.Record.MostFieldShare, _padTowers.Count / (float)towers);
        }

        // A blast that reaches an overgrown pad burns it clean (BOSSES.md: splash damage cleans pads).
        private void OnExploded(Vector3 at, float radius, ProjectileStyle style)
        {
            if (_pads.Count == 0)
            {
                return;
            }

            _pads.Burn(at, radius, _cleaned);
            foreach (Vector2Int cell in _cleaned)
            {
                Clean(cell);
            }
        }

        // The hero's repair burned the vines off the tower it guards.
        private void OnOvergrowthChanged(Tower tower, bool overgrown)
        {
            if (overgrown)
            {
                return;
            }

            Vector2Int cell = Field.Grid.WorldToCell(tower.Position);
            if (_pads.Remove(cell))
            {
                Clean(cell);
            }
        }

        private void Clean(Vector2Int cell)
        {
            Field.Context.ClosedPads.Remove(cell);
            if (_padTowers.TryGetValue(cell, out Tower tower))
            {
                _padTowers.Remove(cell);
                if (tower != null)
                {
                    tower.OvergrowthChanged -= OnOvergrowthChanged;
                    tower.ClearOvergrowth();
                }
            }

            if (_padVines.TryGetValue(cell, out VinesView vines))
            {
                _padVines.Remove(cell);
                vines.BurnOff();
            }

            _brain.Record.PadsCleaned++;
        }

        private void ClearField()
        {
            for (int i = _flights.Count - 1; i >= 0; i--)
            {
                Flight flight = _flights[i];
                flight.Pod.Killed -= OnPodKilled;
                flight.Haze.Hide();
                if (flight.Pod.IsAlive)
                {
                    Field.Spawner?.Withdraw(flight.Pod);
                }
            }

            _flights.Clear();
            foreach (KeyValuePair<Vector2Int, Tower> pad in _padTowers)
            {
                if (pad.Value != null)
                {
                    pad.Value.OvergrowthChanged -= OnOvergrowthChanged;
                    pad.Value.ClearOvergrowth();
                }
            }

            foreach (Vector2Int cell in _pads.Cells)
            {
                Field.Context.ClosedPads.Remove(cell);
            }

            _padTowers.Clear();
            _pads.Clear();
            foreach (VinesView vines in _padVines.Values)
            {
                vines.BurnOff();
            }

            _padVines.Clear();
            foreach (TowerAuraView haze in _hazes)
            {
                haze.Hide();
            }

            foreach (RootsView roots in _creepRoots)
            {
                roots.Show(-1, Vector3.zero, Vector3.forward);
            }
        }

        private TowerAuraView FreeHaze()
        {
            foreach (TowerAuraView haze in _hazes)
            {
                if (!haze.gameObject.activeSelf)
                {
                    return haze;
                }
            }

            TowerAuraView made = TowerAuraView.Create(Field.Effects, "SporeHaze");
            _hazes.Add(made);
            return made;
        }

        private VinesView FreeVines()
        {
            foreach (VinesView vines in _vines)
            {
                if (!vines.Active)
                {
                    return vines;
                }
            }

            VinesView made = VinesView.Create(Field.Effects, Power.Vines);
            _vines.Add(made);
            return made;
        }

        // ---------------------------------------------------------------- uprooted

        // It tears its roots out (a roar at the clip's moment, one kick) and from now on walks, leaving rot behind it.
        private IEnumerator Uproot()
        {
            _brain.State = "Uprooting";
            _uprooted = true;
            _brain.Record.Uprooted = true;
            Enemy.SetDirected(true);
            BossClip clip = BossClip.Of(View, ModelClipRole.Uproot, Boss.MechanicClipRate);
            if (clip.Exists)
            {
                View.PlayScripted(ModelClipRole.Uproot, clip.Rate, true);
                yield return _brain.WaitBreaking(clip.Moment);
                _brain.Impact(Position, BurstRadius * 2f, BogSplash);
                Field.Context.BossImpact.Raise(Boss.ImpactKick * UprootKick);
                yield return _brain.WaitBreaking(clip.Seconds - clip.Moment);
                View.ReleaseScript();
            }

            foreach (RootsView roots in _creepRoots)
            {
                roots.Show(-1, Vector3.zero, Vector3.forward);
            }

            View.UseMoveCycle(ModelClipRole.Move);
            Enemy.SetPace(1f);
            _rotView.SetEmbers(true);
            Enemy.SetDirected(false);
        }
    }
}
