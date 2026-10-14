using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>Placeholder look of an enemy: a coloured shape, a health bar and (for flyers) a ground shadow.</summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyView : MonoBehaviour
    {
        private const float BarWidth = 0.9f;
        private const float BarHeight = 0.1f;
        private const float BarOffsetY = 0.9f;
        private const float ShadowSize = 0.6f;
        private static readonly Color BarColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f);

        private Enemy _enemy;
        private Health _health;
        private Transform _bar;
        private Transform _shadow;
        private bool _built;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _enemy.Initialized += OnInitialized;
        }

        private void OnDestroy()
        {
            _enemy.Initialized -= OnInitialized;
        }

        private void LateUpdate()
        {
            if (_shadow != null)
            {
                Vector3 position = _enemy.Position;
                _shadow.position = new Vector3(position.x, 0.02f, position.z);
            }
        }

        private void OnInitialized(Enemy enemy)
        {
            EnemyDefinition definition = enemy.Definition;
            if (!_built)
            {
                Build(definition);
                _built = true;
            }

            if (_health != null)
            {
                _health.Changed -= OnHealthChanged;
            }

            _health = enemy.Health;
            _health.Changed += OnHealthChanged;
            SetBar(1f);
        }

        private void Build(EnemyDefinition definition)
        {
            PrimitiveType shape = definition.Layer == TargetLayers.Air ? PrimitiveType.Sphere : PrimitiveType.Cube;
            float scale = definition.Scale;
            PlaceholderVisuals.CreatePrimitive(shape, definition.Color, transform, new Vector3(0f, scale * 0.5f, 0f), Vector3.one * scale);

            GameObject bar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, BarColor, transform, new Vector3(0f, BarOffsetY, 0f), new Vector3(BarWidth, BarHeight, BarHeight));
            _bar = bar.transform;

            if (definition.Layer == TargetLayers.Air)
            {
                GameObject shadow = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, ShadowColor, transform, Vector3.zero, new Vector3(ShadowSize, 0.01f, ShadowSize));
                _shadow = shadow.transform;
            }
        }

        private void OnHealthChanged(float _) => SetBar(_health.Fraction);

        private void SetBar(float fraction)
        {
            _bar.localScale = new Vector3(BarWidth * Mathf.Max(0.001f, fraction), BarHeight, BarHeight);
        }
    }
}

