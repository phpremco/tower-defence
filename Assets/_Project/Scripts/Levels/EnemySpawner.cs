using System;
using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Levels
{
    /// <summary>Creates pooled enemies, sends them down the right path and applies kill rewards and base damage.</summary>
    public sealed class EnemySpawner
    {
        private const float AirHeight = 1.6f;

        private readonly LevelContext _context;
        private readonly Transform _parent;
        private readonly Vector3[] _landPath;
        private readonly Vector3[] _airPath;
        private readonly Dictionary<EnemyDefinition, ObjectPool<Enemy>> _pools = new Dictionary<EnemyDefinition, ObjectPool<Enemy>>();

        public EnemySpawner(LevelContext context, Transform parent)
        {
            _context = context;
            _parent = parent;
            GridMap grid = context.Level.Grid;
            _landPath = ToWorld(context.Level.LandPath, grid);
            _airPath = ToWorld(context.Level.AirPath, grid);
        }

        public int Alive { get; private set; }

        public void Spawn(EnemyDefinition definition)
        {
            Enemy enemy = PoolFor(definition).Get();
            bool air = definition.Layer == TargetLayers.Air;
            DifficultyProfile difficulty = _context.Difficulty;

            enemy.Initialize(
                definition,
                air ? _airPath : _landPath,
                difficulty.HealthMultiplier,
                difficulty.RewardMultiplier,
                air ? AirHeight : 0f,
                _context.Targets);
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
            Release(enemy);
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
