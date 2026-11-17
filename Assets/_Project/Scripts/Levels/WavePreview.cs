using System.Collections.Generic;
using Game.Enemies;

namespace Game.Levels
{
    /// <summary>
    /// What the player is told about the next wave (round 78): which creature types come and how many of each, with the
    /// flyers, the plated ones and a boss told apart. Read from the wave's own data, so it always matches what calling the
    /// wave would bring. It deliberately leaves out where the creatures come from (a road, a pool, a rift behind the gate)
    /// and what a boss calls in during the fight: those stay surprises, announced on the field when they happen.
    /// The HUD draws this list and the reference players of the balance probes read the same list, nothing more.
    /// </summary>
    public static class WavePreview
    {
        /// <summary>
        /// The creature types of a wave on a difficulty, one entry per type with its groups added up, the ones a player has to
        /// answer first: a boss, then flyers, then plated creatures, then the rest; within a rank the larger number first, and
        /// the wave's own order between equals. Empty for no wave (after the last one).
        /// </summary>
        public static void Collect(WaveDefinition wave, DifficultyProfile difficulty, List<WavePreviewEntry> into)
        {
            into.Clear();
            if (wave == null || difficulty == null)
            {
                return;
            }

            foreach (SpawnGroup group in wave.Groups)
            {
                if (group.Enemy != null)
                {
                    Add(group.Enemy, WaveScheduler.CountOf(group, difficulty), into);
                }
            }

            SortByRank(into);
        }

        /// <summary>The entries a strip of <paramref name="capacity"/> places shows; the rest (the lowest ranks) are counted in <paramref name="hidden"/>.</summary>
        public static int Shown(IReadOnlyList<WavePreviewEntry> entries, int capacity, out int hidden)
        {
            int shown = entries.Count <= capacity ? entries.Count : System.Math.Max(0, capacity - 1);
            hidden = entries.Count - shown;
            return shown;
        }

        /// <summary>How many of the listed creatures fly, for a reader who sizes its sky guns by them.</summary>
        public static int Flyers(IReadOnlyList<WavePreviewEntry> entries)
        {
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                count += entries[i].Flies ? entries[i].Count : 0;
            }

            return count;
        }

        /// <summary>How many of the listed creatures wear armour.</summary>
        public static int Plated(IReadOnlyList<WavePreviewEntry> entries)
        {
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                count += entries[i].Armoured ? entries[i].Count : 0;
            }

            return count;
        }

        private static void Add(EnemyDefinition enemy, int count, List<WavePreviewEntry> into)
        {
            for (int i = 0; i < into.Count; i++)
            {
                if (into[i].Enemy == enemy)
                {
                    into[i] = new WavePreviewEntry(enemy, into[i].Count + count);
                    return;
                }
            }

            into.Add(new WavePreviewEntry(enemy, count));
        }

        // An insertion sort: stable (the wave's order between equals), no allocation, and a wave lists a handful of types.
        private static void SortByRank(List<WavePreviewEntry> entries)
        {
            for (int i = 1; i < entries.Count; i++)
            {
                WavePreviewEntry entry = entries[i];
                int j = i - 1;
                while (j >= 0 && Before(entry, entries[j]))
                {
                    entries[j + 1] = entries[j];
                    j--;
                }

                entries[j + 1] = entry;
            }
        }

        private static bool Before(WavePreviewEntry a, WavePreviewEntry b)
        {
            int rankA = Rank(a);
            int rankB = Rank(b);
            return rankA != rankB ? rankA < rankB : a.Count > b.Count;
        }

        private static int Rank(WavePreviewEntry entry) => entry.Boss ? 0 : entry.Flies ? 1 : entry.Armoured ? 2 : 3;
    }
}
