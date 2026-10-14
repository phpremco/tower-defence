using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>A short expanding glow shown where a projectile lands. Pooled by ProjectilePool.</summary>
    public sealed class ImpactFlash : MonoBehaviour
    {
        private const float Duration = 0.25f;
        private const float StartScale = 0.2f;
        private const float Emission = 3f;
        private static readonly Color FlashColor = new Color(1f, 0.7f, 0.3f);

        private System.Action<ImpactFlash> _release;
        private Transform _shape;
        private float _age;
        private float _endScale;

        public void Show(Vector3 position, float radius, System.Action<ImpactFlash> release)
        {
            if (_shape == null)
            {
                _shape = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, FlashColor, transform, Vector3.zero, Vector3.one, Emission).transform;
            }

            transform.position = position;
            _endScale = Mathf.Max(StartScale * 2f, radius * 2f);
            _age = 0f;
            _release = release;
            _shape.localScale = Vector3.one * StartScale;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / Duration);
            _shape.localScale = Vector3.one * Mathf.Lerp(StartScale, _endScale, t);

            if (t >= 1f)
            {
                _release(this);
            }
        }
    }
}
