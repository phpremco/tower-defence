using Game.Enemies;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The story of the campaign as data: the opening cutscene of the first launch and how the hero meets each continent
    /// boss. Level briefings live on the levels themselves, so a level carries its own piece of the story.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story", fileName = "Story")]
    public sealed class StoryDefinition : ScriptableObject
    {
        [Tooltip("The cutscene the first launch opens with: prologue, landing, the first attack.")]
        [SerializeField] private CutsceneDefinition _openingCutscene;

        [Tooltip("The hero's first meeting with each continent boss and his barks in the fight (Docs/BOSS_INTROS.md).")]
        [SerializeField] private BossIntroDefinition[] _bossIntros = new BossIntroDefinition[0];

        /// <summary>The opening cutscene; null while there is none.</summary>
        public CutsceneDefinition OpeningCutscene => _openingCutscene;

        /// <summary>The intro of a boss, or null when it has none.</summary>
        public BossIntroDefinition IntroFor(EnemyDefinition boss)
        {
            if (boss == null || _bossIntros == null)
            {
                return null;
            }

            foreach (BossIntroDefinition intro in _bossIntros)
            {
                if (intro != null && intro.Boss == boss)
                {
                    return intro;
                }
            }

            return null;
        }
    }
}
