using System.Collections.Generic;
using Game.Enemies;

namespace Game.Levels
{
    /// <summary>
    /// What the player is told about the next wave (round 78): which creature types come and how many of each, with the
    /// flyers, the plated ones and a boss told apart. Read from the wave's own data, so it always matches what calling the
    /// wave would bring. It deliberately leaves out where the creatures come from (a road, a pool, a rift behind the gate):
    /// that stays a surprise, announced on the field when it happens. Since round 88c it does say which creature a level
    /// boss sends out as its brood: a plus on that creature's number when the wave sends it anyway, else an entry without
    /// a number right after the boss. The brood is a rule of every boss wave now, and a player who lets the boss walk
    /// should know what the road behind it fills with.
    /// The HUD draws this list and the reference players of the balance probes read the same list, nothing more.
    /// </summary>
    public static class WavePreview
    {
        /// <summary>
        /// The creature types the HUD's strip has places for. A wave lists no more of its own (a test holds the data to
        /// it), and a brood the wave does not send itself is named only while a place is left: the wave's own creatures
        /// are what a call brings for certain.
        /// </summary>
        public const int Places = 6;

        /// <summary>
        /// The creature types of a wave on a difficulty, one entry per type with its groups added up, the ones a player has to
        /// answer first: a boss, then its brood, then flyers, then plated creatures, then the rest; within a rank the larger number first, and
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

            AddBroods(difficulty, into);
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

        // The brood of every level boss in the list, once per creature, when the difficulty's escort rule sends one.
        private static void AddBroods(DifficultyProfile difficulty, List<WavePreviewEntry> into)
        {
            if (difficulty.EscortWorth <= 0f)
            {
                return;
            }

            int listed = into.Count;
            for (int i = 0; i < listed; i++)
            {
                EnemyDefinition boss = into[i].Enemy;
                EnemyDefinition creature = EscortRule.Binds(boss) ? EscortRule.CreatureOf(boss) : null;
                if (creature != null)
                {
                    MarkBrood(into, creature);
                }
            }
        }

        // A creature the wave already lists is marked on its own entry (its number gets a plus); only a creature the wave
        // does not send gets an entry of its own, without a number. So a brood never costs the strip a second place.
        private static void MarkBrood(List<WavePreviewEntry> entries, EnemyDefinition creature)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Enemy == creature)
                {
                    entries[i] = new WavePreviewEntry(creature, entries[i].Count, true);
                    return;
                }
            }

            if (entries.Count < Places)
            {
                entries.Add(new WavePreviewEntry(creature, 0, true));
            }
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

        private static int Rank(WavePreviewEntry entry) => entry.Boss ? 0 : entry.BroodOnly ? 1 : entry.Flies ? 2 : entry.Armoured ? 3 : 4;
    }
}
