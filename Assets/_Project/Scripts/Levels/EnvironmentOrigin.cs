using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>The opening creatures rise from at a nest on one terrain kind: a burrow in the ground, a vent in a pool.</summary>
    [Serializable]
    public sealed class EnvironmentOrigin
    {
        [SerializeField] private TerrainKind _terrain;
        [SerializeField] private EnvironmentMesh _model;
        [Tooltip("The model's glow is a white mask: tint it with the nest's element colour.")]
        [SerializeField] private bool _tintWithElement;
        [SerializeField] [Min(0.05f)] private float _scale = 1f;

        public EnvironmentOrigin(TerrainKind terrain, EnvironmentMesh model, bool tintWithElement, float scale)
        {
            _terrain = terrain;
            _model = model;
            _tintWithElement = tintWithElement;
            _scale = scale;
        }

        public TerrainKind Terrain => _terrain;
        public EnvironmentMesh Model => _model;
        public bool TintWithElement => _tintWithElement;
        public float Scale => _scale;
    }
}
