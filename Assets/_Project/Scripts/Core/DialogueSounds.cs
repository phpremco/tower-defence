using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// How the dialogue box sounds: a radio squelch when a speaker starts talking, soft blips while the text types out
    /// (every few letters, a variant of the speaker's own set) and no sound on closing. Filled by the generator.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Audio/Dialogue Sounds", fileName = "DialogueSounds")]
    public sealed class DialogueSounds : ScriptableObject
    {
        [Tooltip("Plays when a speaker starts talking: the first line in a fresh box, or another speaker's turn.")]
        [SerializeField] private SoundSet _opener = new SoundSet();
        [SerializeField] [Range(0f, 1f)] private float _openerVolume = 0.5f;

        [Tooltip("One blip per this many revealed letters or digits; spaces and punctuation do not count.")]
        [SerializeField] [Min(1)] private int _blipEveryLetters = 2;
        [SerializeField] [Range(0f, 1f)] private float _blipVolume = 0.3f;

        [Tooltip("The blips of a speaker without a set of its own.")]
        [SerializeField] private SoundSet _defaultBlips = new SoundSet();

        public SoundSet Opener => _opener;
        public float OpenerVolume => _openerVolume;
        public int BlipEveryLetters => _blipEveryLetters;
        public float BlipVolume => _blipVolume;
        public SoundSet DefaultBlips => _defaultBlips;

        private void OnValidate()
        {
            if (_opener == null || _opener.IsEmpty)
            {
                Debug.LogWarning($"Dialogue sounds '{name}' have no opener.", this);
            }
        }
    }
}
