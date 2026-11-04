using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The ground of the menu island as the creatures walk on it: a bilinear lookup in the 16-bit height map Blender ray
    /// cast onto the island mesh (<see cref="MenuDioramaArt"/>), so feet land on what is drawn. Without a map the ground
    /// is flat at zero.
    /// </summary>
    public sealed class MenuTerrain
    {
        private readonly Texture2D _map;
        private readonly float _minHeight;
        private readonly float _range;
        private readonly Vector2 _origin;
        private readonly float _size;

        public MenuTerrain(MenuDioramaArt art)
        {
            if (art == null || !art.IsComplete)
            {
                return;
            }

            _map = art.HeightMap;
            _minHeight = art.MinHeight;
            _range = art.MaxHeight - art.MinHeight;
            _origin = art.HeightOrigin;
            _size = art.HeightSize;
        }

        /// <summary>The ground height at a world point; the lowest height off the map.</summary>
        public float Height(float x, float z)
        {
            if (_map == null)
            {
                return 0f;
            }

            float u = (x - _origin.x) / _size;
            float v = (z - _origin.y) / _size;
            if (u < 0f || u > 1f || v < 0f || v > 1f)
            {
                return _minHeight;
            }

            return _minHeight + _map.GetPixelBilinear(u, v).r * _range;
        }

        /// <summary>A point on the ground, lifted a little so feet and roots do not sink into it.</summary>
        public Vector3 OnGround(float x, float z, float lift = 0f) => new Vector3(x, Height(x, z) + lift, z);
    }
}
