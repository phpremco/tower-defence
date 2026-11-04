using System;
using UnityEngine;

namespace Game.UI
{
    /// <summary>A point light of the stage: in a theme's accent colour, or its own colour when the slot is None.</summary>
    [Serializable]
    public struct MenuDioramaLight
    {
        public string Name;
        public Vector3 Position;
        public MenuThemeSlot Theme;
        public Color Colour;
        public float Intensity;
        public float Range;
    }
}
