using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The source that carries one held sound of a cutscene: a shot's layer (kept across shots that list it again, so it
    /// never restarts) or a loop a set drives itself (drill, drone fans). Its level is the share before the slot's gain
    /// and the player's mix.
    /// </summary>
    public sealed class CutsceneLayerVoice
    {
        public CutsceneLayerVoice(CutsceneSound sound, AudioSource source, float gain, CutsceneSoundChannel channel, bool loop, bool drivenBySet)
        {
            Sound = sound;
            Source = source;
            Gain = gain;
            Channel = channel;
            Loop = loop;
            DrivenBySet = drivenBySet;
        }

        public CutsceneSound Sound { get; }

        /// <summary>Null when the sound has no file and no fallback: the layer stays silent.</summary>
        public AudioSource Source { get; }

        public float Gain { get; }
        public CutsceneSoundChannel Channel { get; }
        public bool Loop { get; }

        /// <summary>A set's own loop: its level comes from the set, never from a shot's layers.</summary>
        public bool DrivenBySet { get; }

        /// <summary>The level now and the level the shot clock asks for.</summary>
        public float Level { get; set; }
        public float Target { get; set; }

        /// <summary>Seconds since the loudest layer of this sound started in the shot; negative before it.</summary>
        public float Offset { get; set; }

        /// <summary>
        /// Starts the clip where the shot clock says it is: a loop anywhere in its cycle, a one-shot from its offset and
        /// not at all once the offset has run past its end (so a finished one-shot layer never starts over).
        /// </summary>
        public void StartAtOffset()
        {
            AudioClip clip = Source != null ? Source.clip : null;
            if (clip == null || Offset < 0f)
            {
                return;
            }

            if (Loop)
            {
                Source.time = Mathf.Repeat(Offset, clip.length);
            }
            else if (Offset < clip.length)
            {
                Source.time = Offset;
            }
            else
            {
                return;
            }

            Source.Play();
        }
    }
}
