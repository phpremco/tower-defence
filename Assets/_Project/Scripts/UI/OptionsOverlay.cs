using Game.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Options panel over the main menu: master volume in steps, stored in the saved progress.</summary>
    public sealed class OptionsOverlay : MonoBehaviour
    {
        private const int VolumeStep = 25;
        private const int MaxVolume = 100;

        private MainMenuScreen _owner;
        private ProgressService _progress;
        private GameObject _panel;
        private Button _volumeButton;
        private GameObject _controlsPanel;
        private RectTransform _column;
        private Button _controlsButton;

        private const string ControlsText =
            "Move the cursor      Left stick / D-pad    WASD    Mouse\n" +
            "Confirm              A    Enter    Left click\n" +
            "Cancel               B    Esc    Right click\n" +
            "Switch tower         LB / RB    Q / E\n" +
            "Tower icons          Stick / D-pad to pick, A to use, B to close\n" +
            "Upgrade / Repair / Sell   U / R / X   (X on a blockade sends the hero)\n" +
            "Hero: next post / shockwave / repair post   X button / LT / RT    H / F / G\n" +
            "Game speed           Y    Space\n" +
            "Pause                Start    P\n" +
            "Skip wave (test)     Select    N";

        public void Open(MainMenuScreen owner)
        {
            _owner = owner;
            Game.Core.Services.TryGet(out _progress);

            RectTransform root = UiKit.CreateRoot(transform);
            _panel = root.parent.gameObject;
            var background = new GameObject("Dim", typeof(RectTransform)).AddComponent<Image>();
            background.transform.SetParent(root, false);
            background.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);
            RectTransform rect = background.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            UiKit.CreateFixedText(root, "Options", UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1200f, 120f));
            _column = UiKit.CreateColumn(root, 24f);
            _volumeButton = UiKit.CreateButton(_column, VolumeLabel(), CycleVolume);
            _controlsButton = UiKit.CreateButton(_column, "Controls", ShowControls);
            UiKit.CreateButton(_column, "Back", Close);
            UiKit.Select(_volumeButton);

            _controlsPanel = new GameObject("Controls", typeof(RectTransform));
            _controlsPanel.transform.SetParent(root, false);
            Text text = UiKit.CreateFixedText(_controlsPanel.GetComponent<RectTransform>(), ControlsText, UiKit.BodySize, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), new Vector2(-560f, 300f), new Vector2(1200f, 600f));
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Button back = UiKit.CreateButton(_controlsPanel.GetComponent<RectTransform>(), "Back", HideControls);
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 60f);
            backRect.sizeDelta = new Vector2(UiKit.ButtonWidth, UiKit.ButtonHeight);
            _controlsPanel.SetActive(false);
        }

        private void ShowControls()
        {
            _column.gameObject.SetActive(false);
            _controlsPanel.SetActive(true);
            UiKit.Select(_controlsPanel.GetComponentInChildren<Button>());
        }

        private void HideControls()
        {
            _controlsPanel.SetActive(false);
            _column.gameObject.SetActive(true);
            UiKit.Select(_controlsButton);
        }

        private void CycleVolume()
        {
            int next = _progress.Data.MasterVolumePercent + VolumeStep;
            if (next > MaxVolume)
            {
                next = 0;
            }

            _progress.SetVolume(next);
            AudioListener.volume = next / (float)MaxVolume;
            UiKit.SetLabel(_volumeButton, VolumeLabel());
        }

        private string VolumeLabel() => $"Volume: {_progress.Data.MasterVolumePercent}%";

        private void Close()
        {
            Destroy(_panel);
            _owner.ShowMenu();
            Destroy(this);
        }
    }
}



