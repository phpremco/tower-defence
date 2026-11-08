using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Keeps a panel laid out at a fixed reference size inside its parent (the safe area): when the parent is smaller than
    /// the panel plus a margin, the panel scales down as a whole, so every line in it stays on screen whatever the aspect
    /// of the window. It never scales up. The canvas scaler matches width and height halfway, so a 16:9 screen always fits
    /// the reference layout, but a narrower window (a docked Game view, a handheld in portrait) gets less width than the
    /// panel was drawn for. Checks the parent's size once per frame and only writes when it changed.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class FitInsideParent : MonoBehaviour
    {
        private RectTransform _rect;
        private RectTransform _parent;
        private float _margin;
        private Vector2 _lastParentSize = new Vector2(-1f, -1f);

        /// <summary>The scale the fit applies, from 0 to 1; owners that animate the panel's scale multiply by it.</summary>
        public float Scale { get; private set; } = 1f;

        public static FitInsideParent Add(RectTransform panel, float margin)
        {
            var fit = panel.gameObject.AddComponent<FitInsideParent>();
            fit._margin = margin;
            fit._rect = panel;
            fit._parent = panel.parent as RectTransform;
            fit.Refit();
            return fit;
        }

        /// <summary>The share of a panel of that size that fits into a space, with the margin on every side; at most 1.</summary>
        public static float FitScale(Vector2 panelSize, Vector2 space, float margin)
        {
            if (panelSize.x <= 0f || panelSize.y <= 0f)
            {
                return 1f;
            }

            float width = Mathf.Max(0f, space.x - 2f * margin) / panelSize.x;
            float height = Mathf.Max(0f, space.y - 2f * margin) / panelSize.y;
            return Mathf.Clamp01(Mathf.Min(width, height));
        }

        private void LateUpdate()
        {
            if (_parent != null && _parent.rect.size != _lastParentSize)
            {
                Refit();
            }
        }

        private void Refit()
        {
            // Before the canvas has laid out its first frame the parent has no size yet; the next frame tries again.
            if (_parent == null || _parent.rect.width <= 0f || _parent.rect.height <= 0f)
            {
                return;
            }

            _lastParentSize = _parent.rect.size;
            float previous = Scale;
            Scale = FitScale(_rect.rect.size, _lastParentSize, _margin);
            // Keeps whatever an owner animates on top (a pop-in) by the ratio, instead of overwriting it.
            _rect.localScale = previous > 0f ? _rect.localScale * (Scale / previous) : Vector3.one * Scale;
        }
    }
}
