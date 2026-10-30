using Game.Combat;
using Game.Enemies;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// A creature living in the menu scene: the real <see cref="Enemy"/> and <see cref="EnemyView"/>, so it walks,
    /// breathes, rolls and climbs out exactly as in a level, driven along a route of its own. At the end of the
    /// route it sinks away (into the pool, the hive mouth or the ground), waits a while and comes up again.
    /// </summary>
    public sealed class MenuCreature : MonoBehaviour
    {
        private const float SinkSeconds = 0.9f;
        private const float SinkDepth = 1.4f;

        private Enemy _enemy;
        private EnemyDefinition _definition;
        private Vector3[] _route;
        private float _speed;
        private float _height;
        private EnemyOrigin _origin;
        private float _restMin;
        private float _restMax;
        private float _sinking = float.NegativeInfinity;
        private float _rest = float.NegativeInfinity;
        private Vector3 _sinkFrom;
        private TargetRegistry _targets;
        private StructureRegistry _structures;

        /// <summary>
        /// Builds a creature that follows <paramref name="route"/> at <paramref name="speed"/> times its usual pace,
        /// comes up the way its <paramref name="origin"/> dictates and, after sinking at the end, rests between
        /// <paramref name="restMin"/> and <paramref name="restMax"/> seconds. A speed of zero makes it stand and watch.
        /// </summary>
        public static MenuCreature Create(Transform parent, EnemyDefinition definition, Vector3[] route, float speed, float height, EnemyOrigin origin, float restMin, float restMax, TargetRegistry targets, StructureRegistry structures)
        {
            var go = new GameObject(definition.name);
            go.transform.SetParent(parent, false);
            var creature = go.AddComponent<MenuCreature>();
            creature._enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyView>().HideHealthBar();
            creature._definition = definition;
            creature._route = route;
            creature._speed = speed;
            creature._height = height;
            creature._origin = origin;
            creature._restMin = restMin;
            creature._restMax = restMax;
            creature._targets = targets;
            creature._structures = structures;
            creature._enemy.ReachedEnd += creature.OnReachedEnd;
            return creature;
        }

        /// <summary>Puts the creature at the start of its route after a delay, so the scene does not fill all at once.</summary>
        public void Emerge(float delay)
        {
            _rest = Time.time + delay;
            SetVisible(false);
        }

        private void Spawn()
        {
            SetVisible(true);
            _enemy.Initialize(_definition, _route, 1f, 1f, _height, _targets, _structures, _speed, _origin);
        }

        private void OnReachedEnd(Enemy enemy)
        {
            if (_sinking > float.NegativeInfinity)
            {
                return;
            }

            enemy.Deactivate();
            _sinking = Time.time;
            _sinkFrom = transform.position;
        }

        private void Update()
        {
            if (_sinking > float.NegativeInfinity)
            {
                float t = (Time.time - _sinking) / SinkSeconds;
                transform.position = _sinkFrom - Vector3.up * (SinkDepth * t * t);
                if (t >= 1f)
                {
                    _sinking = float.NegativeInfinity;
                    _rest = Time.time + Random.Range(_restMin, _restMax);
                    SetVisible(false);
                }

                return;
            }

            if (_rest > float.NegativeInfinity && Time.time >= _rest)
            {
                _rest = float.NegativeInfinity;
                Spawn();
            }
        }

        // The Enemy and its view stay enabled; only the renderers go, so the view keeps its rig between lives.
        private void SetVisible(bool visible)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = visible;
            }
        }
    }
}
