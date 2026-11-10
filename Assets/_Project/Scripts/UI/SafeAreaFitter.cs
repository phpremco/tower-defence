using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Keeps a canvas's HUD frame (the "SafeArea" rect <see cref="UiKit.CreateRoot"/> makes) inside the safe area and the
    /// title-safe margin, and follows them when the resolution, the safe area or the HUD margin option changes.
    /// </summary>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _safeArea;
        private int _width;
        private int _height;
        private float _margin = -1f;

        /// <summary>The frame's anchors in the canvas as last applied, for the backdrops that cover the whole screen.</summary>
        public Vector2 AnchorMin { get; private set; } = Vector2.zero;

        public Vector2 AnchorMax { get; private set; } = Vector2.one;

        public static SafeAreaFitter Attach(RectTransform frame)
        {
            var fitter = frame.gameObject.AddComponent<SafeAreaFitter>();
            fitter._rect = frame;
            fitter.Apply();
            return fitter;
        }

        private void LateUpdate()
        {
            if (Screen.safeArea != _safeArea || Screen.width != _width || Screen.height != _height || !Mathf.Approximately(UiKit.SafeMarginPercent, _margin))
            {
                Apply();
            }
        }

        private void Apply()
        {
            _safeArea = Screen.safeArea;
            _width = Screen.width;
            _height = Screen.height;
            _margin = UiKit.SafeMarginPercent;
            SafeFrame.Anchors(_safeArea, _width, _height, _margin, out Vector2 min, out Vector2 max);
            AnchorMin = min;
            AnchorMax = max;
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
