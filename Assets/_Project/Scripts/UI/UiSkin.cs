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
    }
}
