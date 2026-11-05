using Game.Core;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The warning on a tower a boss is about to break out beside (Docs/BOSSES.md: "a pulsing red ring 3 s before"): a
    /// red band on the ground round the tower that pulses faster as the moment comes, on the painted range band when
    /// there is one, else a glow pool.
    /// </summary>
    public sealed class TargetRingView : MonoBehaviour
    {
        private const float Radius = 0.85f;
        private const float Height = 0.06f;
        private const float SlowPulse = 4f;
        private const float FastPulse = 14f;
        private const float PulseDepth = 0.35f;
        private static readonly Color Red = new Color(1f, 0.12f, 0.08f, 1f) * 7f;
        private static readonly Color Pool = new Color(1f, 0.1f, 0.06f, 0.8f);

        private MeshRenderer _band;
        private MaterialPropertyBlock _block;
        private float _seconds = 1f;
        private float _age;
        private float _phase;

        public static TargetRingView Create(Transform parent)
        {
            var go = new GameObject("TargetRing");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<TargetRingView>();
            MarkerArt art = MarkerArt.Active;
            view._band = art != null && art.Has(MarkerKind.RangeBand) ? art.Place(MarkerKind.RangeBand, go.transform) : null;
            view._block = view._band != null ? new MaterialPropertyBlock() : null;
            // A red glow on the ground inside the band, so the warning reads even under the tower and the mist.
            GlowDecal.PlacePool(go.transform, Pool, 1f);

            go.SetActive(false);
            return view;
        }

        /// <summary>Rings a spot for the seconds left before the boss breaks out there.</summary>
        public void Show(Vector3 at, float seconds)
        {
            transform.position = new Vector3(at.x, at.y + Height, at.z);
            _seconds = Mathf.Max(0.1f, seconds);
            _age = 0f;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            // The pulse quickens towards the moment, like a heartbeat.
            float urgency = Mathf.Clamp01(_age / _seconds);
            _phase += dt * Mathf.Lerp(SlowPulse, FastPulse, urgency);
            float pulse = 1f + Mathf.Sin(_phase) * PulseDepth;
            transform.localScale = new Vector3(Radius * pulse, 1f, Radius * pulse);
            if (_band != null)
            {
                MarkerArt.SetTint(_band, _block, Red * (0.7f + 0.3f * pulse));
            }
        }
    }
}
