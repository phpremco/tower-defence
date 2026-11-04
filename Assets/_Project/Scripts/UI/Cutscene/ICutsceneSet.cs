using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// One place a cutscene plays in. The player builds it once (behind a fade), shows it while its shots play and
    /// hides it in between. <see cref="Tick"/> must depend on the shot time alone, never on earlier frames, so any
    /// moment can be sought for a screenshot or a test.
    /// </summary>
    public interface ICutsceneSet
    {
        /// <summary>The theme for the set's post-processing volume and lighting.</summary>
        VisualTheme Theme { get; }

        void Build(Transform root, CutsceneContext context);

        /// <summary>A shot of this set begins; the set is shown again and sets its lighting.</summary>
        void Enter(CutsceneAction action);

        /// <summary>Poses everything for this moment: seconds into the shot and progress of its camera move (0..1).</summary>
        void Tick(float time, float progress);

        /// <summary>The shot is over and the next one plays somewhere else.</summary>
        void Exit();

        /// <summary>Where an anchor of this set lies: its origin and facing. False when the set has no such anchor.</summary>
        bool TryGetAnchor(CameraAnchor anchor, out Vector3 origin, out Quaternion facing);

        /// <summary>
        /// Clears the scattered decor off the sight lines from each camera spot to what it looks at (a close shot); empty
        /// lists bring everything back. A set without scattered decor ignores it.
        /// </summary>
        void ClearView(IReadOnlyList<Vector3> eyes, IReadOnlyList<Vector3> targets);
    }
}
