using UnityEngine;

namespace Game.Core
{
    /// <summary>Lets shared glow materials breathe: their emission swells and sinks slowly around the colour they were given. Pure view code.</summary>
    public sealed class EmissionPulse : MonoBehaviour
    {
        private const string EmissionColor = "_EmissionColor";
        private const float PhaseStep = 1.9f;

        private Material[] _materials;
        private Color[] _rest;
        private float _depth;
        private float _speed;

        public void Initialize(Material[] materials, float depth, float speed)
        {
            _materials = materials;
            _rest = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                _rest[i] = materials[i].GetColor(EmissionColor);
            }

            _depth = depth;
            _speed = speed;
        }

        private void Update()
        {
            for (int i = 0; i < _materials.Length; i++)
            {
                float swell = 1f + _depth * Mathf.Sin(Time.time * _speed + i * PhaseStep);
                _materials[i].SetColor(EmissionColor, _rest[i] * swell);
            }
        }
    }
}
