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
        private const float ModelTintStrength = 0.55f;
        private const float ModelEmission = 0.25f;
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
        private const float WallWidth = 1.05f;
        private const float WallDepth = 0.34f;
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
        private Renderer[] _modelRenderers;
        private Material[][] _modelMaterials;
        private Material[] _glowMaterials;
        private bool _built;
        private GameObject _currentModel;
        private GameObject _currentModelSource;

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

            SwapModelForLevel(tower);

            // A blockade only shows its gun once the level has one.
            if (tower.Definition.BlocksGroundEnemies)
            {
                _head.gameObject.SetActive(tower.Definition.GetLevel(tower.LevelIndex).Damage > 0f);
            }

            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].SetActive(i <= tower.LevelIndex && !tower.IsWrecked);
            }
        }

        // Visual upgrades: the newest level at or below the current one that names a model wins.
        private void SwapModelForLevel(Tower tower)
        {
            if (_modelRenderers == null)
            {
                return;
            }

            GameObject wanted = null;
            for (int i = tower.LevelIndex; i >= 0 && wanted == null; i--)
            {
                wanted = tower.Definition.GetLevel(i).Model;
            }

            if (wanted == null || wanted == _currentModelSource)
            {
                return;
            }

            Destroy(_currentModel);
            PlaceModel(wanted, tower.Definition);
        }

        private void PlaceModel(GameObject source, TowerDefinition definition)
        {
            bool keep = definition.KeepModelColours;
            Color tint = keep ? Color.white : Color.Lerp(Color.white, definition.Color, ModelTintStrength);
            _currentModel = ModelFit.Place(source, _head, new Vector3(0f, -0.35f, 0f), 0f, definition.ModelScale, tint, keep ? 0f : ModelEmission);
            _currentModelSource = source;
            _modelRenderers = _currentModel.GetComponentsInChildren<Renderer>();
            _modelMaterials = new Material[_modelRenderers.Length][];
            for (int i = 0; i < _modelRenderers.Length; i++)
            {
                _modelMaterials[i] = _modelRenderers[i].sharedMaterials;
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

            if (_modelRenderers != null)
            {
                Material wreck = PlaceholderVisuals.MaterialFor(WreckColor, 0f);
                for (int i = 0; i < _modelRenderers.Length; i++)
                {
                    if (tower.IsWrecked)
                    {
                        var wrecked = new Material[_modelMaterials[i].Length];
                        for (int m = 0; m < wrecked.Length; m++)
                        {
                            wrecked[m] = wreck;
                        }

                        _modelRenderers[i].sharedMaterials = wrecked;
                    }
                    else
                    {
                        _modelRenderers[i].sharedMaterials = _modelMaterials[i];
                    }
                }
            }

            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].SetActive(i <= tower.LevelIndex && !tower.IsWrecked);
            }
        }

        private void Build(TowerDefinition definition)
        {
            bool wall = definition.BlocksGroundEnemies;
            GameObject ring;
            if (wall)
            {
                // A low wall across the road with a glowing top edge. Its gun sits on top and appears with the first gun upgrade.
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, BaseColor, transform, new Vector3(0f, 0.3f, 0f), new Vector3(WallWidth, 0.6f, WallDepth));
                ring = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, definition.Color, transform, new Vector3(0f, 0.63f, 0f), new Vector3(WallWidth + 0.04f, 0.06f, WallDepth + 0.04f), BarrelEmission);
            }
            else
            {
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, BaseColor, transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.85f, 0.2f, 0.85f));
                ring = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, RingColor, transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.9f, 0.03f, 0.9f), BarrelEmission);
            }

            var head = new GameObject("Head").transform;
            head.SetParent(transform, false);
            head.localPosition = new Vector3(0f, wall ? 0.95f : 0.8f, 0f);
            _head = head;

            if (definition.Model != null)
            {
                PlaceModel(definition.Model, definition);
                _glowParts = new[] { ring.GetComponent<Renderer>() };
                _glowMaterials = new[] { _glowParts[0].sharedMaterial };
            }
            else
            {
                GameObject block = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, definition.Color, head, Vector3.zero, new Vector3(0.6f, 0.5f, 0.6f), HeadEmission);
                GameObject barrel = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, definition.Color, head, new Vector3(0f, 0f, 0.45f), new Vector3(0.18f, 0.35f, 0.18f), BarrelEmission);
                barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _glowParts = new[] { ring.GetComponent<Renderer>(), block.GetComponent<Renderer>(), barrel.GetComponent<Renderer>() };
                _glowMaterials = new Material[_glowParts.Length];
                for (int i = 0; i < _glowParts.Length; i++)
                {
                    _glowMaterials[i] = _glowParts[i].sharedMaterial;
                }
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

