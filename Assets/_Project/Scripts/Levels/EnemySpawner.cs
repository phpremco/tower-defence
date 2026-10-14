using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Levels
{
    /// <summary>Creates pooled enemies, sends them down the path their spawn group names and applies kill rewards and base damage.</summary>
    public sealed class EnemySpawner
    {
        private const float AirHeight = 1.6f;
        private const float DeathBurstRadius = 0.5f;
        private const float LootShare = 0.5f;

        private readonly LevelContext _context;
        private readonly Transform _parent;
        private readonly Vector3[][] _paths;
        private readonly Dictionary<EnemyDefinition, ObjectPool<Enemy>> _pools = new Dictionary<EnemyDefinition, ObjectPool<Enemy>>();

        public EnemySpawner(LevelContext context, Transform parent)
        {
            _context = context;
            _parent = parent;

            GridMap grid = context.Level.Grid;
            EnemyPath[] paths = context.Level.Paths;
            _paths = new Vector3[paths.Length][];
            for (int i = 0; i < paths.Length; i++)
            {
                _paths[i] = ToWorld(paths[i].Waypoints, grid);
            }
        }

        public int Alive { get; private set; }

        public void Spawn(EnemyDefinition definition, int pathIndex)
        {
            Enemy enemy = PoolFor(definition).Get();
            bool air = definition.Layer == TargetLayers.Air;
            DifficultyProfile difficulty = _context.Difficulty;

            enemy.Initialize(
                definition,
                _paths[pathIndex],
                difficulty.HealthMultiplier * _context.Level.EnemyHealthMultiplier,
                1f,
                air ? AirHeight : 0f,
                _context.Targets,
                _context.Structures);
            Alive++;
        }

        private ObjectPool<Enemy> PoolFor(EnemyDefinition definition)
        {
            if (_pools.TryGetValue(definition, out ObjectPool<Enemy> pool))
            {
                return pool;
            }

            pool = new ObjectPool<Enemy>(CreateEnemy, e => e.gameObject.SetActive(true), e => e.gameObject.SetActive(false));
            _pools[definition] = pool;
            return pool;
        }

        private Enemy CreateEnemy()
        {
            var go = new GameObject("Enemy");
            go.transform.SetParent(_parent, false);
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyView>();
            enemy.Killed += OnKilled;
            enemy.ReachedEnd += OnReachedEnd;
            return enemy;
        }

        private void OnKilled(Enemy enemy)
        {
            _context.Wallet.Add(enemy.Reward);
            _context.Projectiles.Burst(enemy.Position, DeathBurstRadius);
            _context.EnemyDefeated.Raise();
            DropLoot(enemy);
            Release(enemy);
        }

        // With collector drones in the level a defeated enemy also leaves loot worth half its reward.
        private void DropLoot(Enemy enemy)
        {
            if (_context.Pickups != null && _context.GlobalModifiers.CollectorDroneCount > 0)
            {
                _context.Pickups.Drop(enemy.Position, Mathf.CeilToInt(enemy.Reward * LootShare));
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
            Alive--;
        }

        private static Vector3[] ToWorld(Vector2Int[] cells, GridMap grid)
        {
            var result = new Vector3[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                result[i] = grid.CellToWorld(cells[i]);
            }

            return result;
        }
    }
}
