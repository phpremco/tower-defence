using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Hero;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Levels
{
    /// <summary>
    /// Creates pooled enemies and sends them on their way: down the path their spawn group names, up out of a pool
    /// or an element nest towards the nearest road, or, for bosses, through the level's own entrance. Applies kill
    /// rewards and base damage, runs the slime trails and the pack rage, and answers a boss's rallying call. Every
    /// enemy gets its own lane on the road, pace and flying height, so a wave reads as a crowd. Flyers that strike
    /// gates get a detour over the gate nearest their route.
    /// </summary>
    public sealed class EnemySpawner
    {
        private const float AirHeight = 1.6f;
        private const float AirHeightVariation = 0.3f;
        private const float AirLateralCells = 0.7f;
        private const float DeathBurstRadius = 0.5f;
        private const float BossBurstRadius = 1.5f;
        private const float SpeedVariation = 0.12f;
        private const float MinionSpread = 0.4f;
        private const float SlimeRadiusCells = 0.55f;

        // How far from the centre line a walker may be: up to the inner face of the edge stones, less a small gap and the
        // room its own body takes (Remco 2026-11-05: the slug boss slid under the stones). Every extra cell of road width
        // adds a little less of spread, so the crowd stays off the very edge in the corners. A body whose data gives no
        // width is estimated from its scale.
        private const float KerbMargin = 0.05f;
        private const float BodyHalfWidthPerScale = 0.3f;
        // A ground body (a slug's foot) wider than the road narrows to fit between the stones, but never below this share.
        private const float MinBodyFit = 0.8f;
        private const float MaxLateralCells = 0.35f;
        private const float MaxLateralPerExtraCell = 0.35f;
        private const float MinLateral = 0.02f;

        private readonly LevelContext _context;
        private readonly Transform _parent;
        private readonly Vector3[][] _paths;
        private readonly float _kerb;
        private readonly float _maxLateral;
        private readonly int _firstFlightPath;
        private readonly System.Random _random;
        private readonly EnemyDeathEffectPool _deathEffects;
        private readonly LiquidPools _liquids;
        private readonly Vector3[] _gates;
        private readonly List<Enemy> _living = new List<Enemy>(128);
        private readonly List<ElementNest> _nestBuffer = new List<ElementNest>(8);
        private HeroTarget _heroTarget;
        private readonly Dictionary<EnemyDefinition, ObjectPool<Enemy>> _pools = new Dictionary<EnemyDefinition, ObjectPool<Enemy>>();

        /// <param name="random">The spawner's draws (nest choice, spread, pool cells); seeded for a reproducible run.</param>
        public EnemySpawner(LevelContext context, Transform parent, System.Random random = null)
        {
            _random = random ?? new System.Random();
            _context = context;
            _parent = parent;
            _deathEffects = new EnemyDeathEffectPool(parent);
            _liquids = new LiquidPools(context.Level);
            Slime = new SlimeField(_living, context.Structures, SlimeRadiusCells * context.Level.CellSize, context.Difficulty.StructureDamageMultiplier);

            RoadCorridor road = context.Level.Road;
            _paths = new Vector3[context.Level.Paths.Length][];
            _firstFlightPath = 0;
            for (int i = 0; i < _paths.Length; i++)
            {
                _paths[i] = road.Route(i);
                if (_firstFlightPath == 0 && context.Level.Paths[i].Flying)
                {
                    _firstFlightPath = i;
                }
            }

            _gates = new Vector3[context.Level.Blockades.Length];
            for (int i = 0; i < _gates.Length; i++)
            {
                _gates[i] = road.CentreOf(context.Level.Blockades[i].Cell);
            }

            int extraCells = road.Width - 1;
            _kerb = road.HalfWidthCells * context.Level.CellSize - KerbMargin;
            _maxLateral = (MaxLateralCells + extraCells * MaxLateralPerExtraCell) * context.Level.CellSize;
        }

        public int Alive { get; private set; }

        /// <summary>Every enemy on the field right now, in spawn order.</summary>
        public IReadOnlyList<Enemy> Living => _living;

        /// <summary>The slime the trail layers leave; the level view draws it.</summary>
        public SlimeField Slime { get; }

        /// <summary>Health scale of the wave under way, set by the controller; minions a boss releases inherit it.</summary>
        public float WaveHealthScale { get; set; } = 1f;

        /// <summary>Advances what the spawner runs between spawns: the slime on the road. Called once per frame.</summary>
        public void Step(float deltaTime)
        {
            Slime.SetHero(_context.Hero);
            Slime.Step(deltaTime);
        }

        /// <param name="nest">For a nest spawn, the index of the level's nest it comes out of; -1 picks one at random.</param>
        public void Spawn(EnemyDefinition definition, int pathIndex, SpawnSource source = SpawnSource.Gate, int nest = -1)
        {
            LevelDefinition level = _context.Level;
            if (definition.Boss != null && definition.Boss.Arrival == BossArrival.ColonyRig && TrySpawnAtRig(definition))
            {
                return;
            }

            // The wave says per group where a boss comes in; an entrance the level does not have falls back to the gate.
            if (source == SpawnSource.Entrance)
            {
                if (level.BossEntrance.Kind != EntranceKind.Gate)
                {
                    SpawnThroughEntrance(definition, level.BossEntrance);
                    return;
                }

                source = SpawnSource.Gate;
            }

            if (source == SpawnSource.Nest)
            {
                SpawnFromNest(definition, nest);
                return;
            }

            if (source != SpawnSource.Gate)
            {
                SpawnFromPool(definition, LiquidPools.KindOf(source), definition.IsBoss ? level.BossEntrance.Cell : level.MainRoad[0]);
                return;
            }

            bool air = definition.Flies;
            float cell = level.CellSize;
            float lateralRoom = air
                ? AirLateralCells * cell
                : Mathf.Clamp(_kerb - BodyHalf(definition), 0f, _maxLateral);
            float lateral = Range(-lateralRoom, lateralRoom);
            Vector3[] route = Mathf.Abs(lateral) > MinLateral ? PathOffset.Shift(_paths[pathIndex], lateral) : _paths[pathIndex];
            if (air && definition.GateStrikes > 0)
            {
                route = WithGateDetour(route);
            }

            Launch(definition, route, FlightHeight(definition), EnemyOrigin.Gate, 0f);
            _context.GateSpawned.Raise(pathIndex);
        }

        // A continent boss that answers the colony's drill comes up under the rig beside the field and makes for the road;
        // its own mechanic hides it until it breaks out. Without a rig it uses the entrance the wave names.
        private bool TrySpawnAtRig(EnemyDefinition definition)
        {
            var clearance = new StructureClearance(_context.Level);
            if (!clearance.HasColonyRig)
            {
                return false;
            }

            Vector3 start = clearance.ColonyRigPosition;
            Vector3[] route = OffRoadRoute.Build(_context.Level, start, out float progress);
            Launch(definition, route, 0f, EnemyOrigin.Ground, progress);
            return true;
        }

        // A boss that does not use the gate comes up where the level says: out of a pool or out of the ground.
        private void SpawnThroughEntrance(EnemyDefinition definition, BossEntrance entrance)
        {
            if (entrance.Kind == EntranceKind.Pool)
            {
                TerrainKind kind = _context.Level.Terrain.KindAt(entrance.Cell);
                SpawnFromPool(definition, TerrainMap.IsLiquid(kind) ? kind : TerrainKind.Water, entrance.Cell);
                return;
            }

            Vector3 start = _context.Level.Grid.CellToWorld(entrance.Cell);
            Vector3[] route = OffRoadRoute.Build(_context.Level, start, out float progress);
            Launch(definition, route, FlightHeight(definition), EnemyOrigin.Ground, progress);
        }

        // Out of the pool nearest the given cell, in open liquid, then straight for the nearest road.
        private void SpawnFromPool(EnemyDefinition definition, TerrainKind kind, Vector2Int near)
        {
            if (!_liquids.TryFindPool(kind, near, out IReadOnlyList<Vector2Int> pool))
            {
                // Without such a pool the enemy simply takes the main road, so a data slip never loses a wave.
                Spawn(definition, definition.Flies ? _firstFlightPath : 0);
                return;
            }

            Vector2Int cell = _liquids.SurfaceCell(pool, _random.Next());
            Vector3 start = _context.Level.Grid.CellToWorld(cell);
            Vector3[] route = OffRoadRoute.Build(_context.Level, start, out float progress);
            EnemyOrigin origin = kind == TerrainKind.Water ? EnemyOrigin.Water : kind == TerrainKind.Lava ? EnemyOrigin.Lava : kind == TerrainKind.Ice ? EnemyOrigin.Ice : kind == TerrainKind.Bog ? EnemyOrigin.Bog : kind == TerrainKind.Rift ? EnemyOrigin.Rift : EnemyOrigin.Quicksand;
            Launch(definition, route, FlightHeight(definition), origin, progress);
        }

        // Out of the given nest (drawn ahead, so its origin opens in time) or one of the level's nests of the creature's
        // element at random, then for the nearest road. A flyer still dives at the gate nearest its way when it strikes gates.
        private void SpawnFromNest(EnemyDefinition definition, int index)
        {
            ElementNest[] nests = _context.Level.Nests;
            ElementNest chosen;
            if (index >= 0 && index < nests.Length)
            {
                chosen = nests[index];
            }
            else
            {
                _nestBuffer.Clear();
                foreach (ElementNest nest in nests)
                {
                    if (nest.Element == definition.Element)
                    {
                        _nestBuffer.Add(nest);
                    }
                }

                if (_nestBuffer.Count == 0)
                {
                    Spawn(definition, definition.Flies ? _firstFlightPath : 0);
                    return;
                }

                chosen = _nestBuffer[_random.Next(_nestBuffer.Count)];
            }

            Vector3 start = _context.Level.Grid.CellToWorld(chosen.Cell);
            Vector3[] route = OffRoadRoute.Build(_context.Level, start, out float progress);
            if (definition.Flies && definition.GateStrikes > 0)
            {
                route = WithGateDetour(route);
            }

            Launch(definition, route, FlightHeight(definition), NestOrigin(chosen.Element), progress);
        }

        /// <summary>How a creature leaves a nest of its element: a stone nest sits in a rift, so it claws out of the crack.</summary>
        public static EnemyOrigin NestOrigin(Element element) =>
            LevelValidator.NestTerrain(element) == TerrainKind.Rift ? EnemyOrigin.Rift : EnemyOrigin.Nest;

        private float FlightHeight(EnemyDefinition definition) =>
            definition.Flies ? AirHeight + Range(-AirHeightVariation, AirHeightVariation) : 0f;

        // What a boss laser locks onto: the nearest tower or gate in reach, or the hero when they stand closer.
        private IStructure FindLaserTarget(Vector3 origin, float range)
        {
            IStructure best = _context.Structures.FindNearest(origin, range);
            Commander hero = _context.Hero;
            if (hero == null || hero.IsDown)
            {
                return best;
            }

            float heroSqr = (hero.transform.position - origin).sqrMagnitude;
            if (heroSqr > range * range || (best != null && (best.Position - origin).sqrMagnitude <= heroSqr))
            {
                return best;
            }

            _heroTarget ??= new HeroTarget(hero);
            return _heroTarget;
        }

        /// <summary>A creature clawing up out of a crack at a point on the field, then making for the nearest road.</summary>
        public void SpawnFromCrack(EnemyDefinition definition, Vector3 point)
        {
            if (definition == null)
            {
                return;
            }

            Vector3[] route = OffRoadRoute.Build(_context.Level, point, out float progress);
            Launch(definition, route, FlightHeight(definition), EnemyOrigin.Rift, progress);
        }

        /// <summary>
        /// Puts a creature on the field at a point and hands it back (a boss's egg in a pool): its route leads to the nearest
        /// road, but whoever placed it usually holds it there.
        /// </summary>
        public Enemy SpawnAt(EnemyDefinition definition, Vector3 point)
        {
            if (definition == null)
            {
                return null;
            }

            Vector3[] route = OffRoadRoute.Build(_context.Level, point, out float progress);
            route[0] = new Vector3(point.x, 0f, point.z);
            return Launch(definition, route, 0f, EnemyOrigin.Lava, progress);
        }

        /// <summary>
        /// Puts a creature on the field at a point and sends it along one of the level's paths from the point of that path
        /// nearest to it (a boss's shard taking its own route); a flyer at its flight height.
        /// </summary>
        public Enemy SpawnOnPath(EnemyDefinition definition, Vector3 point, int pathIndex)
        {
            if (definition == null || pathIndex < 0 || pathIndex >= _paths.Length)
            {
                return null;
            }

            Vector3[] route = RouteFrom(_paths[pathIndex], new Vector3(point.x, 0f, point.z), out float progress);
            return Launch(definition, route, FlightHeight(definition), EnemyOrigin.Gate, progress);
        }

        /// <summary>The world points of one of the level's paths, from its start to the base.</summary>
        public IReadOnlyList<Vector3> PathPoints(int pathIndex) => pathIndex >= 0 && pathIndex < _paths.Length ? _paths[pathIndex] : new Vector3[0];

        // From a point onto the nearest spot of a path and along the rest of it; the progress counts from the path's start.
        private static Vector3[] RouteFrom(Vector3[] path, Vector3 start, out float progress)
        {
            progress = 0f;
            if (path.Length < 2)
            {
                return new[] { start, path.Length > 0 ? path[0] : start };
            }

            int bestSegment = 0;
            Vector3 bestPoint = path[0];
            float bestSqr = float.MaxValue;
            for (int i = 1; i < path.Length; i++)
            {
                Vector3 a = new Vector3(path[i - 1].x, 0f, path[i - 1].z);
                Vector3 b = new Vector3(path[i].x, 0f, path[i].z);
                Vector3 ab = b - a;
                float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(start - a, ab) / ab.sqrMagnitude) : 0f;
                Vector3 on = a + ab * t;
                float sqr = (on - start).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestSegment = i;
                    bestPoint = on;
                }
            }

            var route = new List<Vector3>(path.Length - bestSegment + 2) { start, bestPoint };
            for (int i = bestSegment; i < path.Length; i++)
            {
                route.Add(path[i]);
            }

            for (int i = 1; i < bestSegment; i++)
            {
                progress += Vector3.Distance(path[i - 1], path[i]);
            }

            progress += Vector3.Distance(path[bestSegment - 1], bestPoint);
            return route.ToArray();
        }

        /// <summary>Takes a creature off the field without a reward or a hit on the base (an egg that hatched).</summary>
        public void Withdraw(Enemy enemy)
        {
            if (enemy != null && _living.Contains(enemy))
            {
                Release(enemy);
            }
        }

        /// <summary>Sends a creature out of a boss's body, down the rest of the boss's road.</summary>
        public void SpawnMinion(EnemyDefinition definition, Enemy boss)
        {
            if (definition == null || boss == null)
            {
                return;
            }

            Vector3[] route = boss.RemainingRoute();
            Vector3 scatter = new Vector3(Range(-MinionSpread, MinionSpread), 0f, Range(-MinionSpread, MinionSpread));
            route[0] += scatter;
            Launch(definition, route, FlightHeight(definition), EnemyOrigin.Gate, boss.PathProgress);
        }

        // A boss's call reaches everyone on the field but the boss itself.
        private void Rally(Enemy caller, float speedFactor, float seconds)
        {
            for (int i = 0; i < _living.Count; i++)
            {
                if (_living[i] != caller)
                {
                    _living[i].ApplyHaste(speedFactor, seconds);
                }
            }
        }

        private Enemy Launch(EnemyDefinition definition, Vector3[] route, float height, EnemyOrigin origin, float progress)
        {
            Enemy enemy = PoolFor(definition).Get();
            DifficultyProfile difficulty = _context.Difficulty;
            enemy.Initialize(
                definition,
                route,
                difficulty.HealthMultiplier * _context.Level.EnemyHealthScale * WaveHealthScale * (definition.Boss != null ? difficulty.BossHealthShare : 1f),
                1f,
                height,
                _context.Targets,
                _context.Structures,
                1f + Range(-SpeedVariation, SpeedVariation),
                origin,
                progress,
                difficulty.StructureDamageMultiplier);
            if (!definition.Flies && definition.GroundBody)
            {
                float half = BodyHalf(definition);
                enemy.FitBody(half > _kerb ? Mathf.Max(MinBodyFit, _kerb / half) : 1f);
            }

            Alive++;
            _living.Add(enemy);
            if (definition.IsBoss)
            {
                _context.BossesAlive++;
            }

            _context.EnemySpawned.Raise(enemy);
            return enemy;
        }

        // Half its body across its heading: its data (a continent boss's own), else an estimate from its scale.
        private float BodyHalf(EnemyDefinition definition) =>
            definition.BodyHalfWidth > 0f ? definition.BodyHalfWidth
            : definition.Boss != null ? definition.Boss.BodyHalfWidth
            : definition.Scale * BodyHalfWidthPerScale * _context.Level.CellSize;

        // The gate nearest the flight route becomes a waypoint, inserted before the first point past it, so the
        // flyer dives at the gate and carries on along its route afterwards.
        private Vector3[] WithGateDetour(Vector3[] route)
        {
            if (_gates.Length == 0 || route.Length < 2)
            {
                return route;
            }

            int bestSegment = -1;
            Vector3 bestGate = Vector3.zero;
            float bestSqr = float.MaxValue;
            for (int g = 0; g < _gates.Length; g++)
            {
                for (int i = 0; i < route.Length - 1; i++)
                {
                    float sqr = DistanceToSegmentSqr(_gates[g], route[i], route[i + 1]);
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        bestSegment = i;
                        bestGate = _gates[g];
                    }
                }
            }

            var result = new Vector3[route.Length + 1];
            for (int i = 0; i <= bestSegment; i++)
            {
                result[i] = route[i];
            }

            result[bestSegment + 1] = new Vector3(bestGate.x, 0f, bestGate.z);
            for (int i = bestSegment + 1; i < route.Length; i++)
            {
                result[i + 1] = route[i];
            }

            return result;
        }

        private static float DistanceToSegmentSqr(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 line = b - a;
            line.y = 0f;
            Vector3 toPoint = point - a;
            toPoint.y = 0f;
            float lengthSqr = line.sqrMagnitude;
            float t = lengthSqr > 0f ? Mathf.Clamp01(Vector3.Dot(toPoint, line) / lengthSqr) : 0f;
            Vector3 nearest = a + line * t;
            float dx = nearest.x - point.x;
            float dz = nearest.z - point.z;
            return dx * dx + dz * dz;
        }

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        private ObjectPool<Enemy> PoolFor(EnemyDefinition definition)
        {
            if (_pools.TryGetValue(definition, out ObjectPool<Enemy> pool))
            {
                return pool;
            }

            pool = new ObjectPool<Enemy>(() => CreateEnemy(definition), e => e.gameObject.SetActive(true), e => e.gameObject.SetActive(false));
            _pools[definition] = pool;
            return pool;
        }

        private Enemy CreateEnemy(EnemyDefinition definition)
        {
            var go = new GameObject("Enemy");
            go.transform.SetParent(_parent, false);
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyView>().Initialize(_deathEffects);
            if (definition.Abilities.Length > 0)
            {
                var abilities = go.AddComponent<BossAbilities>();
                abilities.Initialize(_context.Structures, SpawnMinion, FindLaserTarget, Rally);
                abilities.SetDamageMultiplier(_context.Difficulty.StructureDamageMultiplier);
                abilities.Pulsed += OnBossPulsed;
                abilities.LaserChanged += on => _context.BossLaserChanged.Raise(on);
                abilities.MinionsReleased += (centre, color) => _context.BossMinionsReleased.Raise(centre);
                abilities.Volleyed += (origin, points, color) => _context.BossVolleyed.Raise(origin);
                abilities.Rallied += (centre, color) => _context.BossRallied.Raise(centre);
                go.AddComponent<BossAbilityView>();
            }

            enemy.Killed += OnKilled;
            enemy.ReachedEnd += OnReachedEnd;
            enemy.Attacked += OnAttacked;
            enemy.TrailDropped += Slime.Add;
            return enemy;
        }

        private void OnBossPulsed(Vector3 centre, float radius, Color color)
        {
            _context.Projectiles.PlayBurst(centre, radius, _context.Projectiles.Vfx.BossPulse, color);
            _context.BossPulsed.Raise(centre);
        }

        private void OnAttacked(Vector3 position) => _context.StructureHit.Raise(position);

        private void OnKilled(Enemy enemy)
        {
            _context.Wallet.Earn(enemy.Reward);
            // A boss goes down in a chain of blasts; any other creature in a small burst.
            if (enemy.Definition.IsBoss)
            {
                _context.Projectiles.PlayBurst(enemy.Position, BossBurstRadius, _context.Projectiles.Vfx.BossDeath);
            }
            else
            {
                _context.Projectiles.PlayBurst(enemy.Position, DeathBurstRadius);
            }
            _context.EnemyKilled.Raise(enemy);
            _context.EnemyDefeated.Raise();
            Enrage(enemy);
            Release(enemy);
        }

        // A pack avenges its dead: every pack mate close enough breaks into a run.
        private void Enrage(Enemy fallen)
        {
            EnemyDefinition definition = fallen.Definition;
            if (!definition.Rages)
            {
                return;
            }

            float radiusSqr = definition.RageRadius * definition.RageRadius;
            for (int i = 0; i < _living.Count; i++)
            {
                Enemy mate = _living[i];
                if (mate == fallen || mate.Definition != definition || !mate.IsAlive)
                {
                    continue;
                }

                if ((mate.Position - fallen.Position).sqrMagnitude <= radiusSqr)
                {
                    mate.ApplyHaste(definition.RageSpeedMultiplier, definition.RageSeconds);
                }
            }
        }

        private void OnReachedEnd(Enemy enemy)
        {
            _context.BaseHealth.TakeDamage(enemy.Definition.BaseDamage);
            Release(enemy);
        }

        private void Release(Enemy enemy)
        {
            enemy.Deactivate();
            _pools[enemy.Definition].Release(enemy);
            _living.Remove(enemy);
            Alive--;
            if (enemy.Definition.IsBoss)
            {
                _context.BossesAlive = Mathf.Max(0, _context.BossesAlive - 1);
            }
        }
    }
}
