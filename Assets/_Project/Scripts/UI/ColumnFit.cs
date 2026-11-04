using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Keeps a menu column under its title: the column hangs from a fixed distance below the top of its parent and
    /// shrinks evenly when its rows would run past the bottom margin, so the title never overlaps the first row on a
    /// short screen (720p, or a wide editor window) and the last button never leaves the safe area.
    /// </summary>
    public sealed class ColumnFit : MonoBehaviour
    {
        private RectTransform _column;
        private RectTransform _parent;
        private float _top;
        private float _bottom;

        /// <summary>Hangs <paramref name="column"/> <paramref name="top"/> units below its parent's top edge.</summary>
        public static ColumnFit Attach(RectTransform column, float top, float bottom)
        {
            column.anchorMin = new Vector2(0.5f, 1f);
            column.anchorMax = new Vector2(0.5f, 1f);
            column.pivot = new Vector2(0.5f, 1f);
            column.anchoredPosition = new Vector2(column.anchoredPosition.x, -top);
            var fit = column.gameObject.AddComponent<ColumnFit>();
            fit._column = column;
            fit._parent = (RectTransform)column.parent;
            fit._top = top;
            fit._bottom = bottom;
            return fit;
        }

        /// <summary>The share the column is drawn at, 1 when it fits; for probes.</summary>
        public float Scale => _column != null ? _column.localScale.y : 1f;

        // After layout, so the content size fitter has set the column's height for this frame.
        private void LateUpdate()
        {
            float height = _column.rect.height;
            float room = _parent.rect.height - _top - _bottom;
            float scale = height > room && height > 0f ? Mathf.Max(0.1f, room / height) : 1f;
            if (!Mathf.Approximately(_column.localScale.y, scale))
            {
                _column.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
