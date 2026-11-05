namespace Game.Levels
{
    /// <summary>What a boss's arrival gets: the full intro beat the first time, a bark afterwards.</summary>
    public enum BossMeeting
    {
        /// <summary>Never met before: slow motion, the hero's reaction, the cut-in, ORA and Voss answer, the Codex toast.</summary>
        First,

        /// <summary>Met before: only a short bark, no slow motion.</summary>
        Again,
    }

    /// <summary>
    /// The rules of the boss intro (Docs/BOSS_INTROS.md), apart from how it looks: which kind of meeting it is, and when
    /// a beat or a bark may start at all (never during a pause, a held story box, a cutscene or after the level ended).
    /// The first-seen flag is the one the Codex uses (<c>ProgressData.SeenEnemyIds</c>), read before the spawn sets it.
    /// </summary>
    public static class BossIntroRules
    {
        public static BossMeeting MeetingFor(bool seenBefore) => seenBefore ? BossMeeting.Again : BossMeeting.First;

        /// <summary>True when the field is live: not paused, not held by a story box or a panel, not in a cutscene, still running.</summary>
        public static bool FieldIsLive(bool paused, bool held, bool cutscene, LevelOutcome outcome) =>
            !paused && !held && !cutscene && outcome == LevelOutcome.Running;

        /// <summary>A pick from a pool of lines by a roll in [0, 1), or null when the pool is empty.</summary>
        public static string Pick(System.Collections.Generic.IReadOnlyList<string> lines, float roll)
        {
            if (lines == null || lines.Count == 0)
            {
                return null;
            }

            int index = (int)(roll * lines.Count);
            return lines[index < 0 ? 0 : index >= lines.Count ? lines.Count - 1 : index];
        }
    }
}
