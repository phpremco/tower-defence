using System.Collections.Generic;
using System.Text;
using Game.Core;
using Game.Economy;
using Game.Enemies;
using Game.Levels;
using Game.Platform;
using Game.Towers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The codex: a book the player opens from the HUD that holds the game while it is read. One page lists the
    /// tower combinations (which two towers stand side by side, what comes out, what it costs and what it gains),
    /// the other every creature with its numbers and its skill. A list on the left, the chosen entry drawn and
    /// described on the right; the stick walks the list, Cancel closes the book.
    /// </summary>
    public sealed class CodexView : MonoBehaviour
    {
        private const float PanelWidth = 1640f;
        private const float PanelHeight = 920f;
        private const float Padding = 28f;
        private const float TabWidth = 260f;
        private const float TabHeight = 56f;
        private const float ListWidth = 520f;
        private const float RowHeight = 68f;
        private const float RowGap = 6f;
        private const float PreviewSize = 230f;
        private const float BigPreviewSize = 360f;
        private const float DetailGap = 24f;
        private const int TitleSize = 44;
        private const int TabSize = 26;
        private const int RowNameSize = 24;
        private const int RowNoteSize = 18;
        private const int DetailNameSize = 36;
        private const int DetailSize = 24;
        private const int OperatorSize = 48;
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color PanelTint = new Color(0.1f, 0.16f, 0.3f, 0.98f);
        private static readonly Color ListTint = new Color(0.05f, 0.09f, 0.18f, 0.9f);
        private static readonly Color RowTint = new Color(0.16f, 0.26f, 0.42f);
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color Muted = new Color(0.6f, 0.7f, 0.85f);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.4f);
        private static readonly Color Warn = new Color(1f, 0.55f, 0.45f);

        private enum Page
        {
            Recipes,
            Enemies,
        }

        // Tells the view when the stick lands on a row, so the detail follows the selection, not only clicks.
        private sealed class SelectRelay : MonoBehaviour, ISelectHandler
        {
            public System.Action Selected;

            public void OnSelect(BaseEventData eventData) => Selected?.Invoke();
        }

        private LevelContext _context;
        private TimeController _time;
        private IInputProvider _input;
        private CodexCatalog _catalog;
        private RectTransform _safeRoot;
        private GameObject _root;
        private RectTransform _detail;
        private ScrollRect _scroll;
        private RectTransform _content;
        private Button _recipesTab;
        private Button _enemiesTab;
        private Button _close;
        private Text _detailName;
        private Text _detailText;
        private readonly List<ModelPreview> _previews = new List<ModelPreview>();
        private readonly List<EnemyDefinition> _shownEnemies = new List<EnemyDefinition>();
        private readonly List<Text> _operators = new List<Text>();
        private readonly List<Button> _rows = new List<Button>();
        private readonly StringBuilder _builder = new StringBuilder();
        private Page _page = Page.Recipes;
        private int _selected = -1;
        private bool _open;

        public bool Available => _catalog != null && (_catalog.Recipes.Count > 0 || _catalog.Enemies.Count > 0);

        /// <summary>True while the book is open and holds the game.</summary>
        public bool IsOpen => _open;

        public void Initialize(LevelContext context, RectTransform safeRoot, TimeController time, IInputProvider input)
        {
            _context = context;
            _safeRoot = safeRoot;
            _time = time;
            _input = input;
            Services.TryGet(out _catalog);
            _input.Cancelled += OnCancelled;
            _context.Changed.Subscribe(OnContextChanged);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Cancelled -= OnCancelled;
            }

            _context?.Changed.Unsubscribe(OnContextChanged);
        }

        public void Open()
        {
            // A conversation owns the screen until it ends; the book waits.
            if (!Available || _open || _context.StoryBusy || _context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            if (_root == null)
            {
                Build();
            }

            // Built once, after every other panel, and raised again on each opening so it draws over them all.
            _root.transform.SetAsLastSibling();
            _root.SetActive(true);
            _open = true;
            _time.Hold(this);
            ShowPage(_page);
        }

        public void Close()
        {
            if (!_open)
            {
                return;
            }

            _open = false;
            _root.SetActive(false);
            _time.Release(this);
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnCancelled()
        {
            if (_open)
            {
                Close();
            }
        }

        // The level ending takes the book away, whatever page it was on.
        private void OnContextChanged()
        {
            if (_open && _context.Outcome != LevelOutcome.Running)
            {
                Close();
            }
        }

        private void Build()
        {
            Image dim = UiKit.CreatePanel(_safeRoot, "CodexDim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Dim);
            dim.raycastTarget = true;
            _root = dim.gameObject;

            Image panel = UiKit.CreatePanel(dim.rectTransform, "CodexPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, PanelTint);
            panel.raycastTarget = true;
            RectTransform rect = panel.rectTransform;
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Text title = UiKit.CreateFixedText(rect, "Codex", TitleSize, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(Padding, -Padding), new Vector2(300f, TabHeight));
            title.color = Gold;

            _recipesTab = Tab(rect, "Combinations", 0, () => ShowPage(Page.Recipes));
            _enemiesTab = Tab(rect, "Enemies", 1, () => ShowPage(Page.Enemies));
            _close = UiKit.CreateButton(rect, "Close", Close);
            Destroy(_close.GetComponent<LayoutElement>());
            RectTransform closeRect = _close.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-Padding, -Padding);
            closeRect.sizeDelta = new Vector2(TabWidth * 0.7f, TabHeight);

            float top = Padding + TabHeight + Padding * 0.5f;
            BuildList(rect, top);
            BuildDetail(rect, top);
        }

        private Button Tab(RectTransform parent, string label, int index, UnityEngine.Events.UnityAction onClick)
        {
            Button tab = UiKit.CreateButton(parent, label, onClick);
            Destroy(tab.GetComponent<LayoutElement>());
            RectTransform rect = tab.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Padding + 320f + index * (TabWidth + 16f), -Padding);
            rect.sizeDelta = new Vector2(TabWidth, TabHeight);
            tab.GetComponentInChildren<Text>().fontSize = TabSize;
            return tab;
        }

        // A scrolling column of rows down the left: the stick walks them, the viewport follows the selection.
        private void BuildList(RectTransform panel, float top)
        {
            Image back = UiKit.CreatePanel(panel, "List", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(Padding, Padding), new Vector2(Padding + ListWidth, -top), ListTint);
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(back.transform, false);
            var viewport = viewportGo.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8f, 8f);
            viewport.offsetMax = new Vector2(-8f, -8f);
            viewportGo.AddComponent<RectMask2D>();
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.clear;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewport, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.offsetMin = Vector2.zero;
            _content.offsetMax = Vector2.zero;
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowGap;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scroll = back.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewport;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 40f;
        }

        private void BuildDetail(RectTransform panel, float top)
        {
            var go = new GameObject("Detail", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            _detail = go.GetComponent<RectTransform>();
            _detail.anchorMin = new Vector2(0f, 0f);
            _detail.anchorMax = new Vector2(1f, 1f);
            _detail.offsetMin = new Vector2(Padding + ListWidth + DetailGap, Padding);
            _detail.offsetMax = new Vector2(-Padding, -top);

            for (int i = 0; i < 3; i++)
            {
                ModelPreview preview = ModelPreview.Create(_detail, new Vector2(PreviewSize, PreviewSize));
                RectTransform previewRect = preview.GetComponent<RectTransform>();
                previewRect.anchorMin = new Vector2(0f, 1f);
                previewRect.anchorMax = new Vector2(0f, 1f);
                previewRect.pivot = new Vector2(0f, 1f);
                _previews.Add(preview);
            }

            for (int i = 0; i < 2; i++)
            {
                Text op = UiKit.CreateFixedText(_detail, i == 0 ? "+" : "=", OperatorSize, TextAnchor.MiddleCenter, new Vector2(0f, 1f), Vector2.zero, new Vector2(60f, PreviewSize));
                op.color = Gold;
                _operators.Add(op);
            }

            _detailName = UiKit.CreateFixedText(_detail, string.Empty, DetailNameSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), Vector2.zero, new Vector2(PanelWidth - ListWidth - Padding * 3f - DetailGap, 50f));
            _detailName.color = Gold;
            _detailText = UiKit.CreateFixedText(_detail, string.Empty, DetailSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), Vector2.zero, new Vector2(PanelWidth - ListWidth - Padding * 3f - DetailGap, 420f));
            _detailText.color = TextColor;
            _detailText.resizeTextForBestFit = false;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void ShowPage(Page page)
        {
            _page = page;
            _recipesTab.interactable = page != Page.Recipes;
            _enemiesTab.interactable = page != Page.Enemies;
            foreach (Button row in _rows)
            {
                Destroy(row.gameObject);
            }

            _rows.Clear();
            ListShownEnemies();
            int count = page == Page.Recipes ? _catalog.Recipes.Count : _shownEnemies.Count;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                string name;
                string note;
                if (page == Page.Recipes)
                {
                    CombineRecipe recipe = _catalog.Recipes[i];
                    name = recipe.Result.DisplayName;
                    note = $"{recipe.First.DisplayName} + {recipe.Second.DisplayName}";
                }
                else
                {
                    EnemyDefinition enemy = _shownEnemies[i];
                    name = enemy.DisplayName;
                    note = enemy.IsBoss ? "Boss" : enemy.Flies ? "Air" : "Ground";
                }

                _rows.Add(Row(name, note, () => Show(index)));
            }

            // Up and down walk the list; the tabs and the close button hang above the first row.
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? _rows[i - 1] : _close,
                    selectOnDown = i + 1 < _rows.Count ? _rows[i + 1] : null,
                };
            }

            _close.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = _rows.Count > 0 ? _rows[0] : null, selectOnLeft = _enemiesTab };
            _enemiesTab.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _recipesTab, selectOnRight = _close, selectOnDown = _rows.Count > 0 ? _rows[0] : null };
            _recipesTab.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _enemiesTab, selectOnDown = _rows.Count > 0 ? _rows[0] : null };

            // The rows were only just added: lay them out now, so the first scroll knows the list's height.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            Canvas.ForceUpdateCanvases();
            _selected = -1;
            if (_rows.Count > 0)
            {
                Show(0);
                UiKit.Select(_rows[0]);
            }
            else
            {
                _detailName.text = string.Empty;
                _detailText.text = "Nothing known yet.";
            }
        }

        private Button Row(string name, string note, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiKit.CreateIconButton(_content, null, RowHeight, onClick);
            Destroy(button.GetComponent<LayoutElement>());
            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = RowHeight;
            element.minHeight = RowHeight;
            Destroy(button.transform.Find("Icon").gameObject);
            UiKit.UseTint(button);
            ColorBlock colors = button.colors;
            colors.normalColor = RowTint;
            colors.highlightedColor = Color.Lerp(RowTint, Color.white, 0.35f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.white;
            button.colors = colors;
            button.gameObject.AddComponent<SelectRelay>().Selected = () => onClick();

            RectTransform rect = button.GetComponent<RectTransform>();
            Text label = UiKit.CreateFixedText(rect, name, RowNameSize, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(16f, -6f), new Vector2(ListWidth - 48f, 34f));
            label.color = TextColor;
            Text sub = UiKit.CreateFixedText(rect, note, RowNoteSize, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(16f, -38f), new Vector2(ListWidth - 48f, 24f));
            sub.color = Muted;
            return button;
        }

        // A boss is described only once the player has met it (Docs/BOSSES.md); every other creature is listed from the start.
        private void ListShownEnemies()
        {
            _shownEnemies.Clear();
            Services.TryGet(out ProgressService progress);
            foreach (EnemyDefinition enemy in _catalog.Enemies)
            {
                if (enemy != null && (!enemy.IsBoss || progress == null || progress.HasSeenEnemy(enemy.name)))
                {
                    _shownEnemies.Add(enemy);
                }
            }
        }

        private void Show(int index)
        {
            if (index == _selected)
            {
                return;
            }

            _selected = index;
            ScrollTo(index);
            if (_page == Page.Recipes)
            {
                ShowRecipe(_catalog.Recipes[index]);
            }
            else
            {
                ShowEnemy(_shownEnemies[index]);
            }
        }

        // Keeps the chosen row inside the viewport when the stick walks past its edge.
        private void ScrollTo(int index)
        {
            float rowTop = index * (RowHeight + RowGap);
            float rowBottom = rowTop + RowHeight;
            float viewHeight = _scroll.viewport.rect.height;
            float contentHeight = Mathf.Max(viewHeight, _rows.Count * (RowHeight + RowGap));
            float scrollable = contentHeight - viewHeight;
            if (scrollable <= 0f)
            {
                return;
            }

            float offset = scrollable * (1f - _scroll.verticalNormalizedPosition);
            if (rowTop < offset)
            {
                offset = rowTop;
            }
            else if (rowBottom > offset + viewHeight)
            {
                offset = rowBottom - viewHeight;
            }

            _scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(offset / scrollable);
        }

        private void ShowRecipe(CombineRecipe recipe)
        {
            float x = 0f;
            for (int i = 0; i < 3; i++)
            {
                ModelPreview preview = _previews[i];
                preview.gameObject.SetActive(true);
                RectTransform rect = preview.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(PreviewSize, PreviewSize);
                rect.anchoredPosition = new Vector2(x, 0f);
                preview.ShowTower(i == 0 ? recipe.First : i == 1 ? recipe.Second : recipe.Result);
                x += PreviewSize;
                if (i < 2)
                {
                    _operators[i].gameObject.SetActive(true);
                    _operators[i].rectTransform.anchoredPosition = new Vector2(x, 0f);
                    x += 60f;
                }
            }

            float textTop = -(PreviewSize + DetailGap);
            PlaceText(0f, textTop);
            _detailName.text = $"{recipe.First.DisplayName} + {recipe.Second.DisplayName} = {recipe.Result.DisplayName}";
            _detailText.text = DescribeRecipe(recipe);
        }

        private string DescribeRecipe(CombineRecipe recipe)
        {
            TowerDefinition a = recipe.First;
            TowerDefinition b = recipe.Second;
            TowerDefinition r = recipe.Result;
            _builder.Clear();
            _builder.Append("Build the two towers on neighbouring cells, open one of them and choose Combine. Cost ").Append(recipe.Cost).Append(" on top of what both towers are worth.\n");
            _builder.Append("The hybrid takes the lower of the two levels: a level 2 and a level 1 make a level 1 hybrid, which upgrades on from there.\n\n");
            for (int level = 0; level <= r.MaxLevelIndex; level++)
            {
                _builder.Append("Level ").Append(level + 1).Append(":  damage per second ")
                    .Append(Dps(a, level)).Append(" + ").Append(Dps(b, level)).Append("  >  ").Append(Dps(r, level));
                if (level > 0)
                {
                    _builder.Append("   (upgrade ").Append(r.GetLevel(level).Cost).Append(')');
                }

                _builder.Append('\n');
            }

            _builder.Append("\nRange ").Append(a.GetLevel(0).Range.ToString("0.0")).Append(" and ").Append(b.GetLevel(0).Range.ToString("0.0")).Append("  >  ").Append(r.GetLevel(0).Range.ToString("0.0"));
            _builder.Append("\nHits: ").Append(Targets(r));
            if (r.SplashRadius > 0f)
            {
                _builder.Append("   Splash ").Append(r.SplashRadius.ToString("0.0"));
            }

            if (r.SlowFactor < 1f)
            {
                _builder.Append("   Slows to ").Append(Mathf.RoundToInt(r.SlowFactor * 100f)).Append('%');
            }

            _builder.Append("\n\nGain: ").Append(Gain(a, b, r));
            return _builder.ToString();
        }

        private static string Dps(TowerDefinition tower, int level)
        {
            TowerLevel at = tower.GetLevel(Mathf.Min(level, tower.MaxLevelIndex));
            return at.FireInterval > 0f ? Mathf.RoundToInt(at.Damage / at.FireInterval).ToString() : "0";
        }

        private static string Targets(TowerDefinition tower)
        {
            bool ground = (tower.Targets & TargetLayers.Ground) != 0;
            bool air = (tower.Targets & TargetLayers.Air) != 0;
            return ground && air ? "ground and air" : air ? "air only" : "ground only";
        }

        // One line on what the player buys with the recipe: more damage in one cell, and whatever the parents lacked.
        private static string Gain(TowerDefinition a, TowerDefinition b, TowerDefinition r)
        {
            float parents = DpsValue(a, 0) + DpsValue(b, 0);
            int percent = parents > 0f ? Mathf.RoundToInt((DpsValue(r, 0) / parents - 1f) * 100f) : 0;
            var text = new StringBuilder();
            text.Append(percent >= 0 ? "+" : string.Empty).Append(percent).Append("% damage over both parents, in one cell");
            bool parentsBoth = ((a.Targets | b.Targets) & TargetLayers.Air) != 0 && ((a.Targets | b.Targets) & TargetLayers.Ground) != 0;
            bool eachBoth = (a.Targets & TargetLayers.Air) != 0 && (a.Targets & TargetLayers.Ground) != 0;
            if ((r.Targets & TargetLayers.Air) != 0 && (r.Targets & TargetLayers.Ground) != 0 && (!parentsBoth || !eachBoth))
            {
                text.Append(", hits ground and air");
            }

            if (r.SplashRadius > Mathf.Max(a.SplashRadius, b.SplashRadius))
            {
                text.Append(", wider splash");
            }

            if (r.SlowFactor < 1f)
            {
                text.Append(", slows what it hits");
            }

            text.Append('.');
            return text.ToString();
        }

        private static float DpsValue(TowerDefinition tower, int level)
        {
            TowerLevel at = tower.GetLevel(Mathf.Min(level, tower.MaxLevelIndex));
            return at.FireInterval > 0f ? at.Damage / at.FireInterval : 0f;
        }

        private void ShowEnemy(EnemyDefinition enemy)
        {
            for (int i = 1; i < 3; i++)
            {
                // Cleared, not just hidden: every preview rig stands on the same spot behind the scenes.
                _previews[i].ShowNothing();
                _previews[i].gameObject.SetActive(false);
            }

            _previews[0].gameObject.SetActive(true);

            foreach (Text op in _operators)
            {
                op.gameObject.SetActive(false);
            }

            RectTransform rect = _previews[0].GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(BigPreviewSize, BigPreviewSize);
            rect.anchoredPosition = Vector2.zero;
            _previews[0].ShowEnemy(enemy);

            PlaceText(BigPreviewSize + DetailGap, 0f);
            _detailName.text = enemy.DisplayName + (enemy.IsBoss ? "  (boss)" : string.Empty);
            _detailText.text = DescribeEnemy(enemy);
        }

        // The text fills whatever width is left of the detail pane beside the pictures.
        private void PlaceText(float left, float top)
        {
            float width = _detail.rect.width - left;
            _detailName.rectTransform.anchoredPosition = new Vector2(left, top);
            _detailName.rectTransform.sizeDelta = new Vector2(width, 50f);
            _detailText.rectTransform.anchoredPosition = new Vector2(left, top - 56f);
            _detailText.rectTransform.sizeDelta = new Vector2(width, 420f);
        }

        // A continent boss: its phases, its signature, how to see it coming and how to beat it, in plain words.
        private void DescribeBoss(BossDefinition boss)
        {
            if (boss == null)
            {
                return;
            }

            _builder.Append("\n\nPhases: ");
            for (int i = 0; i < boss.Phases.Length; i++)
            {
                _builder.Append(i > 0 ? ", " : string.Empty).Append(boss.Phases[i].Name);
            }

            _builder.Append("\nSignature: ").Append(boss.SignatureText);
            _builder.Append("\nSee it coming: ").Append(boss.TelegraphText);
            _builder.Append("\nHow to beat it: ").Append(boss.CounterText);
        }

        private string DescribeEnemy(EnemyDefinition enemy)
        {
            _builder.Clear();
            _builder.Append(enemy.Flies ? "Flies: only weapons that hit the air reach it." : "Walks the road: ground weapons hit it.").Append('\n');
            _builder.Append("Health ").Append(Mathf.RoundToInt(enemy.MaxHealth));
            if (enemy.HasArmor)
            {
                _builder.Append("   Armour ").Append(Mathf.RoundToInt(enemy.Armor)).Append(" (takes ").Append(Mathf.RoundToInt(enemy.ArmorReduction * 100f)).Append("% less until it is chipped away)");
            }

            if (enemy.HasPlating)
            {
                _builder.Append("   Plating ").Append(Mathf.RoundToInt(enemy.Plating)).Append(" (every hit loses that much, at least ").Append(Mathf.RoundToInt(enemy.PlatingFloor * 100f)).Append("% gets through: heavy shots pass it)");
            }

            _builder.Append("\nSpeed ").Append(enemy.Speed.ToString("0.0")).Append("   Bounty ").Append(enemy.Reward).Append("   Base damage ").Append(enemy.BaseDamage).Append('\n');

            // A continent boss's own mechanic decides what it does to towers; the signature below says it.
            string towers = enemy.AttackDamage > 0f ? $"damages them ({enemy.AttackDamage:0} per hit)" : enemy.Boss != null && enemy.Boss.Signature != BossSignature.None ? "see its signature" : "leaves them alone";
            _builder.Append("Towers: ").Append(towers);
            _builder.Append("   Gates and hero: ").Append(enemy.BlockadeDamage > 0f ? $"{enemy.BlockadeDamage:0} per hit" : "no");
            if (enemy.GateStrikes > 0)
            {
                _builder.Append("   Strikes a gate ").Append(enemy.GateStrikes).Append(" times in passing");
            }

            _builder.Append('\n');
            if (enemy.Rolls)
            {
                _builder.Append("Curls up and rolls at ").Append(enemy.RollSpeedMultiplier.ToString("0.0")).Append("x speed.\n");
            }

            if (enemy.LeavesTrail)
            {
                _builder.Append("Leaves a trail for ").Append(Mathf.RoundToInt(enemy.TrailSeconds)).Append("s that hurries others").Append(enemy.TrailDamagePerSecond > 0f ? " and burns the hero and gates" : string.Empty).Append(".\n");
            }

            if (enemy.Rages)
            {
                _builder.Append("Rages at ").Append(enemy.RageSpeedMultiplier.ToString("0.0")).Append("x speed when a pack mate dies nearby.\n");
            }

            if (enemy.Element != Element.None)
            {
                _builder.Append("Element: ").Append(enemy.Element).Append(".\n");
            }

            if (enemy.IsBoss && enemy.Abilities.Length > 0)
            {
                _builder.Append("Powers: ");
                for (int i = 0; i < enemy.Abilities.Length; i++)
                {
                    _builder.Append(i > 0 ? ", " : string.Empty).Append(enemy.Abilities[i] != null ? enemy.Abilities[i].DisplayName : "?");
                }

                _builder.Append(".\n");
            }

            if (!string.IsNullOrEmpty(enemy.Description))
            {
                _builder.Append('\n').Append(enemy.Description);
            }

            DescribeBoss(enemy.Boss);

            return _builder.ToString();
        }
    }
}
