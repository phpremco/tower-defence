using UnityEngine;

namespace Game.Levels
{
    /// <summary>Someone who speaks in dialogues: name, colour and how the box frames them. The asset name is the id.</summary>
    [CreateAssetMenu(menuName = "Game/Story/Speaker", fileName = "Speaker_")]
    public sealed class SpeakerDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Speaker";

        [Tooltip("Name tag, rim light and box border.")]
        [SerializeField] private Color _roleColour = Color.white;

        [SerializeField] private SpeakerFrame _frame = SpeakerFrame.Portrait;
        [SerializeField] private PortraitModel _portrait = PortraitModel.Hero;

        [Tooltip("Optional drawn portrait. When set it replaces the 3D bust.")]
        [SerializeField] private Sprite _portraitOverride;

        public string Id => name;
        public string DisplayName => _displayName;
        public Color RoleColour => _roleColour;
        public SpeakerFrame Frame => _frame;
        public PortraitModel Portrait => _portrait;
        public Sprite PortraitOverride => _portraitOverride;

        public bool IsValid(out string error)
        {
            error = string.IsNullOrWhiteSpace(_displayName) ? "Display name is empty." : null;
            return error == null;
        }

        private void OnValidate()
        {
            if (!IsValid(out string error))
            {
                Debug.LogWarning($"Speaker '{name}' is invalid: {error}", this);
            }
        }
    }
}
