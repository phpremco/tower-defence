using Game.Core;
using Game.Hero;
using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Developer story viewer under the options (only with developer mode on): every cutscene and every level's
    /// briefing and dialogues from <see cref="StoryIndex"/>, a page per level, and any of them played on the spot.
    /// A dialogue plays in the same box as in a level and marks nothing seen; a cutscene plays in the intro scene and
    /// returns to the main menu; a briefing shows its text. Controller: up and down pick, A plays, the row at the
    /// bottom pages and goes back; in a dialogue A reads on and B skips it.
    /// </summary>
    public sealed class StoryViewerOverlay : MonoBehaviour
    {
        private const float TitleTop = -36f;
        private const float TitleHeight = 100f;
        private const float PageTop = 130f;
        private const float PageHeight = 56f;
        private const float ColumnTop = 200f;
        private const float BottomMargin = 40f;
        private const float RowSpacing = 10f;
        private const float RowHeight = 56f;
        private const float BriefingWidth = 1300f;
        private const float BriefingHeight = 560f;
        private const int BriefingSize = 30;
        // A press that started or ended a dialogue must not also reach the box or the button under the focus.
        private const float InputGuardSeconds = 0.25f;
        private static readonly Color Dim = new Color(0.04f, 0.05f, 0.08f, 0.92f);

        private OptionsOverlay _owner;
        private GameObject _panel;
        private RectTransform _root;
        private RectTransform _list;
        private RectTransform _column;
        private Text _pageText;
        private GameObject _briefing;
        private Text _briefingText;
        private Button _briefingBack;
        private StoryIndex _index;
        private int _page;
        private Button _lastPlayed;

        private DialoguePlayer _player;
        private DialogueView _view;
        private GameObject _dialogueCanvas;
        private DialogueInput _input;
        private float _guardUntil;
        private bool _playing;

        public void Open(OptionsOverlay owner)
        {
            _owner = owner;
            Services.TryGet(out CampaignCatalog catalog);
            _index = new StoryIndex(catalog);

            _root = UiKit.CreateRoot(transform);
            _panel = _root.parent.gameObject;
            UiKit.CreatePanel(_root, "Dim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Dim);
            _list = NewLayer("List");
            UiKit.CreateFixedText(_list, "Story viewer", UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, TitleTop), new Vector2(1200f, TitleHeight));
            _pageText = UiKit.CreateFixedText(_list, string.Empty, UiKit.BodySize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -PageTop), new Vector2(1400f, PageHeight));
            _column = UiKit.CreateColumn(_list, RowSpacing);
            ColumnFit.Attach(_column, ColumnTop, BottomMargin);
            BuildBriefing();
            BuildDialogue(catalog);
            ShowPage(0);
        }

        private RectTransform NewLayer(string name)
        {
            var layer = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(_root, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            return layer;
        }

        // The level's own dialogue box over its own canvas, drawn above the list; the story is never marked seen.
        private void BuildDialogue(CampaignCatalog catalog)
        {
            _player = new DialoguePlayer(null);
            _player.BusyChanged += OnBusyChanged;
            RectTransform safe = UiKit.CreateRoot(transform, false);
            _dialogueCanvas = safe.parent.gameObject;
            _dialogueCanvas.GetComponent<Canvas>().sortingOrder = 20;
            _view = gameObject.AddComponent<DialogueView>();
            _view.Initialize(safe, _player, new PortraitProvider(Hero(catalog)));
            _player.Presenter = _view;
            if (Services.TryGet(out IInputProvider input))
            {
                _input = new DialogueInput(_player, input, () => Time.unscaledTime < _guardUntil);
            }
        }

        // The face the dialogue box shows for the hero: the first campaign level's hero, else the cutscene's.
        private static HeroDefinition Hero(CampaignCatalog catalog)
        {
            if (catalog == null)
            {
                return null;
            }

            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null && node.Level.Hero != null)
                {
                    return node.Level.Hero;
                }
            }

            CutsceneDefinition opening = catalog.Story != null ? catalog.Story.OpeningCutscene : null;
            return opening != null && opening.Cast != null ? opening.Cast.Hero : null;
        }

        private void BuildBriefing()
        {
            RectTransform layer = NewLayer("Briefing");
            _briefing = layer.gameObject;
            UiKit.CreatePanel(layer, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-BriefingWidth * 0.5f, -BriefingHeight * 0.5f), new Vector2(BriefingWidth * 0.5f, BriefingHeight * 0.5f), Color.white);
            _briefingText = UiKit.CreateFixedText(layer, string.Empty, BriefingSize, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(BriefingWidth - 120f, BriefingHeight - 200f));
            _briefingBack = UiKit.CreateButton(layer, "Back", HideBriefing);
            RectTransform back = _briefingBack.GetComponent<RectTransform>();
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0.5f, 0.5f);
            back.sizeDelta = new Vector2(UiKit.ButtonWidth * 0.5f, UiKit.ButtonHeight);
            back.anchoredPosition = new Vector2(0f, -BriefingHeight * 0.5f + UiKit.ButtonHeight);
            _briefing.SetActive(false);
        }

        private void ShowPage(int page)
        {
            int pages = Mathf.Max(1, _index.Pages.Count);
            _page = (page + pages) % pages;
            for (int i = _column.childCount - 1; i >= 0; i--)
            {
                Destroy(_column.GetChild(i).gameObject);
            }

            Button first = null;
            if (_index.Pages.Count > 0)
            {
                StoryIndex.Page shown = _index.Pages[_page];
                _pageText.text = $"{shown.Title}   ({_page + 1} of {pages})";
                foreach (StoryIndex.Entry entry in shown.Entries)
                {
                    Button button = null;
                    button = UiKit.CreateButton(_column, entry.Label, () => Play(entry, button));
                    button.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                    first = first != null ? first : button;
                }
            }
            else
            {
                _pageText.text = "No story in the campaign data";
            }

            var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(_column, false);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = RowSpacing;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = UiKit.ButtonHeight;
            UiKit.CreateButton(row, "Previous", () => ShowPage(_page - 1));
            UiKit.CreateButton(row, "Next", () => ShowPage(_page + 1));
            UiKit.CreateButton(row, "Back", Close);
            UiKit.Select(first != null ? first : row.GetComponentInChildren<Button>());
        }

        private void Play(StoryIndex.Entry entry, Button from)
        {
            if (_playing || Time.unscaledTime < _guardUntil)
            {
                return;
            }

            _lastPlayed = from;
            switch (entry.Kind)
            {
                case StoryIndex.EntryKind.Cutscene:
                    Services.Register(new CutsceneRequest(entry.Cutscene, CutsceneHandoff.ReturnScene, SceneNames.MainMenu));
                    SceneManager.LoadScene(SceneNames.Intro);
                    break;
                case StoryIndex.EntryKind.Briefing:
                    _list.gameObject.SetActive(false);
                    _briefingText.text = entry.Text;
                    _briefing.SetActive(true);
                    UiKit.Select(_briefingBack);
                    break;
                case StoryIndex.EntryKind.BossArena:
                    // A practice fight: the arena's level with only the boss wave; nothing is paid or saved.
                    BossArenaDefinition arena = _index.Arena;
                    Services.Register(new LevelSession(arena.Level, arena.Difficulty, false, null, arena.WavesFor(entry.Boss), arena.StartingMoney));
                    SceneManager.LoadScene(SceneNames.Level);
                    break;
                case StoryIndex.EntryKind.Dialogue:
                    _playing = true;
                    _guardUntil = Time.unscaledTime + InputGuardSeconds;
                    _list.gameObject.SetActive(false);
                    UiKit.Select(null);
                    _player.Enqueue(entry.Dialogue);
                    _player.TryStart();
                    break;
            }
        }

        private void HideBriefing()
        {
            _briefing.SetActive(false);
            _list.gameObject.SetActive(true);
            UiKit.Select(_lastPlayed);
        }

        private void OnBusyChanged()
        {
            if (_playing && !_player.IsBusy)
            {
                _playing = false;
                _guardUntil = Time.unscaledTime + InputGuardSeconds;
            }
        }

        private void Update()
        {
            _player?.Tick(Time.unscaledDeltaTime);
            // The list comes back once the closing press has passed, so it cannot press the button under the focus.
            if (!_playing && !_list.gameObject.activeSelf && !_briefing.activeSelf && Time.unscaledTime >= _guardUntil)
            {
                _list.gameObject.SetActive(true);
                UiKit.Select(_lastPlayed);
            }
        }

        private void Close()
        {
            _input?.Dispose();
            if (_player != null)
            {
                _player.BusyChanged -= OnBusyChanged;
                _player.Clear();
            }

            Destroy(_panel);
            Destroy(_dialogueCanvas);
            Destroy(_view);

            _owner.ReturnFromStoryViewer();
            Destroy(this);
        }
    }
}
