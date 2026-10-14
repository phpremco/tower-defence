using UnityEngine;

namespace Game.Levels
{
    /// <summary>Look of one level as data: palette, light, fog and bloom. Changing it needs no code.</summary>
    [CreateAssetMenu(menuName = "Game/Visual Theme", fileName = "VisualTheme")]
    public sealed class VisualTheme : ScriptableObject
    {
        [SerializeField] private Color _backgroundColor = new Color(0.04f, 0.05f, 0.08f);
        [SerializeField] private Color _groundColor = new Color(0.25f, 0.28f, 0.33f);
        [SerializeField] private Color _lightColor = new Color(0.8f, 0.85f, 1f);
        [SerializeField] [Min(0f)] private float _lightIntensity = 1.1f;
        [SerializeField] private Color _fogColor = new Color(0.06f, 0.08f, 0.12f);
        [SerializeField] [Min(0f)] private float _fogDensity = 0.015f;
        [SerializeField] [Min(0f)] private float _bloomIntensity = 0.8f;

        public Color BackgroundColor => _backgroundColor;
        public Color GroundColor => _groundColor;
        public Color LightColor => _lightColor;
        public float LightIntensity => _lightIntensity;
        public Color FogColor => _fogColor;
        public float FogDensity => _fogDensity;
        public float BloomIntensity => _bloomIntensity;
    }
}
