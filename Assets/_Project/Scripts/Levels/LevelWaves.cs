using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Every wave of one level on every difficulty, in one asset. Kept apart from the <see cref="LevelDefinition"/>
    /// because the waves are written wholesale by the generator and the balance tool, while the level's layout is not:
    /// rewriting the waves never touches the level file, and the level file stays small enough to read.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Level Waves", fileName = "LevelWaves")]
    public sealed class LevelWaves : ScriptableObject
    {
        [SerializeField] private DifficultyWaves[] _difficultyWaves = new DifficultyWaves[0];

        /// <summary>The wave list of each difficulty, easiest first.</summary>
        public DifficultyWaves[] Sets => _difficultyWaves;

        /// <summary>Returns the waves for a difficulty, or null when there are none for it.</summary>
        public WaveDefinition[] For(DifficultyProfile difficulty)
        {
            for (int i = 0; i < _difficultyWaves.Length; i++)
            {
                if (_difficultyWaves[i].Difficulty == difficulty)
                {
                    return _difficultyWaves[i].Waves;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            var seen = new HashSet<DifficultyProfile>();
            foreach (DifficultyWaves set in _difficultyWaves)
            {
                if (set.Difficulty == null || set.Waves == null || set.Waves.Length == 0)
                {
                    Debug.LogWarning($"LevelWaves '{name}' has a difficulty entry without a difficulty or without waves.", this);
                    continue;
                }

                if (!seen.Add(set.Difficulty))
                {
                    Debug.LogWarning($"LevelWaves '{name}' lists {set.Difficulty.name} twice.", this);
                }

                WarnAboutEmptyGroups(set);
            }
        }

        private void WarnAboutEmptyGroups(DifficultyWaves set)
        {
            for (int i = 0; i < set.Waves.Length; i++)
            {
                foreach (SpawnGroup group in set.Waves[i].Groups)
                {
                    if (group.Enemy == null)
                    {
                        Debug.LogWarning($"LevelWaves '{name}' {set.Difficulty.name} wave {i + 1} has a group without an enemy.", this);
                    }
                }
            }
        }
    }
}
