using System;
using UnityEngine;

namespace Game.UI
{
    /// <summary>Where one export stands: world position (on the ground as Blender placed it), yaw about +Y, uniform scale.</summary>
    [Serializable]
    public struct MenuDioramaPlacement
    {
        public int Model;
        public Vector3 Position;
        public float Yaw;
        public float Scale;
        [Tooltip("Lays a soft glow on the ground under it in its corner's accent colour.")]
        public bool GlowPool;
        public MenuThemeSlot Theme;
    }
}
