using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Brings a liquid sheet to life: slides its texture (and a second glowing layer at another pace, so the two
    /// interfere), rolls slow waves through the mesh so the light plays over it, and lets lava breathe by pulsing
    /// its glow. Pure view code.
    /// </summary>
    public sealed class LiquidFlow : MonoBehaviour
    {
        private const string BaseMap = "_BaseMap";
        private const string EmissionMap = "_EmissionMap";
        private const string EmissionColor = "_EmissionColor";
        private const float SecondWaveShare = 0.4f;
        private const float SecondWaveLength = 1.3f;
        private const float SecondWaveSpeed = 0.8f;
        private const float FlickerShare = 0.3f;
        private const float FlickerRate = 2.7f;

        private Material _material;
        private Vector2 _speed;
        private Vector2 _offset;
        private Material _overlay;
        private Vector2 _overlaySpeed;
        private Vector2 _overlayOffset;
        private Mesh _mesh;
        private Vector3[] _rest;
        private Vector3[] _moving;
        private float _waveHeight;
        private float _waveNumber;
        private float _waveSpeed;
        private Color _emission;
        private float _pulseDepth;
        private float _pulseSpeed;
        private float _time;

        public void Initialize(Material material, Vector2 speed)
        {
            _material = material;
            _speed = speed;
            _time = Random.Range(0f, 100f);
        }

        /// <summary>A second texture layer that drifts at its own pace over the first.</summary>
        public void SetOverlay(Material overlay, Vector2 speed)
        {
            _overlay = overlay;
            _overlaySpeed = speed;
        }

        /// <summary>Rolls waves through the sheet. The mesh must be subdivided for this to show.</summary>
        public void SetWaves(Mesh mesh, float height, float length, float speed)
        {
            _mesh = mesh;
            _rest = mesh.vertices;
            _moving = new Vector3[_rest.Length];
            _waveHeight = height;
            _waveNumber = Mathf.PI * 2f / Mathf.Max(0.1f, length);
            _waveSpeed = speed;
        }

        /// <summary>Lets the glow swell and sink around its base colour, with a faster flicker on top.</summary>
        public void SetPulse(Color emission, float depth, float speed)
        {
            _emission = emission;
            _pulseDepth = depth;
            _pulseSpeed = speed;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            _offset += _speed * Time.deltaTime;
            _material.SetTextureOffset(BaseMap, _offset);
            _material.SetTextureOffset(EmissionMap, _offset);

            if (_overlay != null)
            {
                _overlayOffset += _overlaySpeed * Time.deltaTime;
                _overlay.SetTextureOffset(BaseMap, _overlayOffset);
                _overlay.SetTextureOffset(EmissionMap, _overlayOffset);
            }

            if (_mesh != null)
            {
                Roll();
            }

            if (_pulseDepth > 0f)
            {
                float swell = Mathf.Sin(_time * _pulseSpeed) + FlickerShare * Mathf.Sin(_time * _pulseSpeed * FlickerRate);
                _material.SetColor(EmissionColor, _emission * (1f + _pulseDepth * swell));
            }
        }

        // Two wave trains at an angle to each other: one alone reads as a conveyor belt, two make a surface.
        private void Roll()
        {
            float phase = _time * _waveSpeed;
            float k2 = _waveNumber / SecondWaveLength;
            for (int i = 0; i < _rest.Length; i++)
            {
                Vector3 v = _rest[i];
                float a = Mathf.Sin(v.x * _waveNumber + v.z * _waveNumber * 0.6f + phase);
                float b = Mathf.Sin(v.z * k2 - v.x * k2 * 0.35f - phase * SecondWaveSpeed);
                v.y = _waveHeight * (a * (1f - SecondWaveShare) + b * SecondWaveShare);
                _moving[i] = v;
            }

            _mesh.vertices = _moving;
            _mesh.RecalculateNormals();
        }
    }
}
