using System;
using System.Collections.Generic;
using Game.Economy;
using Game.Levels;
using Game.PathObjects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The loadout step on the world map, after the difficulty is chosen: every street weapon the campaign knows
    /// as one row in a scrolling list, with its picture, name, effect badge, price, a line on what it does and a
    /// tick when it is taken. The player takes at most two into the level; they are still bought with money there.
    /// The choice is remembered between levels. The list scrolls with the wheel, by dragging, or along with the
    /// gamepad selection.
    /// </summary>
    public sealed class LoadoutPanelView : MonoBehaviour
    {
        public const int LoadoutSize = 2;

        private const float PanelWidth = 1010f;
        private const float PanelHeight = 940f;
        private const float PanelRight = -50f;
        private const float Side = 40f;
        private const float ListTop = -200f;
        private const float ListBottom = 210f;
        private const float RowHeight = 140f;
        private const float RowGap = 10f;
        private const float ScrollbarWidth = 10f;
        private const float PictureSize = 92f;
        private const float BadgeSize = 26f;
        private const float TickSize = 44f;
        private const float Inset = 12f;
        private const float CostWidth = 130f;
        private const float ButtonWidth = 300f;
        private const float ScrollFollowSpeed = 12f;
        private const int TitleSize = 52;
        private const int SubtitleSize = 28;
        private const int NoteSize = 22;
        private const int NameSize = 26;
        private const int TextSize = 20;
        private static readonly Color PanelTint = new Color(0.08f, 0.14f, 0.26f, 0.97f);
        private static readonly Color ListTint = new Color(0.04f, 0.08f, 0.16f, 0.9f);
        private static readonly Color Muted = new Color(0.75f, 0.82f, 0.95f);
        private static readonly Color Chosen = new Color(0.22f, 0.45f, 0.78f);
        private static readonly Color Idle = new Color(0.12f, 0.2f, 0.34f);
        private static readonly Color Full = new Color(0.09f, 0.13f, 0.22f);
        private static readonly Color Tick = new Color(0.45f, 1f, 0.6f);
        private static readonly Color ScrollbarTint = new Color(0.3f, 0.45f, 0.7f, 0.7f);

        private readonly List<PathObjectDefinition> _chosen = new List<PathObjectDefinition>();
        private readonly List<Row> _rows = new List<Row>();
        private ProgressService _progress;
        private LevelDefinition _level;
        private DifficultyProfile _difficulty;
        private Action _onBack;
        private Action<LevelDefinition, DifficultyProfile, IReadOnlyList<PathObjectDefinition>> _onDeploy;
        private GameObject _panel;
        private ScrollRect _scroll;
        private RectTransform _content;
        private float _viewportHeight;
        private float _scrollTarget = -1f;
        private Text _count;
        private GameObject _lastSelected;

        private struct Row
        {
            public PathObjectDefinition Definition;
            public Button Button;
            public Image Fill;
            public Image Tick;
        }

        public void Open(
            RectTransform root,
            CampaignCatalog catalog,
            LevelDefinition level,
            DifficultyProfile difficulty,
            ProgressService progress,
            Action onBack,
            Action<LevelDefinition, DifficultyProfile, IReadOnlyList<PathObjectDefinition>> onDeploy)
        {
            _progress = progress;
            _level = level;
            _difficulty = difficulty;
            _onBack = onBack;
            _onDeploy = onDeploy;

            List<PathObjectDefinition> catalogue = StreetWeapons(catalog, progress);
            foreach (string id in progress.Loadout)
            {
                PathObjectDefinition saved = catalogue.Find(d => d.name == id);
                if (saved != null && !_chosen.Contains(saved) && _chosen.Count < LoadoutSize)
                {
                    _chosen.Add(saved);
                }
            }

            Image panel = UiKit.CreatePanel(root, "LoadoutPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero, PanelTint);
            panel.raycastTarget = true;
            RectTransform rect = panel.rectTransform;
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rect.anchoredPosition = new Vector2(PanelRight, 0f);
            _panel = panel.gameObject;

            float width = PanelWidth - Side * 2f;
            Text where = UiKit.CreateFixedText(rect, $"{level.DisplayName}   {difficulty.DisplayName}", SubtitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(width, 40f));
            where.color = Muted;
            UiKit.CreateFixedText(rect, "Street weapons", TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(width, 70f));
            Text note = UiKit.CreateFixedText(rect, $"Take up to {LoadoutSize} along; you buy and place them in the level.", NoteSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -146f), new Vector2(width, 34f));
            note.color = Muted;

            BuildList(rect, catalogue);

            _count = UiKit.CreateFixedText(rect, string.Empty, NoteSize, TextAnchor.UpperCenter, new Vector2(0.5f, 0f), new Vector2(0f, ListBottom - 34f), new Vector2(width, 34f));
            Button deploy = BottomButton(rect, "Deploy", Deploy, 24f + UiKit.ButtonHeight + 12f);
            Button back = BottomButton(rect, "Back", Back, 24f);
            Button first = _rows.Count > 0 ? _rows[0].Button : null;
            deploy.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _rows.Count > 0 ? _rows[_rows.Count - 1].Button : null, selectOnDown = back };
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = deploy };
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? _rows[i - 1].Button : null,
                    selectOnDown = i + 1 < _rows.Count ? _rows[i + 1].Button : deploy,
                };
            }

            RefreshRows();
            UiKit.Select(first != null ? first : deploy);
        }

        /// <summary>Leaves the loadout without starting: back to the difficulty choice.</summary>
        public void Back()
        {
            Action onBack = _onBack;
            Close();
            onBack?.Invoke();
        }

        // A gamepad selection moving through the list drags the list along, so the chosen row is always in view.
        private void Update()
        {
            if (_scroll == null)
            {
                return;
            }

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != _lastSelected)
            {
                _lastSelected = selected;
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (_rows[i].Button.gameObject == selected)
                    {
                        float rowTop = i * (RowHeight + RowGap);
                        float maxScroll = Mathf.Max(0f, _content.sizeDelta.y - _viewportHeight);
                        _scrollTarget = Mathf.Clamp(rowTop - (_viewportHeight - RowHeight) * 0.5f, 0f, maxScroll);
                    }
                }
            }

            if (_scrollTarget >= 0f)
            {
                Vector2 position = _content.anchoredPosition;
                position.y = Mathf.Lerp(position.y, _scrollTarget, 1f - Mathf.Exp(-ScrollFollowSpeed * Time.unscaledDeltaTime));
                _content.anchoredPosition = position;
                if (Mathf.Abs(position.y - _scrollTarget) < 0.5f)
                {
                    _scrollTarget = -1f;
                }
            }
        }

        private void Deploy()
        {
            var ids = new List<string>(_chosen.Count);
            foreach (PathObjectDefinition definition in _chosen)
            {
                ids.Add(definition.name);
            }

            _progress.SetLoadout(ids);
            Action<LevelDefinition, DifficultyProfile, IReadOnlyList<PathObjectDefinition>> onDeploy = _onDeploy;
            PathObjectDefinition[] chosen = _chosen.ToArray();
            Close();
            onDeploy?.Invoke(_level, _difficulty, chosen);
        }

        private void Close()
        {
            if (_panel != null)
            {
                Destroy(_panel);
            }

            Destroy(this);
        }

        /// <summary>True when the player has unlocked any street weapon to choose from; without one the panel is skipped.</summary>
        public static bool HasChoice(CampaignCatalog catalog, ProgressService progress) => StreetWeapons(catalog, progress).Count > 0;

        // Every street weapon any level offers and a first clear has unlocked, in the order the campaign introduces them, each once.
        private static List<PathObjectDefinition> StreetWeapons(CampaignCatalog catalog, ProgressService progress)
        {
            var all = new List<PathObjectDefinition>();
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                foreach (PathObjectDefinition definition in node.Level.PathObjects)
                {
                    if (definition != null && !all.Contains(definition) && progress.IsPathObjectUnlocked(definition.name))
                    {
                        all.Add(definition);
                    }
                }
            }

            return all;
        }

        // A masked viewport with a vertical list inside and a slim scrollbar on the right.
        private void BuildList(RectTransform panel, List<PathObjectDefinition> catalogue)
        {
            Image viewportImage = UiKit.CreatePanel(panel, "Viewport", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(Side, ListBottom), new Vector2(-Side - ScrollbarWidth - 6f, ListTop), ListTint);
            viewportImage.raycastTarget = true;
            RectTransform viewport = viewportImage.rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();
            _viewportHeight = PanelHeight + ListTop - ListBottom;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            _content = contentGo.GetComponent<RectTransform>();
            _content.SetParent(viewport, false);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            float rowWidth = PanelWidth - Side * 2f - ScrollbarWidth - 6f - Inset * 2f;
            float contentHeight = catalogue.Count * RowHeight + Mathf.Max(0, catalogue.Count - 1) * RowGap + Inset * 2f;
            _content.sizeDelta = new Vector2(0f, contentHeight);
            _content.anchoredPosition = Vector2.zero;

            for (int i = 0; i < catalogue.Count; i++)
            {
                CreateRow(catalogue[i], rowWidth, -Inset - i * (RowHeight + RowGap));
            }

            _scroll = panel.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewport;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 40f;
            _scroll.verticalScrollbar = BuildScrollbar(panel);
            _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private Scrollbar BuildScrollbar(RectTransform panel)
        {
            Image track = UiKit.CreatePanel(panel, "Scrollbar", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-Side - ScrollbarWidth, ListBottom), new Vector2(-Side, ListTop), new Color(0f, 0f, 0f, 0.35f));
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            Image handle = UiKit.CreatePanel(track.rectTransform, "Handle", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, ScrollbarTint);
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            return scrollbar;
        }

        // One row: picture and badge on the left, name with the effect and price beside it, what it does below,
        // and a tick at the right end when the weapon is taken along.
        private void CreateRow(PathObjectDefinition definition, float width, float y)
        {
            Button button = UiKit.CreateButton(_content, string.Empty, () => Toggle(definition));
            Destroy(button.GetComponent<LayoutElement>());
            Text placeholder = button.GetComponentInChildren<Text>();
            if (placeholder != null)
            {
                Destroy(placeholder.gameObject);
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, RowHeight);
            rect.anchoredPosition = new Vector2(0f, y);

            var pictureGo = new GameObject("Picture", typeof(RectTransform));
            pictureGo.transform.SetParent(rect, false);
            var picture = pictureGo.AddComponent<Image>();
            picture.raycastTarget = false;
            picture.preserveAspect = true;
            picture.enabled = false;
            RectTransform pictureRect = picture.rectTransform;
            pictureRect.anchorMin = new Vector2(0f, 0.5f);
            pictureRect.anchorMax = new Vector2(0f, 0.5f);
            pictureRect.pivot = new Vector2(0f, 0.5f);
            pictureRect.sizeDelta = new Vector2(PictureSize, PictureSize);
            pictureRect.anchoredPosition = new Vector2(Inset, 0f);
            if (definition.Look != PathObjectLook.None)
            {
                ModelIconRenderer.Request(definition, stage => PathObjectModels.Build(stage, definition), sprite =>
                {
                    if (picture != null)
                    {
                        picture.sprite = sprite;
                        picture.enabled = true;
                    }
                });
            }

            PathObjectBadge.For(definition.Effect, out IconType icon, out Color colour);
            float textLeft = Inset + PictureSize + Inset + 4f;
            float textWidth = width - textLeft - CostWidth - TickSize - Inset * 2f;

            Text name = UiKit.CreateFixedText(rect, definition.DisplayName, NameSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(textLeft, -Inset), new Vector2(textWidth, 32f));
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            Image badge = Icon(rect, icon, colour, BadgeSize);
            badge.rectTransform.anchorMin = new Vector2(0f, 1f);
            badge.rectTransform.anchorMax = new Vector2(0f, 1f);
            badge.rectTransform.pivot = new Vector2(0f, 1f);
            badge.rectTransform.anchoredPosition = new Vector2(textLeft, -Inset - 36f);
            Text effect = UiKit.CreateFixedText(rect, PathObjectBadge.Name(definition.Effect), TextSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(textLeft + BadgeSize + 8f, -Inset - 38f), new Vector2(textWidth - BadgeSize - 8f, 26f));
            effect.color = colour;
            effect.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text description = UiKit.CreateFixedText(rect, definition.Description, TextSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(textLeft, -Inset - 66f), new Vector2(textWidth + CostWidth, RowHeight - Inset - 66f));
            description.color = Muted;
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;

            Text cost = UiKit.CreateFixedText(rect, $"{definition.Cost} money", NameSize, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-Inset - TickSize - Inset, -Inset), new Vector2(CostWidth, 32f));
            cost.horizontalOverflow = HorizontalWrapMode.Overflow;

            Image tick = Icon(rect, IconType.Check, Tick, TickSize);
            tick.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            tick.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            tick.rectTransform.pivot = new Vector2(1f, 0.5f);
            tick.rectTransform.anchoredPosition = new Vector2(-Inset, 0f);

            _rows.Add(new Row { Definition = definition, Button = button, Fill = button.GetComponent<Image>(), Tick = tick });
        }

        private static Image Icon(RectTransform parent, IconType type, Color colour, float size)
        {
            var go = new GameObject(type.ToString(), typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = IconFactory.Get(type);
            image.color = colour;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        private static Button BottomButton(RectTransform panel, string label, UnityEngine.Events.UnityAction onClick, float bottom)
        {
            Button button = UiKit.CreateButton(panel, label, onClick);
            Destroy(button.GetComponent<LayoutElement>());
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(ButtonWidth, UiKit.ButtonHeight);
            rect.anchoredPosition = new Vector2(0f, bottom);
            return button;
        }

        private void Toggle(PathObjectDefinition definition)
        {
            if (_chosen.Contains(definition))
            {
                _chosen.Remove(definition);
            }
            else if (_chosen.Count < LoadoutSize)
            {
                _chosen.Add(definition);
            }

            RefreshRows();
        }

        private void RefreshRows()
        {
            bool full = _chosen.Count >= LoadoutSize;
            for (int i = 0; i < _rows.Count; i++)
            {
                bool chosen = _chosen.Contains(_rows[i].Definition);
                _rows[i].Fill.color = chosen ? Chosen : full ? Full : Idle;
                _rows[i].Tick.enabled = chosen;
            }

            _count.text = _chosen.Count == LoadoutSize ? $"{_chosen.Count} of {LoadoutSize} chosen: ready to deploy" : $"{_chosen.Count} of {LoadoutSize} chosen";
        }
    }
}
