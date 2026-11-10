using Game.Platform;
using UnityEngine;

namespace Game.UI
{
    /// <summary>The sprites and fonts the interface is drawn with. Replace them in the asset to restyle every screen.</summary>
    [CreateAssetMenu(menuName = "Game/UI Skin", fileName = "UiSkin")]
    public sealed class UiSkin : ScriptableObject
    {
        [SerializeField] private Font _font;
        [SerializeField] private Font _narrowFont;
        [SerializeField] private Sprite _panel;
        [SerializeField] private Sprite _panelSquare;
        [SerializeField] private Sprite _button;
        [SerializeField] private Sprite _buttonSquare;
        [SerializeField] private Sprite _barBack;
        [SerializeField] private Sprite _barBlue;
        [SerializeField] private Sprite _barGreen;
        [SerializeField] private Sprite _barYellow;
        [SerializeField] private Sprite _barRed;
        [SerializeField] private Sprite _cardBlue;
        [SerializeField] private Sprite _cardGreen;
        [SerializeField] private Sprite _cardYellow;
        [SerializeField] private Sprite _cardRed;
        [Tooltip("The pedestal, halo and backdrop the upgrade screen's models turn on (Blender round 27).")]
        [SerializeField] private DisplayStageArt _displayStage;
        [Tooltip("The icons, nine-slice kit, dialogue pieces and pictures painted in Blender (round 25).")]
        [SerializeField] private UiArtSet _art;

        [Tooltip("What prompts print for pad buttons, one table per pad layout; the platform settings pick one.")]
        [SerializeField] private ButtonGlyphTable[] _padGlyphs = new ButtonGlyphTable[0];

        [Tooltip("Short names prompts print for keys and mouse buttons (\"Esc\", \"Click\").")]
        [SerializeField] private ButtonGlyphTable _keyGlyphs;

        [Tooltip("The smallest font size (in 1080p canvas units) for labels the player must read in play, such as the quick bar: 27 draws about 18 px on a 720p handheld.")]
        [SerializeField, Min(1)] private int _minReadableFontSize = 27;

        public Font Font => _font;
        public Font NarrowFont => _narrowFont != null ? _narrowFont : _font;
        public Sprite Panel => _panel;
        public Sprite PanelSquare => _panelSquare;
        public Sprite Button => _button;
        public Sprite ButtonSquare => _buttonSquare;
        public Sprite BarBack => _barBack;
        public Sprite BarBlue => _barBlue;
        public Sprite BarGreen => _barGreen;
        public Sprite BarYellow => _barYellow;
        public Sprite BarRed => _barRed;
        public Sprite CardBlue => _cardBlue;
        public Sprite CardGreen => _cardGreen;
        public Sprite CardYellow => _cardYellow;
        public Sprite CardRed => _cardRed;

        /// <summary>The model preview's stage; null on data generated before it existed.</summary>
        public DisplayStageArt DisplayStage => _displayStage;

        /// <summary>The Blender interface art; null on data generated before it existed.</summary>
        public UiArtSet Art => _art;

        /// <summary>Short key names for prompts; null on data generated before it existed.</summary>
        public ButtonGlyphTable KeyGlyphs => _keyGlyphs;

        public int MinReadableFontSize => _minReadableFontSize;

        /// <summary>The pad table of a layout, else the first table, else null (prompts then print the Input System's names).</summary>
        public ButtonGlyphTable PadGlyphs(PadLayout layout)
        {
            if (_padGlyphs == null || _padGlyphs.Length == 0)
            {
                return null;
            }

            foreach (ButtonGlyphTable table in _padGlyphs)
            {
                if (table != null && table.Layout == layout)
                {
                    return table;
                }
            }

            return _padGlyphs[0];
        }
    }
}
