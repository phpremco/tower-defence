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
            RectTransform column = UiKit.CreateColumn(root, 24f);
            _volumeButton = UiKit.CreateButton(column, VolumeLabel(), CycleVolume);
            UiKit.CreateButton(column, "Back", Close);
            UiKit.Select(_volumeButton);
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

