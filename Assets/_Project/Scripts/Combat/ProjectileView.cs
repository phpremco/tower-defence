using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Placeholder look of a projectile: a bright glowing sphere with a short trail.</summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        private const float Size = 0.22f;
        private const float Emission = 4f;
        private const float TrailTime = 0.18f;
        private const float TrailWidth = 0.16f;
        private static readonly Color ProjectileColor = new Color(1f, 0.9f, 0.45f);

        private void Awake()
        {
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, ProjectileColor, transform, Vector3.zero, Vector3.one * Size, Emission);

            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = TrailTime;
            trail.widthMultiplier = TrailWidth;
            trail.sharedMaterial = PlaceholderVisuals.MaterialFor(ProjectileColor, Emission);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void OnEnable()
        {
            var trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }
        }
    }
}
