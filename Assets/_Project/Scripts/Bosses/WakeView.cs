using Game.Core;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// What shows of a boss hidden under a liquid (Docs/BOSSES.md): a glow under the surface, a rising stream of bubbles
    /// and a churn at the surface, following it while it swims. Off while it is not under.
    /// </summary>
    public sealed class WakeView : MonoBehaviour
    {
        private const float GlowRadius = 1.6f;
        private const float BubbleRate = 40f;
        private const float BubbleSpread = 0.9f;

        private ParticleSystem _bubbles;
        private GameObject _glow;

        public static WakeView Create(Transform parent, Color glow, Color bubbles)
        {
            var go = new GameObject("Wake");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<WakeView>();
            view._glow = GlowDecal.PlacePool(go.transform, glow, GlowRadius, 0.05f);
            view._bubbles = ParticleKit.Create(go.transform, "Bubbles", Vector3.up * 0.02f, bubbles, 0.5f, 1.1f, 0.05f, 0.16f, 0.4f, 1.1f, -0.3f, 120, BubbleSpread);
            view.Show(false, Vector3.zero);
            return view;
        }

        public void Show(bool on, Vector3 at)
        {
            transform.position = at;
            _glow.SetActive(on);
            ParticleKit.SetRate(_bubbles, on ? BubbleRate : 0f);
        }

        public void Follow(Vector3 at) => transform.position = at;
    }
}
