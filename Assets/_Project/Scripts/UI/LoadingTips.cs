using System.Collections.Generic;
using System.Text;
using Game.Enemies;
using Game.Levels;

namespace Game.UI
{
    /// <summary>
    /// The advice the loading screen shows, taken from the story's own hint data so no line is written twice: the closing
    /// line of every scan hint (what to build against a creature) and of every gate tip. Hints about creatures the level
    /// being loaded sends on this difficulty come first, so the tip is about what is coming; the rest follow. Pure.
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
                if (TryClosingLine(hint.Dialogue, out LoadingTip tip))
                {
                    (hint.Enemy != null && coming.Contains(hint.Enemy) ? tips : later).Add(tip);
                }
            }

            foreach (GateTip gate in story.GateTips)
            {
                if (TryClosingLine(gate.Dialogue, out LoadingTip tip))
                {
                    later.Add(tip);
                }
            }

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
                count += hint.Enemy != null && coming.Contains(hint.Enemy) && TryClosingLine(hint.Dialogue, out _) ? 1 : 0;
            }

            return count;
        }

        // The last line of a hint is the advice; the lines before it set the scene.
        private static bool TryClosingLine(DialogueDefinition dialogue, out LoadingTip tip)
        {
            tip = default;
            if (dialogue == null || dialogue.Lines.Count == 0)
            {
                return false;
            }

            DialogueLine line = dialogue.Lines[dialogue.Lines.Count - 1];
            if (line.Speaker == null || line.Segments == null)
            {
                return false;
            }

            var text = new StringBuilder();
            foreach (DialogueSegment segment in line.Segments)
            {
                text.Append(segment.Text);
            }

            string words = text.ToString().Trim();
            if (words.Length == 0 || words.Length > MaxLength)
            {
                return false;
            }

            tip = new LoadingTip(line.Speaker.DisplayName, line.Speaker.RoleColour, words);
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
