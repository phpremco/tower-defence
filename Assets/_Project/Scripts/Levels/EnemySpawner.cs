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
        private const float SpeedVariation = 0.12f;
        private const float MinionSpread = 0.4f;
        private const float SlimeRadiusCells = 0.55f;

        // How far from the centre line a walker may be on a one-cell road: up to the shoulder, less the room its own
        // body takes. Every extra cell of road width adds half a cell of shoulder and a little less of spread, so the
        // crowd stays off the very edge in the corners.
        private const float RoadEdgeCells = 0.55f;
        private const float RoadEdgePerExtraCell = 0.5f;
        private const float BodyHalfWidthPerScale = 0.3f;
        private const float MaxLateralCells = 0.35f;
        private const float MaxLateralPerExtraCell = 0.35f;
        private const float MinLateral = 0.02f;

        private readonly LevelContext _context;
        private readonly Transform _parent;
        private readonly Vector3[][] _paths;
        private readonly float _roadEdge;
        private readonly float _maxLateral;
        private readonly int _firstFlightPath;
        private readonly System.Random _random = new System.Random();
        private readonly EnemyDeathEffectPool _deathEffects;
        private readonly LiquidPools _liquids;
        private readonly Vector3[] _gates;
        private readonly List<Enemy> _living = new List<Enemy>(128);
        private readonly List<ElementNest> _nestBuffer = new List<ElementNest>(8);
        private HeroTarget _heroTarget;
        private readonly Dictionary<EnemyDefinition, ObjectPool<Enemy>> _pools = new Dictionary<EnemyDefinition, ObjectPool<Enemy>>();

        public EnemySpawner(LevelContext context, Transform parent)
        {
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
            _roadEdge = (RoadEdgeCells + extraCells * RoadEdgePerExtraCell) * context.Level.CellSize;
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

        public void Spawn(EnemyDefinition definition, int pathIndex, SpawnSource source = SpawnSource.Gate)
        {
            LevelDefinition level = _context.Level;
            if (definition.IsBoss && level.BossEntrance.Kind != EntranceKind.Gate)
            {
                SpawnThroughEntrance(definition, level.BossEntrance);
                return;
            }

            if (source == SpawnSource.Nest)
            {
                SpawnFromNest(definition);
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
                : Mathf.Clamp(_roadEdge - definition.Scale * BodyHalfWidthPerScale * cell, 0f, _maxLateral);
            float lateral = Range(-lateralRoom, lateralRoom);
            Vector3[] route = Mathf.Abs(lateral) > MinLateral ? PathOffset.Shift(_paths[pathIndex], lateral) : _paths[pathIndex];
            if (air && definition.GateStrikes > 0)
            {
                route = WithGateDetour(route);
            }

            Launch(definition, route, FlightHeight(definition), EnemyOrigin.Gate, 0f);
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
            EnemyOrigin origin = kind == TerrainKind.Water ? EnemyOrigin.Water : kind == TerrainKind.Lava ? EnemyOrigin.Lava : kind == TerrainKind.Ice ? EnemyOrigin.Ice : kind == TerrainKind.Bog ? EnemyOrigin.Bog : EnemyOrigin.Quicksand;
            Launch(definition, route, FlightHeight(definition), origin, progress);
        }

        // Out of one of the level's nests of the creature's element, chosen at random, then for the nearest road.
        // A flyer still dives at the gate nearest its way when it strikes gates.
        private void SpawnFromNest(EnemyDefinition definition)
        {
            _nestBuffer.Clear();
            foreach (ElementNest nest in _context.Level.Nests)
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

            ElementNest chosen = _nestBuffer[_random.Next(_nestBuffer.Count)];
            Vector3 start = _context.Level.Grid.CellToWorld(chosen.Cell);
            Vector3[] route = OffRoadRoute.Build(_context.Level, start, out float progress);
            if (definition.Flies && definition.GateStrikes > 0)
            {
                route = WithGateDetour(route);
            }

            Launch(definition, route, FlightHeight(definition), EnemyOrigin.Nest, progress);
        }

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

        private void Launch(EnemyDefinition definition, Vector3[] route, float height, EnemyOrigin origin, float progress)
        {
            Enemy enemy = PoolFor(definition).Get();
            DifficultyProfile difficulty = _context.Difficulty;
            enemy.Initialize(
                definition,
                route,
                difficulty.HealthMultiplier * _context.Level.EnemyHealthScale * WaveHealthScale,
                1f,
                height,
                _context.Targets,
                _context.Structures,
                1f + Range(-SpeedVariation, SpeedVariation),
                origin,
                progress,
                difficulty.StructureDamageMultiplier);
            Alive++;
            _living.Add(enemy);
            if (definition.IsBoss)
            {
                _context.BossesAlive++;
            }

            _context.EnemySpawned.Raise(enemy);
        }

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
            _context.Wallet.Add(enemy.Reward);
            _context.Projectiles.PlayBurst(enemy.Position, DeathBurstRadius);
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
