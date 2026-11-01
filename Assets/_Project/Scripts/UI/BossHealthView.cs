using Game.Enemies;
using Game.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The boss's health and armour as one wide bar at the top of the screen, named, for as long as the boss lives.
    /// The small bar over its head is lost in the crowd; this one tells the player at a glance whether the shots
    /// are still chewing through plate or finally into the flesh.
    /// </summary>
    public sealed class BossHealthView : MonoBehaviour
    {
        private const float Width = 560f;
        private const float Top = -64f;
        private const float HealthHeight = 20f;
        private const float ArmorHeight = 10f;
        private const float Gap = 3f;
        private const float LabelHeight = 30f;
        private const int LabelSize = 24;
        private const float Border = 2f;
        private static readonly Color Backing = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color HealthColor = new Color(1f, 0.35f, 0.3f);
        private static readonly Color ArmorColor = new Color(0.7f, 0.88f, 1f);
        private static readonly Color LabelColor = new Color(1f, 0.6f, 0.55f);

        private LevelContext _context;
        private RectTransform _root;
        private RectTransform _healthFill;
        private RectTransform _armorFill;
        private GameObject _armorRow;
        private Text _label;
        private Enemy _boss;

        public void Initialize(LevelContext context, RectTransform parent)
        {
            _context = context;
            _root = new GameObject("BossHealth", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(parent, false);
            _root.anchorMin = new Vector2(0.5f, 1f);
            _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            _root.anchoredPosition = new Vector2(0f, Top);
            _root.sizeDelta = new Vector2(Width, LabelHeight + ArmorHeight + Gap + HealthHeight);

            _label = UiKit.CreateFixedText(_root, string.Empty, LabelSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Width, LabelHeight));
            _label.color = LabelColor;

            _armorRow = Row(_root, "Armor", -LabelHeight, ArmorHeight, ArmorColor, out _armorFill);
            Row(_root, "Health", -(LabelHeight + ArmorHeight + Gap), HealthHeight, HealthColor, out _healthFill);

            _root.gameObject.SetActive(false);
            context.EnemySpawned.Subscribe(OnSpawned);
            context.EnemyKilled.Subscribe(OnKilled);
        }

        private void OnDestroy()
        {
            if (_context == null)
            {
                return;
            }

            _context.EnemySpawned.Unsubscribe(OnSpawned);
            _context.EnemyKilled.Unsubscribe(OnKilled);
        }

        // A dark backing with a fill that is cut from the left as the value drops.
        private static GameObject Row(RectTransform parent, string name, float top, float height, Color colour, out RectTransform fill)
        {
            var back = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            back.transform.SetParent(parent, false);
            back.color = Backing;
            back.raycastTarget = false;
            RectTransform rect = back.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, top);
            rect.sizeDelta = new Vector2(0f, height);

            var fillImage = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            fillImage.transform.SetParent(rect, false);
            fillImage.color = colour;
            fillImage.raycastTarget = false;
            fill = fillImage.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(Border, Border);
            fill.offsetMax = new Vector2(-Border, -Border);
            return back.gameObject;
        }

        private void OnSpawned(Enemy enemy)
        {
            if (enemy.Definition.IsBoss)
            {
                _boss = enemy;
                _label.text = enemy.Definition.DisplayName.ToUpperInvariant();
            }
        }

        private void OnKilled(Enemy enemy)
        {
            if (enemy == _boss)
            {
                _boss = null;
            }
        }

        private void LateUpdate()
        {
            bool show = _boss != null && _boss.IsAlive && _context.Outcome == LevelOutcome.Running;
            if (_root.gameObject.activeSelf != show)
            {
                _root.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            Fill(_healthFill, _boss.Health.Fraction);
            bool armoured = _boss.Health.ArmorMax > 0f;
            if (_armorRow.activeSelf != armoured)
            {
                _armorRow.SetActive(armoured);
            }

            if (armoured)
            {
                Fill(_armorFill, _boss.Health.ArmorFraction);
            }
        }

        private static void Fill(RectTransform fill, float fraction)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }
    }
}
