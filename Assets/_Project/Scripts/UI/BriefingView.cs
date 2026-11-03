using Game.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The story briefing before the first wave: the region, the level and its piece of the story on a panel, with
    /// time held until the player deploys. Only shown when the session asks for it, so tests and tools skip it.
    /// </summary>
    public sealed class BriefingView : MonoBehaviour
    {
        private const float PanelWidth = 1100f;
        private const float PanelHeight = 560f;
        private const float TitleOnlyHeight = 320f;
        private const float ButtonWidth = 420f;
        private const int RegionSize = 30;
        private const int TitleSize = 56;
        private const int BodySize = 30;
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color Tint = new Color(0.08f, 0.14f, 0.26f, 0.97f);
        private static readonly Color Muted = new Color(0.75f, 0.82f, 0.95f);

        private LevelContext _context;
        private TimeController _time;
        private GameObject _panel;

        public void Initialize(LevelContext context, RectTransform root, TimeController time)
        {
            _context = context;
            _time = time;
            if (!context.BriefingPending)
            {
                Destroy(this);
                return;
            }

            LevelDefinition level = context.Level;
            Image dim = UiKit.CreatePanel(root, "BriefingDim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Dim);
            dim.raycastTarget = true;
            _panel = dim.gameObject;

            Image panel = UiKit.CreatePanel(dim.rectTransform, "Briefing", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Tint);
            RectTransform rect = panel.rectTransform;
            // One source per level: when a dialogue opens the level, it tells the story and the card is only the title.
            bool told = context.StoryEnabled && HasOpeningDialogue(level);
            rect.sizeDelta = new Vector2(PanelWidth, told ? TitleOnlyHeight : PanelHeight);
            float width = PanelWidth - 100f;

            Text region = UiKit.CreateFixedText(rect, level.RegionName, RegionSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(width, 40f));
            region.color = Muted;
            UiKit.CreateFixedText(rect, level.DisplayName, TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(width, 70f));
            if (!told)
            {
                Text body = UiKit.CreateFixedText(rect, level.Briefing, BodySize, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(width, 240f));
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            Button deploy = UiKit.CreateButton(rect, "Deploy", Deploy);
            Destroy(deploy.GetComponent<LayoutElement>());
            RectTransform deployRect = deploy.GetComponent<RectTransform>();
            deployRect.anchorMin = new Vector2(0.5f, 0f);
            deployRect.anchorMax = new Vector2(0.5f, 0f);
            deployRect.pivot = new Vector2(0.5f, 0f);
            deployRect.sizeDelta = new Vector2(ButtonWidth, UiKit.ButtonHeight);
            deployRect.anchoredPosition = new Vector2(0f, 30f);

            _time.Hold(this);
            UiKit.Select(deploy);
        }

        private static bool HasOpeningDialogue(LevelDefinition level)
        {
            foreach (StoryTrigger trigger in level.StoryTriggers)
            {
                if (trigger.Kind == StoryTriggerKind.LevelStart && trigger.Dialogue != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void Deploy()
        {
            _context.BriefingPending = false;
            _time.Release(this);
            Destroy(_panel);
            Destroy(this);
        }
    }
}
