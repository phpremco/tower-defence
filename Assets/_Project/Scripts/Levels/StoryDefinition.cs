using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The story of the campaign as data: the beats of the intro shown on first launch. Level briefings live on the
    /// levels themselves, so a level carries its own piece of the story.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story", fileName = "Story")]
    public sealed class StoryDefinition : ScriptableObject
    {
        [SerializeField] private StoryBeat[] _introBeats = new StoryBeat[0];

        public IReadOnlyList<StoryBeat> IntroBeats => _introBeats;

        private void OnValidate()
        {
            for (int i = 0; i < _introBeats.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(_introBeats[i].Text))
                {
                    Debug.LogWarning($"Story '{name}': intro beat {i + 1} has no text.", this);
                }
            }
        }
    }
}
