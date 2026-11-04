using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The cutscene's glow, trail and crack sprites from Blender, in a High and a Low (Switch) size, by
    /// <see cref="CutsceneSprite"/>. The cutscene registers its cast's set as <see cref="Active"/> while it plays; a
    /// sprite that is missing falls back to the soft texture the code paints, so a fresh clone without the art still runs.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story/Cutscene FX Set", fileName = "CutsceneFxSet")]
    public sealed class CutsceneFxSet : ScriptableObject
    {
        [Tooltip("One texture per CutsceneSprite, in enum order.")]
        [SerializeField] private Texture2D[] _high = new Texture2D[0];
        [SerializeField] private Texture2D[] _low = new Texture2D[0];

        /// <summary>The set of the cutscene that plays now, or null.</summary>
        public static CutsceneFxSet Active { get; set; }

        /// <summary>The sprite of the active set for the quality profile, or null when there is none.</summary>
        public static Texture2D Get(CutsceneSprite sprite) => Active != null ? Active.Texture(sprite) : null;

        public Texture2D Texture(CutsceneSprite sprite)
        {
            int index = (int)sprite;
            Texture2D[] maps = ModelQuality.IsLow && _low.Length > index && _low[index] != null ? _low : _high;
            return index < maps.Length ? maps[index] : null;
        }

        private void OnValidate()
        {
            int count = System.Enum.GetValues(typeof(CutsceneSprite)).Length;
            if (_high.Length != 0 && _high.Length != count)
            {
                Debug.LogWarning($"Cutscene FX set '{name}' holds {_high.Length} sprites; CutsceneSprite has {count}.", this);
            }
        }
    }
}
