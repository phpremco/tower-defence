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
        private const float ModelTintStrength = 0.65f;
        private const float ModelEmission = 0.2f;
        private const float EyeEmission = 4f;
        private const float EyeSize = 0.14f;
        private const float ShadowSize = 0.6f;
        private const float ShadowHeight = 0.02f;
        private const float BarMargin = 0.25f;
        private const float GaitPerUnit = 7f;
        private const float LegSwingDegrees = 28f;
        private const float LegLiftDegrees = 14f;
        private const float WingBeatSpeed = 22f;
        private const float WingBeatDegrees = 38f;
        private const float BobHeight = 0.05f;
        private const float HoverHeight = 0.04f;
        private const float HoverSpeed = 3f;
        private const float LungeSpeed = 7f;
        private const float LungeDistance = 1f;
        private const float StandStillDistance = 0.05f;
        private static readonly Color BarColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f);
        private static readonly Color EyeColor = new Color(1f, 0.95f, 0.6f);

        private Enemy _enemy;
        private Health _health;
        private Transform _body;
        private Transform _bar;
        private Transform _shadow;
        private bool _built;
        private Vector3 _lastPosition;
        private float _gait;
        private float _creatureScale = 1f;
        private bool _procedural;
        private Transform[] _legs;
        private float[] _legPhases;
        private Transform[] _wings;
        private float _clock;
        private float _barHeight;

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

            if (_procedural)
            {
                Animate();
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

            _lastPosition = enemy.Position;
            _health = enemy.Health;
            _health.Changed += OnHealthChanged;
            SetBar(1f);
        }

        // A procedural creature: a body that turns, legs that swing with the distance walked, wings that beat.
        private void BuildCreature(Transform body, EnemyDefinition definition)
        {
            _procedural = true;
            EnemyModelFactory.Parts parts = EnemyModelFactory.Create(definition.Shape);
            _creatureScale = definition.Scale * parts.ScaleFactor;
            body.localPosition = Vector3.zero;
            body.localScale = Vector3.one * _creatureScale;

            Material[] materials = EnemyModelFactory.CreateMaterials(definition.Color);

            var shell = new GameObject("Shell");
            shell.transform.SetParent(body, false);
            shell.AddComponent<MeshFilter>().sharedMesh = parts.Body;
            shell.AddComponent<MeshRenderer>().sharedMaterials = materials;

            _legs = new Transform[parts.Legs.Count];
            _legPhases = new float[parts.Legs.Count];
            for (int i = 0; i < _legs.Length; i++)
            {
                EnemyModelFactory.Leg leg = parts.Legs[i];
                _legs[i] = SpawnPart("Leg", body, parts.LegMesh, materials, leg.Hip, leg.Left);
                _legPhases[i] = leg.Phase;
            }

            _wings = new Transform[parts.WingHinges.Count];
            for (int i = 0; i < _wings.Length; i++)
            {
                _wings[i] = SpawnPart("Wing", body, parts.Wing, materials, parts.WingHinges[i], i == 1);
            }

            _barHeight = parts.Height * _creatureScale + BarMargin;
        }

        private static Transform SpawnPart(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 position, bool mirrored)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go.transform;
        }

        private void Animate()
        {
            _clock += Time.deltaTime;
            Vector3 position = _enemy.Position;
            float moved = (position - _lastPosition).magnitude;
            _lastPosition = position;
            bool walking = moved > StandStillDistance * Time.deltaTime;
            _gait += moved * GaitPerUnit / _creatureScale;

            for (int i = 0; i < _legs.Length; i++)
            {
                float swing = Mathf.Sin(_gait + _legPhases[i]);
                float lift = Mathf.Max(0f, Mathf.Cos(_gait + _legPhases[i]));
                _legs[i].localRotation = Quaternion.Euler(0f, swing * LegSwingDegrees, lift * LegLiftDegrees);
            }

            for (int i = 0; i < _wings.Length; i++)
            {
                float beat = Mathf.Sin(_clock * WingBeatSpeed + i * 0.4f) * WingBeatDegrees;
                _wings[i].localRotation = Quaternion.Euler(0f, 0f, i == 0 ? beat : -beat);
            }

            // Walking bounces the body; standing still (held at a blockade) makes it rear and lunge.
            float bob = walking ? Mathf.Abs(Mathf.Sin(_gait)) * BobHeight : Mathf.Sin(_clock * HoverSpeed) * HoverHeight;
            float lunge = walking ? 0f : Mathf.Max(0f, Mathf.Sin(_clock * LungeSpeed)) * LungeDistance;
            _body.localPosition = new Vector3(0f, bob * _creatureScale, 0f);
            _body.localScale = Vector3.one * (_creatureScale * (1f + (walking ? 0f : lunge * 0.1f)));
        }

        private void Build(EnemyDefinition definition)
        {
            bool air = definition.Layer == TargetLayers.Air;
            float scale = definition.Scale;

            var body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            body.localPosition = new Vector3(0f, scale * 0.5f, 0f);
            _body = body;

            if (definition.Shape != EnemyShape.None)
            {
                BuildCreature(body, definition);
            }
            else if (definition.Model != null)
            {
                // Models sit on y = 0, the body pivot is at half height, so lower the model by that amount.
                Color tint = Color.Lerp(Color.white, definition.Color, ModelTintStrength);
                ModelFit.Place(definition.Model, body, new Vector3(0f, -scale * 0.5f, 0f), 0f, definition.ModelScale, tint, ModelEmission);
            }
            else
            {
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
            }

            GameObject bar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, BarColor, transform, new Vector3(0f, _procedural ? _barHeight : BarOffsetY, 0f), new Vector3(BarWidth, BarHeight, BarHeight), BarEmission);
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

