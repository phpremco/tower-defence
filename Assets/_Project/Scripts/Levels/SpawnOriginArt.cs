using System;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>The living opening (README round 23) a nest on one terrain wears: a burrow in the ground, a vent in a pool.</summary>
    [Serializable]
    public sealed class SpawnOriginArt
    {
        [SerializeField] private TerrainKind _terrain;
        [SerializeField] private ModelArtDefinition _model;
        [Tooltip("The glow is a white mask: tint it with the nest's element colour (the liquid ones).")]
        [SerializeField] private bool _tintWithElement;
        [Tooltip("Closed and marked share one pose and differ only in glow (the burrow): closed is dark, marked pulses.")]
        [SerializeField] private bool _glowMarks;

        public SpawnOriginArt(TerrainKind terrain, ModelArtDefinition model, bool tintWithElement, bool glowMarks)
        {
            _terrain = terrain;
            _model = model;
            _tintWithElement = tintWithElement;
            _glowMarks = glowMarks;
        }

        public TerrainKind Terrain => _terrain;
        public ModelArtDefinition Model => _model;
        public bool TintWithElement => _tintWithElement;
        public bool GlowMarks => _glowMarks;
    }
}
