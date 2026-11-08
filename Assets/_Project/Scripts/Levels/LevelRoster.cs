using System;
using System.Collections.Generic;
using Game.Enemies;

namespace Game.Levels
{
    /// <summary>
    /// Which creatures a level sends, read from its wave data: every group's enemy, and the minions its bosses call in.
    /// The in-level codex lists only these, so a map never shows creatures the player cannot meet there.
    /// </summary>
    public static class LevelRoster
    {
        /// <summary>
        /// The enemies of the level on this difficulty. A difficulty without its own waves falls back to every
        /// difficulty's waves, so the codex never lists nothing for a level that has waves at all.
        /// </summary>
        public static void Collect(LevelDefinition level, DifficultyProfile difficulty, HashSet<EnemyDefinition> into)
        {
            into.Clear();
            if (level == null)
            {
                return;
            }

            WaveDefinition[] waves = difficulty != null ? level.GetWaves(difficulty) : null;
            if (waves != null && waves.Length > 0)
            {
                AddFrom(waves, into);
                return;
            }

            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                AddFrom(set.Waves, into);
            }
        }

        /// <summary>Adds every enemy the waves spawn, and the minions of the bosses among them.</summary>
        public static void AddFrom(IEnumerable<WaveDefinition> waves, HashSet<EnemyDefinition> into)
        {
            if (waves == null)
            {
                return;
            }

            foreach (WaveDefinition wave in waves)
            {
                if (wave == null)
                {
                    continue;
                }

                foreach (SpawnGroup group in wave.Groups)
                {
                    Add(group.Enemy, into);
                }
            }
        }

        /// <summary>
        /// The catalog's enemies, in the catalog's order, that pass the filter: in <paramref name="present"/> when it is
        /// given (null lists every creature), and never a boss the player has not met yet.
        /// </summary>
        public static void Filter(IReadOnlyList<EnemyDefinition> catalog, HashSet<EnemyDefinition> present, Func<EnemyDefinition, bool> bossSeen, List<EnemyDefinition> into)
        {
            into.Clear();
            foreach (EnemyDefinition enemy in catalog)
            {
                if (enemy == null || (present != null && !present.Contains(enemy)))
                {
                    continue;
                }

                if (enemy.IsBoss && bossSeen != null && !bossSeen(enemy))
                {
                    continue;
                }

                into.Add(enemy);
            }
        }

        private static void Add(EnemyDefinition enemy, HashSet<EnemyDefinition> into)
        {
            if (enemy == null || !into.Add(enemy))
            {
                return;
            }

            // A boss's summoned minions come with it: they walk the same map.
            foreach (BossAbilityDefinition ability in enemy.Abilities)
            {
                if (ability != null)
                {
                    Add(ability.Minion, into);
                }
            }
        }
    }
}
