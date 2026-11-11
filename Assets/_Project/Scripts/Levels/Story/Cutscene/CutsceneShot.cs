using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// One shot of a cutscene: where it plays and what the set does, the camera move, how long it lasts, the dialogue
    /// spoken over it, timed cues, the music mood and the framing (fades, letterbox, title card). Plain fields, so
    /// the data stays flat and IL2CPP-plain.
    /// </summary>
    [Serializable]
    public struct CutsceneShot
    {
        private const float MinimumLength = 0.01f;

        public CutsceneSet Set;
        public CutsceneAction Action;
        public CameraKey[] Camera;
        public ShotTiming Timing;

        /// <summary>Fixed: the length of the shot. AfterDialogue: how long the shot stays after its dialogue has ended.</summary>
        [Min(0f)] public float Seconds;

        /// <summary>How long the camera move takes; zero uses the shot length (Fixed) or the dialogue delay plus seconds.</summary>
        [Min(0f)] public float CameraSeconds;

        public DialogueDefinition Dialogue;

        /// <summary>Seconds into the shot before the dialogue box opens.</summary>
        [Min(0f)] public float DialogueDelay;

        public CutsceneCue[] Cues;

        /// <summary>Sounds held over part of the shot (beds, engines, breathing), faded by the shot clock.</summary>
        public CutsceneLayer[] Layers;

        /// <summary>
        /// Round 44 (Remco: "things shaking, but no rumbling noise"): how loud the quake rumble follows everything that shakes
        /// in the shot (a camera kick, the set's rumble of the lens, the ground and props trembling), 0 for no rumble.
        /// </summary>
        [Range(0f, 1f)] public float ShakeRumble;

        public CutsceneMusic Music;

        /// <summary>Seconds of fade from black at the start and to black at the end; zero cuts.</summary>
        [Min(0f)] public float FadeIn;
        [Min(0f)] public float FadeOut;

        public bool Letterbox;
        public CutsceneOverlayKind Overlay;

        /// <summary>Large text shown over the end of the shot; empty for none.</summary>
        public string TitleCard;

        /// <summary>The shot's own exposure, light rig and clear view (cinematography on top of the set's light).</summary>
        public ShotLighting Lighting;

        /// <summary>The length the camera move and the set animation are timed to.</summary>
        public float CameraLength
        {
            get
            {
                if (CameraSeconds > 0f)
                {
                    return CameraSeconds;
                }

                return Timing == ShotTiming.Fixed ? Mathf.Max(Seconds, MinimumLength) : Mathf.Max(DialogueDelay + Seconds, MinimumLength);
            }
        }

        /// <summary>How long the shot runs when nobody reads: dialogue boxes close at once (auto-advance, tests).</summary>
        public float AutoLength => Timing == ShotTiming.Fixed ? Seconds : (Dialogue != null ? DialogueDelay : 0f) + Seconds;
    }
}
