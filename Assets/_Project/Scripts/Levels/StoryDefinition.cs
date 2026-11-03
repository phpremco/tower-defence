using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The story of the campaign as data: the opening cutscene of the first launch. Level briefings live on the
    /// levels themselves, so a level carries its own piece of the story.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story", fileName = "Story")]
    public sealed class StoryDefinition : ScriptableObject
    {
        [Tooltip("The cutscene the first launch opens with: prologue, landing, the first attack.")]
        [SerializeField] private CutsceneDefinition _openingCutscene;

        /// <summary>The opening cutscene; null while there is none.</summary>
        public CutsceneDefinition OpeningCutscene => _openingCutscene;
    }
}
