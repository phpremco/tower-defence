using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// A glowing bolt an enemy spits at a structure: an element shot, a spray of acid. The damage has already
    /// landed when the bolt is fired; this only draws it flying there in an arc and bursting on arrival. Each view
    /// owns a few and reuses them.
    /// </summary>
    public sealed class EnemyBoltView : MonoBehaviour
    {
        private const float BaseSpeed = 7f;
        private const float ArcHeight = 0.35f;
        private const float CoreSize = 0.12f;
        private const float HaloSize = 0.26f;
        private const float HaloAlpha = 0.35f;
        private const float CoreEmission = 3.5f;
        private const int Sparks = 10;

        private Transform _core;
        private Transform _halo;
        private ParticleSystem _sparks;
        private Vector3 _from;
        private Vector3 _to;
        private float _seconds;
        private float _age = float.PositiveInfinity;
        private float _size = 1f;

        public bool InFlight => _age < _seconds;

        public static EnemyBoltView Create(Transform parent, Color color)
        {
            var go = new GameObject("EnemyBolt");
            go.transform.SetParent(parent, false);
            var bolt = go.AddComponent<EnemyBoltView>();
            bolt._core = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.Lerp(color, Color.white, 0.5f), go.transform, Vector3.zero, Vector3.one * CoreSize, CoreEmission).transform;
            GameObject halo = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, color, go.transform, Vector3.zero, Vector3.one * HaloSize);
            halo.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(color, HaloAlpha, 1.5f);
            bolt._halo = halo.transform;
            Color spark = color * 2f;
            spark.a = 1f;
            bolt._sparks = ParticleKit.Create(parent, "BoltSparks", Vector3.zero, spark, 0.25f, 0.5f, 0.03f, 0.07f, 0.8f, 2.4f, 0.8f, Sparks * 3, 0.1f);
            go.SetActive(false);
            return bolt;
        }

        public void Fire(Vector3 from, Vector3 to, float size)
        {
            _from = from;
            _to = to;
            _size = size;
            _seconds = Mathf.Max(0.12f, Vector3.Distance(from, to) / BaseSpeed);
            _age = 0f;
            transform.localScale = Vector3.one * size;
            transform.position = from;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _seconds);
            Vector3 at = Vector3.Lerp(_from, _to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * ArcHeight * _size);
            transform.position = at;
            _halo.localScale = Vector3.one * (HaloSize * (1f + Mathf.Sin(_age * 30f) * 0.15f));
            _core.localRotation = Quaternion.Euler(0f, _age * 400f, 0f);
            if (t >= 1f)
            {
                _sparks.transform.position = _to;
                _sparks.Emit(Sparks);
                gameObject.SetActive(false);
            }
        }
    }
}
