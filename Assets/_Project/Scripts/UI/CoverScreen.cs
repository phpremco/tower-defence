using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Lets a dim or backdrop that hangs in the HUD frame still cover the whole screen when the frame is moved in by the
    /// title-safe margin: it stretches its anchors out past the frame to the canvas edges.
    /// </summary>
    public sealed class CoverScreen : MonoBehaviour
    {
        private RectTransform _rect;
        private SafeAreaFitter _frame;
        private Vector2 _shownMin = new Vector2(float.NaN, float.NaN);
        private Vector2 _shownMax;

        /// <summary>Adds the cover to a rect inside the HUD frame; outside one it does nothing.</summary>
        public static void Add(RectTransform rect)
        {
            // Rects between the frame and this one are assumed to stretch over the frame, as the overlay roots do.
            var frame = rect.parent != null ? rect.parent.GetComponentInParent<SafeAreaFitter>() : null;
            if (frame == null)
            {
                return;
            }

            var cover = rect.gameObject.AddComponent<CoverScreen>();
            cover._rect = rect;
            cover._frame = frame;
            cover.LateUpdate();
        }

        private void LateUpdate()
        {
            if (_frame.AnchorMin == _shownMin && _frame.AnchorMax == _shownMax)
            {
                return;
            }

            _shownMin = _frame.AnchorMin;
            _shownMax = _frame.AnchorMax;
            SafeFrame.CoverAnchors(_shownMin, _shownMax, out Vector2 min, out Vector2 max);
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
