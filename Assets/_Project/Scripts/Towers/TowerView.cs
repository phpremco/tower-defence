using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// Placeholder look of a tower: dark base, light glowing head with a barrel, one star per level,
    /// a health bar once damaged and a dark husk when destroyed.
    /// Light top and dark bottom make the tower stand out from the plane.
    /// </summary>
    [RequireComponent(typeof(Tower))]
    public sealed class TowerView : MonoBehaviour
    {
        private const float StarSize = 0.17f;
        private const float StarSpacing = 0.26f;
        private const float StarHeight = 1.75f;
        private const float StarEmission = 3f;
        private const float HeadEmission = 0.8f;
        private const float BarrelEmission = 1.5f;
        private const float BarWidth = 1f;
        private const float BarHeight = 0.1f;
        private const float BarY = 2.1f;
        private const float BarEmission = 1.5f;
        private static readonly Color BaseColor = new Color(0.1f, 0.12f, 0.18f);
        private static readonly Color RingColor = new Color(0.4f, 0.8f, 1f);
        private static readonly Color StarColor = new Color(1f, 0.9f, 0.3f);
        private static readonly Color HealthyColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color DamagedColor = new Color(1f, 0.55f, 0.2f);
        private static readonly Color WreckColor = new Color(0.12f, 0.12f, 0.13f);

        private readonly GameObject[] _stars = new GameObject[TowerDefinition.LevelCount];

        private Tower _tower;
        private Transform _head;
        private Transform _bar;
        private Renderer _barRenderer;
        private Renderer[] _glowParts;
        private Material[] _glowMaterials;
        private bool _built;

        private void Awake()
        {
            _tower = GetComponent<Tower>();
            _tower.LevelChanged += OnLevelChanged;
            _tower.StateChanged += OnStateChanged;
            _tower.Fired += OnFired;
        }

        private void OnDestroy()
        {
            _tower.LevelChanged -= OnLevelChanged;
            _tower.StateChanged -= OnStateChanged;
            _tower.Fired -= OnFired;
        }

        private void OnLevelChanged(Tower tower)
        {
            if (!_built)
            {
                Build(tower.Definition);
                _built = true;
            }

            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].SetActive(i <= tower.LevelIndex && !tower.IsWrecked);
            }
        }

        private void OnStateChanged(Tower tower)
        {
            bool damaged = tower.Health.Fraction < 1f && !tower.IsWrecked;
            _bar.gameObject.SetActive(damaged);
            if (damaged)
            {
                float fraction = tower.Health.Fraction;
                _bar.localScale = new Vector3(BarWidth * Mathf.Max(0.001f, fraction), BarHeight, BarHeight);
                _barRenderer.sharedMaterial = PlaceholderVisuals.MaterialFor(Color.Lerp(DamagedColor, HealthyColor, fraction), BarEmission);
            }

            for (int i = 0; i < _glowParts.Length; i++)
            {
                _glowParts[i].sharedMaterial = tower.IsWrecked ? PlaceholderVisuals.MaterialFor(WreckColor, 0f) : _glowMaterials[i];
            }

            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].SetActive(i <= tower.LevelIndex && !tower.IsWrecked);
            }
        }

        private void Build(TowerDefinition definition)
        {
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, BaseColor, transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.85f, 0.2f, 0.85f));
            GameObject ring = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, RingColor, transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.9f, 0.03f, 0.9f), BarrelEmission);

            var head = new GameObject("Head").transform;
            head.SetParent(transform, false);
            head.localPosition = new Vector3(0f, 0.8f, 0f);
            _head = head;

            GameObject block = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, definition.Color, head, Vector3.zero, new Vector3(0.6f, 0.5f, 0.6f), HeadEmission);
            GameObject barrel = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, definition.Color, head, new Vector3(0f, 0f, 0.45f), new Vector3(0.18f, 0.35f, 0.18f), BarrelEmission);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            _glowParts = new[] { ring.GetComponent<Renderer>(), block.GetComponent<Renderer>(), barrel.GetComponent<Renderer>() };
            _glowMaterials = new Material[_glowParts.Length];
            for (int i = 0; i < _glowParts.Length; i++)
            {
                _glowMaterials[i] = _glowParts[i].sharedMaterial;
            }

            float startX = -StarSpacing * (_stars.Length - 1) * 0.5f;
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i] = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, StarColor, transform, new Vector3(startX + i * StarSpacing, StarHeight, 0f), Vector3.one * StarSize, StarEmission);
            }

            GameObject bar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, HealthyColor, transform, new Vector3(0f, BarY, 0f), new Vector3(BarWidth, BarHeight, BarHeight), BarEmission);
            _bar = bar.transform;
            _barRenderer = bar.GetComponent<Renderer>();
            _bar.gameObject.SetActive(false);
        }

        private void OnFired(Vector3 targetPosition)
        {
            Vector3 flat = targetPosition - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                _head.rotation = Quaternion.LookRotation(flat);
            }
        }
    }
}
