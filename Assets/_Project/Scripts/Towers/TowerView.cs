using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// Placeholder look of a tower: dark base, light head, one star per level.
    /// Light top and dark bottom make the tower stand out from the plane.
    /// </summary>
    [RequireComponent(typeof(Tower))]
    public sealed class TowerView : MonoBehaviour
    {
        private const float StarSize = 0.15f;
        private const float StarSpacing = 0.22f;
        private const float StarHeight = 1.6f;
        private static readonly Color BaseColor = new Color(0.12f, 0.14f, 0.2f);
        private static readonly Color StarColor = new Color(1f, 0.9f, 0.3f);

        private readonly GameObject[] _stars = new GameObject[TowerDefinition.LevelCount];

        private Tower _tower;
        private Transform _head;
        private bool _built;

        private void Awake()
        {
            _tower = GetComponent<Tower>();
            _tower.LevelChanged += OnLevelChanged;
            _tower.Fired += OnFired;
        }

        private void OnDestroy()
        {
            _tower.LevelChanged -= OnLevelChanged;
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
                _stars[i].SetActive(i <= tower.LevelIndex);
            }
        }

        private void Build(TowerDefinition definition)
        {
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, BaseColor, transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.8f, 0.25f, 0.8f));
            GameObject head = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, definition.Color, transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.55f, 0.55f, 0.8f));
            _head = head.transform;

            float startX = -StarSpacing * (_stars.Length - 1) * 0.5f;
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i] = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, StarColor, transform, new Vector3(startX + i * StarSpacing, StarHeight, 0f), Vector3.one * StarSize);
            }
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
