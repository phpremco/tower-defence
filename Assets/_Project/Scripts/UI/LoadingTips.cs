using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;

namespace Game.UI
{
    /// <summary>
    /// The advice the loading screen shows, taken from the story's own hint data: the loading tip of every scan hint (what
    /// to build against a creature), of the gate tips, the high-ground hint and the rift warning. A tip is the hint's
    /// advice written to stand on its own (<see cref="DialogueDefinition.LoadingTip"/>, round 65: the closing line of a
    /// conversation says "it" about a creature the loading screen never showed). Hints about creatures the level being
    /// loaded sends on this difficulty come first, so the tip is about what is coming; the rest follow. Pure.
    /// </summary>
    public static class LoadingTips
    {
        /// <summary>A tip longer than this does not fit its box at a readable size and is left out.</summary>
        public const int MaxLength = 150;

        public static List<LoadingTip> For(StoryDefinition story, LevelDefinition level, DifficultyProfile difficulty)
        {
            var tips = new List<LoadingTip>();
            if (story == null)
            {
                return tips;
            }

            HashSet<EnemyDefinition> coming = Coming(level, difficulty);
            var later = new List<LoadingTip>();
            foreach (ScanHint hint in story.ScanHints)
            {
                if (TryTip(hint.Dialogue, out LoadingTip tip))
                {
                    (hint.Enemy != null && coming.Contains(hint.Enemy) ? tips : later).Add(tip);
                }
            }

            foreach (GateTip gate in story.GateTips)
            {
                AddTip(gate.Dialogue, later);
            }

            AddTip(story.HighGroundTip, later);
            AddTip(story.RearNestWarning, later);
            tips.AddRange(later);
            return tips;
        }

        /// <summary>How many of the tips are about creatures of the level itself: the screen opens on one of those.</summary>
        public static int AboutLevel(StoryDefinition story, LevelDefinition level, DifficultyProfile difficulty)
        {
            if (story == null)
            {
                return 0;
            }

            HashSet<EnemyDefinition> coming = Coming(level, difficulty);
            int count = 0;
            foreach (ScanHint hint in story.ScanHints)
            {
                count += hint.Enemy != null && coming.Contains(hint.Enemy) && TryTip(hint.Dialogue, out _) ? 1 : 0;
            }

            return count;
        }

        private static void AddTip(DialogueDefinition dialogue, List<LoadingTip> into)
        {
            if (TryTip(dialogue, out LoadingTip tip))
            {
                into.Add(tip);
            }
        }

        // The hint's own loading tip, in the name of whoever closes the conversation (the one giving the advice).
        private static bool TryTip(DialogueDefinition dialogue, out LoadingTip tip)
        {
            tip = default;
            if (dialogue == null || dialogue.Lines.Count == 0)
            {
                return false;
            }

            SpeakerDefinition speaker = dialogue.Lines[dialogue.Lines.Count - 1].Speaker;
            string words = dialogue.LoadingTip.Trim();
            if (speaker == null || words.Length == 0 || words.Length > MaxLength)
            {
                return false;
            }

            tip = new LoadingTip(speaker.DisplayName, speaker.RoleColour, words);
            return true;
        }

        private static HashSet<EnemyDefinition> Coming(LevelDefinition level, DifficultyProfile difficulty)
        {
            var coming = new HashSet<EnemyDefinition>();
            WaveDefinition[] waves = level != null && difficulty != null ? level.GetWaves(difficulty) : null;
            if (waves == null)
            {
                return coming;
            }

            foreach (WaveDefinition wave in waves)
            {
                foreach (SpawnGroup group in wave.Groups)
                {
                    if (group.Enemy != null)
                    {
                        coming.Add(group.Enemy);
                    }
                }
            }

            return coming;
        }
    }
}
