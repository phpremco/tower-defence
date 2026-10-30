using UnityEngine;

namespace Game.Core
{
    /// <summary>Makes a light waver like a flame: its intensity wanders around a base value with smooth noise. Pure view code.</summary>
    [RequireComponent(typeof(Light))]
    public sealed class LightFlicker : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;
        private float _depth;
        private float _speed;
        private float _seed;

        public void Initialize(float baseIntensity, float depth, float speed)
        {
            _light = GetComponent<Light>();
            _baseIntensity = baseIntensity;
            _depth = depth;
            _speed = speed;
            _seed = Random.Range(0f, 1000f);
            _light.intensity = baseIntensity;
        }

        private void Update()
        {
            float wander = Mathf.PerlinNoise(Time.time * _speed, _seed) * 2f - 1f;
            _light.intensity = _baseIntensity * (1f + _depth * wander);
        }
    }
}
