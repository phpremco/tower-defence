using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A cutscene's own sounds and music as data: one slot per <see cref="CutsceneSound"/> it has recorded files for
    /// (looked up before the game's shared library) and one track per music mood. A cutscene with a bank takes its
    /// music only from here, so an empty mood is silent; a slot without files plays its fallback, so a missing recording
    /// arrives later by data alone.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story/Cutscene Sound Bank", fileName = "CutsceneSounds_")]
    public sealed class CutsceneSoundBank : ScriptableObject
    {
        [SerializeField] private CutsceneSoundSlot[] _slots = new CutsceneSoundSlot[0];
        [Tooltip("The track per music mood; leave a track empty for silence (the opening has none until Remco picks one).")]
        [SerializeField] private CutsceneMusicSlot[] _music = new CutsceneMusicSlot[0];

        public IReadOnlyList<CutsceneSoundSlot> Slots => _slots;
        public IReadOnlyList<CutsceneMusicSlot> Music => _music;

        /// <summary>The index of the slot for a sound, or -1 when the bank has none.</summary>
        public int IndexOf(CutsceneSound sound)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Sound == sound)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The track of a mood, or false when the bank has none or it is empty.</summary>
        public bool TryGetMusic(CutsceneMusic mood, out CutsceneMusicSlot track)
        {
            foreach (CutsceneMusicSlot slot in _music)
            {
                if (slot.Mood == mood && !slot.IsEmpty)
                {
                    track = slot;
                    return true;
                }
            }

            track = default;
            return false;
        }

        /// <summary>Adds every data problem to the list; returns true when there is none.</summary>
        public bool Validate(List<string> errors)
        {
            int before = errors.Count;
            var seen = new HashSet<CutsceneSound>();
            foreach (CutsceneSoundSlot slot in _slots)
            {
                if (slot.Sound == CutsceneSound.None)
                {
                    errors.Add("a slot has no sound.");
                }
                else if (!seen.Add(slot.Sound))
                {
                    errors.Add($"{slot.Sound} has two slots.");
                }

                if (slot.Fallback == slot.Sound && slot.Sound != CutsceneSound.None)
                {
                    errors.Add($"{slot.Sound} falls back on itself.");
                }
            }

            var moods = new HashSet<CutsceneMusic>();
            foreach (CutsceneMusicSlot slot in _music)
            {
                if (slot.Mood == CutsceneMusic.Keep || slot.Mood == CutsceneMusic.Silence)
                {
                    errors.Add($"a music track is set for the {slot.Mood} mood, which plays none.");
                }
                else if (!moods.Add(slot.Mood))
                {
                    errors.Add($"the {slot.Mood} mood has two tracks.");
                }
            }

            return errors.Count == before;
        }

        private void OnValidate()
        {
            var errors = new List<string>();
            if (!Validate(errors))
            {
                Debug.LogWarning($"Cutscene sound bank '{name}':\n{string.Join("\n", errors)}", this);
            }
        }
    }
}
