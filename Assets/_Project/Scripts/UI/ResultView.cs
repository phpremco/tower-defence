using System.Collections;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// What the player sees when a level ends: a victory or defeat panel with the stars earned, the rewards and
    /// any new weapon. Nothing happens until the player chooses: upgrades, play again or back to the map.
    /// </summary>
    public sealed class ResultView : MonoBehaviour
    {
        private const float PanelWidth = 1180f;
        private const float PanelHeight = 860f;
        private const float StarSize = 130f;
        private const float StarGap = 20f;
        private const float StarRevealSeconds = 0.45f;
        private const float PopSeconds = 0.3f;
        private const float PreviewSize = 230f;
        private const int UnlockLineSize = 30;
        private const float ButtonWidth = 340f;
        private const float DefeatDelaySeconds = 3.2f;
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color VictoryTint = new Color(0.12f, 0.26f, 0.5f, 0.98f);
        private static readonly Color DefeatTint = new Color(0.4f, 0.12f, 0.14f, 0.98f);
        private static readonly Color StarOn = new Color(1f, 0.85f, 0.25f);
        private static readonly Color StarOff = new Color(0.15f, 0.18f, 0.27f);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
        private static readonly Color Muted = new Color(0.75f, 0.82f, 0.95f);

        private LevelContext _context;
        private RectTransform _safeRoot;
        private GameObject _root;
        private bool _shown;
        private bool _waiting;
        // Game time until which a victory waits for a fallen boss's end (BossDefinition.ResultHoldSeconds).
        private float _holdUntil;

        public void Initialize(LevelContext context, RectTransform safeRoot)
        {
            _context = context;
            _safeRoot = safeRoot;
            _context.Changed.Subscribe(Refresh);
            _context.EnemyKilled.Subscribe(OnKilled);
        }

        private void OnDestroy()
        {
            _context?.Changed.Unsubscribe(Refresh);
            _context?.EnemyKilled.Unsubscribe(OnKilled);
        }

        private void Refresh()
        {
            // A victory dialogue plays first; the story raises Changed again once it is done.
            if (_shown || _waiting || _context.Outcome == LevelOutcome.Running || _context.Result == null || _context.StoryBusy)
            {
                return;
            }

            // A defeat first shows the base going up before the panel covers it.
            if (!_context.Result.Won)
            {
                _waiting = true;
                Time.timeScale = 1f;
                StartCoroutine(ShowAfter(DefeatDelaySeconds));
                return;
            }

            // A boss whose end is a sight of its own (the Sealed One's white Release) plays it before the panel covers it.
            if (Time.time < _holdUntil)
            {
                _waiting = true;
                StartCoroutine(RefreshAfterHold());
                return;
            }

            Show();
        }

        private void OnKilled(Enemy enemy)
        {
            BossDefinition boss = enemy.Definition.Boss;
            if (boss != null && boss.ResultHoldSeconds > 0f)
            {
                _holdUntil = Mathf.Max(_holdUntil, Time.time + boss.ResultHoldSeconds);
            }
        }

        // In game time, so a paused game holds it too; the story may have started meanwhile, so it asks again.
        private IEnumerator RefreshAfterHold()
        {
            while (Time.time < _holdUntil)
            {
                yield return null;
            }

            _waiting = false;
            Refresh();
        }

        private IEnumerator ShowAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            Show();
        }

        private void Show()
        {
            _shown = true;
            Time.timeScale = 1f;
            _root = Build(_context.Result);
            StartCoroutine(Reveal(_root.transform.GetChild(0) as RectTransform, _context.Result));
        }

        private GameObject Build(LevelResult result)
        {
            Image dim = UiKit.CreatePanel(_safeRoot, "ResultDim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Dim);
            dim.raycastTarget = true;

            Image panel = UiKit.CreatePanel(dim.rectTransform, "ResultPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, result.Won ? VictoryTint : DefeatTint);
            RectTransform rect = panel.rectTransform;
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Text title = UiKit.CreateFixedText(rect, result.Won ? "Victory" : "Defeat", 96, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(PanelWidth, 120f));
            title.color = result.Won ? Gold : new Color(1f, 0.55f, 0.5f);

            string subtitle = $"{_context.Level.DisplayName}  -  {_context.Difficulty.DisplayName}";
            UiKit.CreateFixedText(rect, subtitle, 36, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(PanelWidth, 50f)).color = Muted;

            Button first = result.Won ? BuildWin(rect, result) : BuildLoss(rect);
            BuildButtons(rect, result.Won);
            UiKit.Select(first);
            return dim.gameObject;
        }

        private Button BuildWin(RectTransform rect, LevelResult result)
        {
            BuildStars(rect, result.Stars);

            string rewards = $"Research points  +{result.ResearchPoints}";
            if (result.PowerCurrency > 0)
            {
                rewards += $"      Power  +{result.PowerCurrency}";
            }

            if (result.RewardsAlreadyEarned)
            {
                rewards = "Points for this difficulty were earned before.\nBeat it on a harder setting for more.";
            }

            string unlocks = UnlockLine(result);
            bool twoLines = result.RewardsAlreadyEarned || unlocks != null;
            if (unlocks != null)
            {
                rewards += "\n" + unlocks;
            }

            Text rewardText = UiKit.CreateFixedText(rect, rewards, twoLines ? UnlockLineSize : 40, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(PanelWidth - 120f, 100f));
            rewardText.color = result.RewardsAlreadyEarned ? Muted : Color.white;

            if (result.NewTower != null)
            {
                BuildNewWeapon(rect, result);
            }

            return null;
        }

        // One line for the unlocks that have no picture of their own: new gadgets for the loadout and a commander perk.
        private static string UnlockLine(LevelResult result)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (result.NewPathObjects.Length > 0)
            {
                var names = new string[result.NewPathObjects.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    names[i] = result.NewPathObjects[i].DisplayName;
                }

                parts.Add($"New gadgets: {string.Join(", ", names)}");
            }

            if (result.HeroPerk != null)
            {
                parts.Add($"Mercer learns {result.HeroPerk.DisplayName}");
            }

            // The weapon panel shows the credit under its heading; without a weapon it closes this line.
            if (parts.Count > 0 && result.NewTower == null && !string.IsNullOrEmpty(result.Credit))
            {
                parts.Add($"({result.Credit})");
            }

            return parts.Count > 0 ? string.Join("      ", parts) : null;
        }

        private static Button BuildLoss(RectTransform rect)
        {
            UiKit.CreateFixedText(rect, "The base has fallen.\nUpgrade your defences and try again.", 40, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(PanelWidth, 200f));
            return null;
        }

        private static void BuildStars(RectTransform rect, int earned)
        {
            var row = new GameObject("Stars", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(rect, false);
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -215f);
            row.sizeDelta = new Vector2(3 * StarSize + 2 * StarGap, StarSize);

            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Star", typeof(RectTransform));
                go.transform.SetParent(row, false);
                var image = go.AddComponent<Image>();
                image.sprite = IconFactory.Get(IconType.Star);
                image.color = StarOff;
                image.raycastTarget = false;
                RectTransform starRect = image.rectTransform;
                starRect.anchorMin = new Vector2(0f, 0.5f);
                starRect.anchorMax = new Vector2(0f, 0.5f);
                starRect.pivot = new Vector2(0f, 0.5f);
                starRect.sizeDelta = new Vector2(StarSize, StarSize);
                starRect.anchoredPosition = new Vector2(i * (StarSize + StarGap), 0f);
                go.name = i < earned ? "StarEarned" : "StarEmpty";
            }
        }

        private void BuildNewWeapon(RectTransform rect, LevelResult result)
        {
            Image frame = UiKit.CreatePanel(rect, "WeaponFrame", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, new Color(0.06f, 0.1f, 0.2f, 1f));
            RectTransform frameRect = frame.rectTransform;
            frameRect.pivot = new Vector2(0f, 1f);
            frameRect.anchoredPosition = new Vector2(60f, -470f);
            frameRect.sizeDelta = new Vector2(PreviewSize, PreviewSize * 0.78f);

            ModelPreview preview = ModelPreview.Create(frameRect, new Vector2(PreviewSize, PreviewSize * 0.78f));
            preview.ShowTower(result.NewTower);

            string source = !string.IsNullOrEmpty(result.Credit) ? result.Credit
                : result.Source == UnlockSource.StolenAlienTech ? "Stolen alien technology" : "Intel from the home planet";
            Text heading = UiKit.CreateFixedText(rect, $"New weapon: {result.NewTower.DisplayName}", 44, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(60f + PreviewSize + 40f, -470f), new Vector2(PanelWidth - PreviewSize - 140f, 60f));
            heading.color = Gold;
            UiKit.CreateFixedText(rect, source, 30, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(60f + PreviewSize + 40f, -535f), new Vector2(PanelWidth - PreviewSize - 140f, 40f)).color = Muted;
            if (!string.IsNullOrEmpty(result.Story))
            {
                // Room for three lines above the buttons, whatever the story says; the box shrinks the text if it must.
                Text story = UiKit.CreateFixedText(rect, result.Story, 26, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(60f + PreviewSize + 40f, -580f), new Vector2(PanelWidth - PreviewSize - 140f, 130f));
                story.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
        }

        private void BuildButtons(RectTransform rect, bool won)
        {
            var row = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(rect, false);
            row.anchorMin = new Vector2(0.5f, 0f);
            row.anchorMax = new Vector2(0.5f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, 30f);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button primary = AddButton(row, won ? "Upgrades" : "Play again", won ? (UnityEngine.Events.UnityAction)(() => Go(SceneNames.Base)) : PlayAgain);
            AddButton(row, won ? "Play again" : "Upgrades", won ? (UnityEngine.Events.UnityAction)PlayAgain : () => Go(SceneNames.Base));
            AddButton(row, "Back to map", () => Go(SceneNames.WorldMap));
            UiKit.Select(primary);
        }

        private static Button AddButton(RectTransform row, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = UiKit.CreateButton(row, label, action);
            Destroy(button.GetComponent<LayoutElement>());
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(ButtonWidth, UiKit.ButtonHeight);
            return button;
        }

        // The panel pops in, then the earned stars light up one after another.
        private IEnumerator Reveal(RectTransform panel, LevelResult result)
        {
            float t = 0f;
            while (t < PopSeconds)
            {
                t += Time.unscaledDeltaTime;
                float eased = Mathf.SmoothStep(0.8f, 1f, Mathf.Clamp01(t / PopSeconds));
                panel.localScale = Vector3.one * eased;
                yield return null;
            }

            panel.localScale = Vector3.one;
            if (!result.Won)
            {
                yield break;
            }

            Transform stars = panel.Find("Stars");
            for (int i = 0; i < result.Stars && stars != null; i++)
            {
                yield return new WaitForSecondsRealtime(StarRevealSeconds);
                Transform star = stars.GetChild(i);
                star.GetComponent<Image>().color = StarOn;
                star.localScale = Vector3.one * 1.35f;
                float settle = 0f;
                while (settle < 0.2f)
                {
                    settle += Time.unscaledDeltaTime;
                    star.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, settle / 0.2f);
                    yield return null;
                }
            }
        }

        private static void Go(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }

        private static void PlayAgain() => Go(SceneNames.Level);
    }
}
