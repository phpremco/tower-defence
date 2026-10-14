using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Placeholder look of a projectile: a small glowing-coloured sphere.</summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        private const float Size = 0.22f;
        private static readonly Color ProjectileColor = new Color(1f, 0.95f, 0.5f);

        private void Awake()
        {
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, ProjectileColor, transform, Vector3.zero, Vector3.one * Size);
        }
    }
}
