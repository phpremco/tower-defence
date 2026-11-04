using System;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>A pool of the island: its kind and theme, the outline of its shore and where its creature roams.</summary>
    [Serializable]
    public sealed class MenuDioramaPool
    {
        [SerializeField] private TerrainKind _kind;
        [SerializeField] private MenuThemeSlot _theme;
        [SerializeField] private Vector2 _centre;
        [SerializeField] private float _radius;
        [SerializeField] private float _surface;
        [Tooltip("Middle of the arc of bank its creature walks along, degrees counter-clockwise from +x.")]
        [SerializeField] private float _bankDegrees;
        [Tooltip("The shore in world x/z, as Blender cut the basin.")]
        [SerializeField] private Vector2[] _outline = new Vector2[0];

        public MenuDioramaPool(TerrainKind kind, MenuThemeSlot theme, Vector2 centre, float radius, float surface, float bankDegrees, Vector2[] outline)
        {
            _kind = kind;
            _theme = theme;
            _centre = centre;
            _radius = radius;
            _surface = surface;
            _bankDegrees = bankDegrees;
            _outline = outline;
        }

        public TerrainKind Kind => _kind;
        public MenuThemeSlot Theme => _theme;
        public Vector3 Centre => new Vector3(_centre.x, 0f, _centre.y);
        public float Radius => _radius;
        public float Surface => _surface;
        public float BankDegrees => _bankDegrees;
        public Vector2[] Outline => _outline;
    }
}
