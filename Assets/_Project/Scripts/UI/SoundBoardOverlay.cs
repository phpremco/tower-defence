using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A sound test under the options: every recorded sound of the library and every creature voice as a button
    /// that plays its next variant, at the measured gain alone, so the sounds themselves can be judged one by one.
    /// In the game each kind is mixed quieter on top of this.
    /// </summary>
    public sealed class SoundBoardOverlay : MonoBehaviour
    {
        private const int PageSize = 6;
        private const float PreviewVolume = 0.6f;
        private const float TitleTop = -36f;
        private const float ColumnDrop = -60f;
        private const float RowSpacing = 10f;
        private const float RowHeight = 56f;

        private sealed class Entry
        {
            public string Label;
            public SoundSet Set;
            public int Cursor;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private OptionsOverlay _owner;
        private GameObject _panel;
        private RectTransform _column;
        private AudioSource _source;
        private AudioMix _mix;
        private Text _pageText;
        private int _page;

        public void Open(OptionsOverlay owner)
        {
            _owner = owner;
            Services.TryGet(out _mix);
            Services.TryGet(out SfxLibrary library);
            Collect(library);

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            RectTransform root = UiKit.CreateRoot(transform);
            _panel = root.parent.gameObject;
            CoverScreen.Add(UiKit.CreatePanel(root, "Dim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.04f, 0.05f, 0.08f, 0.92f)).rectTransform);
            UiKit.CreateFixedText(root, "Sound test", UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, TitleTop), new Vector2(1200f, 120f));
            _pageText = UiKit.CreateFixedText(root, string.Empty, UiKit.BodySize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, TitleTop - 90f), new Vector2(1200f, 60f));
            _column = UiKit.CreateColumn(root, RowSpacing);
            _column.anchoredPosition = new Vector2(0f, ColumnDrop);
            ShowPage(0);
        }

        private void Collect(SfxLibrary library)
        {
            if (library == null)
            {
                return;
            }

            foreach ((string label, SoundSet set) in library.Sets())
            {
                Add(label, set);
            }

            foreach (CreatureVoice voice in library.Voices)
            {
                if (voice == null)
                {
                    continue;
                }

                string species = voice.name.Replace("Voice_", string.Empty);
                foreach ((string label, SoundSet set) in voice.Sets())
                {
                    Add($"{species}: {label}", set);
                }
            }
        }

        private void Add(string label, SoundSet set)
        {
            if (set != null && !set.IsEmpty)
            {
                _entries.Add(new Entry { Label = label, Set = set });
            }
        }

        private void ShowPage(int page)
        {
            int pages = Mathf.Max(1, (_entries.Count + PageSize - 1) / PageSize);
            _page = (page + pages) % pages;
            for (int i = _column.childCount - 1; i >= 0; i--)
            {
                Destroy(_column.GetChild(i).gameObject);
            }

            _pageText.text = $"Page {_page + 1} of {pages}   -   {_entries.Count} sounds";
            Button first = null;
            int start = _page * PageSize;
            for (int i = start; i < Mathf.Min(_entries.Count, start + PageSize); i++)
            {
                Entry entry = _entries[i];
                Button button = UiKit.CreateButton(_column, $"{entry.Label}   ({entry.Set.Count}x, gain {entry.Set.Gain:0.00})", () => Play(entry));
                button.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(buttonRect.sizeDelta.x, RowHeight);
                first = first != null ? first : button;
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

        private void Play(Entry entry)
        {
            AudioClip clip = entry.Set.Pick(ref entry.Cursor);
            if (clip == null)
            {
                return;
            }

            _source.Stop();
            _source.volume = PreviewVolume * entry.Set.Gain * (_mix != null ? _mix.EffectsGain : 1f);
            _source.PlayOneShot(clip, 1f);
        }

        /// <summary>Closes the board and hands the options column back (Back, or B / Esc through the options panel).</summary>
        public void Close()
        {
            _source.Stop();
            Destroy(_panel);
            _owner.ReturnFromSoundBoard();
            Destroy(this);
        }
    }
}
