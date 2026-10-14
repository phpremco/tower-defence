using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Placeholder look of an enemy: a warm glowing body that turns towards its heading, two bright eyes,
    /// a health bar and (for flyers) a shadow on the ground.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyView : MonoBehaviour
    {
        private const float BarWidth = 0.9f;
        private const float BarHeight = 0.1f;
        private const float BarOffsetY = 1.0f;
        private const float BarEmission = 1.5f;
        private const float BodyEmission = 0.9f;
        private const float EyeEmission = 4f;
        private const float EyeSize = 0.14f;
        private const float ShadowSize = 0.6f;
        private const float ShadowHeight = 0.02f;
        private static readonly Color BarColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f);
        private static readonly Color EyeColor = new Color(1f, 0.95f, 0.6f);

        private Enemy _enemy;
        private Health _health;
        private Transform _body;
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
            if (!_built)
            {
                return;
            }

            if (_enemy.Heading.sqrMagnitude > 0.0001f)
            {
                _body.rotation = Quaternion.LookRotation(_enemy.Heading);
            }

            if (_shadow != null)
            {
                Vector3 position = _enemy.Position;
                _shadow.position = new Vector3(position.x, ShadowHeight, position.z);
            }
        }

        private void OnInitialized(Enemy enemy)
        {
            if (!_built)
            {
                Build(enemy.Definition);
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
            bool air = definition.Layer == TargetLayers.Air;
            float scale = definition.Scale;

            var body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            body.localPosition = new Vector3(0f, scale * 0.5f, 0f);
            _body = body;

            PrimitiveType shape = air ? PrimitiveType.Sphere : PrimitiveType.Capsule;
            GameObject main = PlaceholderVisuals.CreatePrimitive(shape, definition.Color, body, Vector3.zero, new Vector3(scale, scale * (air ? 1f : 0.6f), scale * 1.3f), BodyEmission);
            if (!air)
            {
                main.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            float eyeX = scale * 0.22f;
            float eyeZ = scale * 0.55f;
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, EyeColor, body, new Vector3(eyeX, scale * 0.1f, eyeZ), Vector3.one * EyeSize, EyeEmission);
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, EyeColor, body, new Vector3(-eyeX, scale * 0.1f, eyeZ), Vector3.one * EyeSize, EyeEmission);

            GameObject bar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, BarColor, transform, new Vector3(0f, BarOffsetY, 0f), new Vector3(BarWidth, BarHeight, BarHeight), BarEmission);
            _bar = bar.transform;

            if (air)
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
