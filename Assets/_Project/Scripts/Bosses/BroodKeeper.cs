using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The eggs a boss laid in the pools (Docs/BOSSES.md, Cinder Matriarch): each lies still where it was laid and ripens;
    /// shot in time it bursts and nothing comes of it; after its time it cracks open (its Hatch clip) and its brood climbs
    /// out and makes for the road. Lives on the field, apart from the boss, so eggs laid before the boss fell still hatch.
    /// </summary>
    public sealed class BroodKeeper : MonoBehaviour
    {
        private const float BroodSpread = 0.35f;

        private readonly List<Enemy> _hatched = new List<Enemy>(4);
        private readonly List<Enemy> _cracking = new List<Enemy>(4);
        private readonly List<float> _crackLeft = new List<float>(4);
        private BossField _field;
        private EggClutch<Enemy> _clutch;
        private EnemyDefinition _hatchling;
        private int _perEgg = 1;

        /// <summary>Eggs still in the pools, not yet cracking.</summary>
        public int Waiting => _clutch != null ? _clutch.Count : 0;

        /// <summary>Raised when an egg was shot before it hatched.</summary>
        public event System.Action Shot;

        /// <summary>Raised when an egg hatched its brood.</summary>
        public event System.Action Hatched;

        public static BroodKeeper Create(BossField field)
        {
            var go = new GameObject("Brood");
            go.transform.SetParent(field.Effects, false);
            var keeper = go.AddComponent<BroodKeeper>();
            keeper._field = field;
            return keeper;
        }

        /// <summary>Sets what hatches and when (a boss's data); keeps the eggs already laid.</summary>
        public void Configure(EnemyDefinition hatchling, int perEgg, float hatchSeconds)
        {
            _hatchling = hatchling;
            _perEgg = Mathf.Max(1, perEgg);
            if (_clutch == null || !Mathf.Approximately(_clutch.HatchSeconds, hatchSeconds))
            {
                _clutch = new EggClutch<Enemy>(hatchSeconds);
            }
        }

        /// <summary>Puts an egg on the field, held still at a point (its pivot on the liquid surface).</summary>
        public Enemy Lay(EnemyDefinition egg, Vector3 at, Vector3 facing)
        {
            Enemy laid = _field.Spawner != null ? _field.Spawner.SpawnAt(egg, at) : null;
            if (laid == null)
            {
                return null;
            }

            laid.SetDirected(true);
            laid.Place(at, facing);
            laid.GetComponent<EnemyView>()?.SkipSpawn();
            laid.Killed += OnEggKilled;
            _clutch.Lay(laid);
            return laid;
        }

        private void OnEggKilled(Enemy egg)
        {
            egg.Killed -= OnEggKilled;
            int cracking = _cracking.IndexOf(egg);
            if (cracking >= 0)
            {
                _cracking.RemoveAt(cracking);
                _crackLeft.RemoveAt(cracking);
            }

            if (_clutch.Remove(egg) || cracking >= 0)
            {
                Shot?.Invoke();
            }
        }

        private void Update()
        {
            if (_clutch == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            _clutch.Step(dt, _hatched);
            foreach (Enemy egg in _hatched)
            {
                if (egg != null && egg.IsAlive)
                {
                    StartCracking(egg);
                }
            }

            for (int i = _cracking.Count - 1; i >= 0; i--)
            {
                _crackLeft[i] -= dt;
                if (_crackLeft[i] > 0f)
                {
                    continue;
                }

                Enemy egg = _cracking[i];
                _cracking.RemoveAt(i);
                _crackLeft.RemoveAt(i);
                Release(egg);
            }
        }

        // The shell splits (the Hatch clip); the brood leaves at the clip's moment.
        private void StartCracking(Enemy egg)
        {
            var view = egg.GetComponent<EnemyView>();
            ModelClip hatch = view != null ? view.ClipOf(ModelClipRole.Hatch) : null;
            view?.PlayScripted(ModelClipRole.Hatch, 1f, true);
            _cracking.Add(egg);
            _crackLeft.Add(hatch != null ? hatch.EventSeconds : 0f);
        }

        private void Release(Enemy egg)
        {
            egg.Killed -= OnEggKilled;
            if (!egg.IsAlive)
            {
                return;
            }

            Vector3 at = egg.Position;
            for (int i = 0; i < _perEgg; i++)
            {
                float side = (i - (_perEgg - 1) * 0.5f) * BroodSpread;
                _field.Spawner?.SpawnFromCrack(_hatchling, at + new Vector3(side, 0f, -side));
            }

            _field.Spawner?.Withdraw(egg);
            Hatched?.Invoke();
        }
    }
}
