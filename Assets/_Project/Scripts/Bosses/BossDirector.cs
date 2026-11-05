using Game.Economy;
using Game.Enemies;
using Game.Levels;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Hands every continent boss that enters the level its brain (a pooled boss keeps the one it got), builds the field
    /// the bosses share once, and remembers each boss the player meets so the Codex opens its entry. Listens to the
    /// level's spawn event only; nothing in the level knows the bosses are run from here.
    /// </summary>
    public sealed class BossDirector : MonoBehaviour
    {
        private LevelContext _context;
        private ProgressService _progress;
        private BossField _field;
        private EnvironmentInstancer _decor;
        private ColonyRigView _rig;
        private EnemySpawner _spawner;

        public void Initialize(LevelContext context, EnemySpawner spawner, ProgressService progress)
        {
            _context = context;
            _spawner = spawner;
            _progress = progress;
            // The environment and the rig are built before the gameplay; they are looked up once, here.
            _decor = FindAnyObjectByType<EnvironmentInstancer>();
            _rig = FindAnyObjectByType<ColonyRigView>();
            context.EnemySpawned.Subscribe(OnSpawned);
        }

        private void OnDestroy() => _context?.EnemySpawned.Unsubscribe(OnSpawned);

        private void OnSpawned(Enemy enemy)
        {
            EnemyDefinition definition = enemy.Definition;
            if (definition.IsBoss && !_context.Practice)
            {
                _progress?.MarkEnemySeen(definition.name);
            }

            if (definition.Boss == null)
            {
                return;
            }

            var brain = enemy.GetComponent<ContinentBossBrain>();
            if (brain == null)
            {
                brain = enemy.gameObject.AddComponent<ContinentBossBrain>();
                brain.Initialize(Field());
            }

            // A boss with an entrance of its own (a crack to the rig, a pool) rises its own way, not out of the spawn.
            if (definition.Boss.Arrival != BossArrival.Wave)
            {
                enemy.GetComponent<EnemyView>()?.SkipSpawn();
            }

            brain.Begin();
        }

        private BossField Field()
        {
            if (_field == null)
            {
                var effects = new GameObject("BossEffects").transform;
                effects.SetParent(transform, false);
                _field = new BossField(_context, _decor != null ? _decor.Formations : null, _decor, _rig, _spawner, effects);
            }

            return _field;
        }
    }
}
