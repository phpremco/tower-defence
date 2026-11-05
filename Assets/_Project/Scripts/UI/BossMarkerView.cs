using Game.Enemies;
using Game.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Points the player at the boss. From the alarm until the boss is dead an arrow turns towards where it will come
    /// up or where it walks: at the screen edge while that lies out of view, hanging over the boss while it is in view.
    /// </summary>
    public sealed class BossMarkerView : MonoBehaviour
    {
        private const float EdgeMargin = 90f;
        private const float ArrowSize = 60f;
        private const float LabelWidth = 220f;
        private const float LabelHeight = 40f;
        private const float LabelGap = 46f;
        private const int LabelSize = 28;
        private const float MarkerLift = 2.8f;
        private const float PulseRate = 5f;
        private const float PulseDepth = 0.12f;
        private const string Label = "BOSS";
        // The boss bar (BossHealthView) hangs this far down from the top of the safe area with its name and phase line:
        // while it shows, the marker stays below it so the two never overlap.
        private const float BarClearance = 185f;
        private static readonly Color Warning = new Color(1f, 0.4f, 0.32f);

        private LevelContext _context;
        private Camera _camera;
        private RectTransform _area;
        private HudKeepOut _keepOut;
        private RectTransform _arrow;
        private RectTransform _label;
        private Enemy _boss;
        private Vector3 _target;
        private bool _tracking;

        public void Initialize(LevelContext context, RectTransform root, Camera camera, HudKeepOut keepOut)
        {
            _context = context;
            _keepOut = keepOut;
            _camera = camera;
            _area = root;

            var arrow = new GameObject("BossArrow", typeof(RectTransform)).AddComponent<Image>();
            arrow.transform.SetParent(root, false);
            arrow.sprite = IconFactory.Get(IconType.Arrow);
            arrow.color = Warning;
            arrow.raycastTarget = false;
            _arrow = arrow.rectTransform;
            _arrow.anchorMin = new Vector2(0.5f, 0.5f);
            _arrow.anchorMax = new Vector2(0.5f, 0.5f);
            _arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);

            Text text = UiKit.CreateFixedText(root, Label, LabelSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(LabelWidth, LabelHeight));
            text.color = Warning;
            _label = text.rectTransform;

            Show(false);
            context.BossWaveStarted.Subscribe(OnBossWave);
            context.EnemySpawned.Subscribe(OnSpawned);
            context.EnemyKilled.Subscribe(OnKilled);
        }

        private void OnDestroy()
        {
            if (_context == null)
            {
                return;
            }

            _context.BossWaveStarted.Unsubscribe(OnBossWave);
            _context.EnemySpawned.Unsubscribe(OnSpawned);
            _context.EnemyKilled.Unsubscribe(OnKilled);
        }

        // Until the boss appears the arrow points at its entrance, so the alarm already says where to look.
        private void OnBossWave()
        {
            _boss = null;
            _target = EntrancePosition();
            _tracking = true;
        }

        private void OnSpawned(Enemy enemy)
        {
            if (enemy.Definition.IsBoss)
            {
                _boss = enemy;
                _tracking = true;
            }
        }

        private void OnKilled(Enemy enemy)
        {
            if (enemy == _boss)
            {
                _boss = null;
                _tracking = false;
            }
        }

        private Vector3 EntrancePosition()
        {
            LevelDefinition level = _context.Level;
            Vector2Int cell = level.BossEntrance.Kind == EntranceKind.Gate ? level.MainRoad[0] : level.BossEntrance.Cell;
            return level.Grid.CellToWorld(cell);
        }

        private void LateUpdate()
        {
            if (_boss != null && !_boss.IsAlive)
            {
                // Reaching the base ends the boss without a kill.
                _boss = null;
                _tracking = false;
            }

            if (!_tracking || _context.Outcome != LevelOutcome.Running)
            {
                Show(false);
                return;
            }

            if (_boss != null)
            {
                _target = _boss.Position;
            }

            Place();
        }

        private void Place()
        {
            Vector3 screen = _camera.WorldToScreenPoint(_target + Vector3.up * MarkerLift);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screen, null, out Vector2 local);
            Rect inner = _area.rect;
            inner.xMin += EdgeMargin;
            inner.xMax -= EdgeMargin;
            inner.yMin += EdgeMargin;
            inner.yMax -= EdgeMargin;
            // While the boss lives its bar names it at the top: the marker keeps below the bar and drops its own word.
            bool barShown = _boss != null;
            if (barShown)
            {
                inner.yMax = Mathf.Min(inner.yMax, _area.rect.yMax - BarClearance);
            }

            Show(true);
            ShowLabel(!barShown);
            if (screen.z > 0f && inner.xMin <= local.x && local.x <= inner.xMax && inner.yMin <= local.y && local.y <= inner.yMax + (barShown ? BarClearance : 0f))
            {
                // In view: the arrow hangs over the boss pointing down (never up into the bar), the word above it.
                local.y = Mathf.Min(local.y, inner.yMax);
                _arrow.anchoredPosition = local;
                _arrow.localRotation = Quaternion.Euler(0f, 0f, -90f);
                _label.anchoredPosition = local + Vector2.up * LabelGap;
            }
            else
            {
                // Out of view: pinned to the edge of the screen on the line towards the boss.
                Vector2 direction = screen.z > 0f ? local - inner.center : inner.center - local;
                if (direction.sqrMagnitude < 1f)
                {
                    direction = Vector2.up;
                }

                float reach = Mathf.Min(inner.width * 0.5f / Mathf.Max(0.001f, Mathf.Abs(direction.x)), inner.height * 0.5f / Mathf.Max(0.001f, Mathf.Abs(direction.y)));
                Vector2 edge = inner.center + direction * reach;
                _arrow.anchoredPosition = edge;
                _arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                _label.anchoredPosition = edge - direction.normalized * LabelGap;
            }

            // The HUD stays readable: a marker that would cover a panel moves off it, or waits while it cannot.
            if (!_keepOut.PlaceTogether(_arrow, _label))
            {
                Show(false);
                return;
            }

            _arrow.localScale = Vector3.one * (1f + PulseDepth * Mathf.Sin(Time.unscaledTime * PulseRate));
        }

        private void Show(bool on)
        {
            if (_arrow.gameObject.activeSelf != on)
            {
                _arrow.gameObject.SetActive(on);
                _label.gameObject.SetActive(on);
            }
        }

        private void ShowLabel(bool on)
        {
            if (_label.gameObject.activeSelf != on)
            {
                _label.gameObject.SetActive(on);
            }
        }
    }
}
