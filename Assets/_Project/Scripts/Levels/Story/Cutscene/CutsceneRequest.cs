using System;
using Game.Core;

namespace Game.Levels
{
    /// <summary>
    /// Which cutscene the Intro scene plays and where it goes afterwards. Boot registers one on the first launch;
    /// a replay from the options registers one that returns to the menu. Tests switch on auto-advance and speed.
    /// </summary>
    public sealed class CutsceneRequest
    {
        public CutsceneRequest(CutsceneDefinition cutscene, CutsceneHandoff handoff, string returnScene = SceneNames.MainMenu)
        {
            Cutscene = cutscene != null ? cutscene : throw new ArgumentNullException(nameof(cutscene));
            Handoff = handoff;
            ReturnScene = returnScene;
        }

        /// <summary>The request of a first launch: the cutscene hands over the way its own data says.</summary>
        public static CutsceneRequest FirstLaunch(CutsceneDefinition cutscene)
        {
            if (cutscene == null)
            {
                throw new ArgumentNullException(nameof(cutscene));
            }

            return new CutsceneRequest(cutscene, cutscene.Handoff);
        }

        public CutsceneDefinition Cutscene { get; }

        public CutsceneHandoff Handoff { get; }

        /// <summary>The scene a <see cref="CutsceneHandoff.ReturnScene"/> handoff loads.</summary>
        public string ReturnScene { get; }

        /// <summary>Test hook: dialogue boxes advance by themselves, so the cutscene runs to its end unattended.</summary>
        public bool AutoAdvance { get; set; }

        /// <summary>Test hook: how fast cutscene time runs against real time.</summary>
        public float Speed { get; set; } = 1f;
    }
}
