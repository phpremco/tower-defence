using System;
using System.Collections.Generic;
using Game.Enemies;

namespace Game.Levels
{
    /// <summary>
    /// The first-sighting rule of the scan hints: a creature's scan plays the first time the creature appears in the
    /// player's campaign. "First" is the save's seen-dialogue flag, set when the scan ends or is skipped, so a scan lost
    /// with a lost level comes again next time. Pure and allocation-free, like the trigger selector.
    /// </summary>
    public static class SightingHints
    {
        /// <summary>The scan of a creature in the table, or null when it has none.</summary>
        public static DialogueDefinition Find(IReadOnlyList<ScanHint> hints, EnemyDefinition enemy)
        {
            if (hints == null || enemy == null)
            {
                return null;
            }

            for (int i = 0; i < hints.Count; i++)
            {
                if (hints[i].Enemy == enemy)
                {
                    return hints[i].Dialogue;
                }
            }

            return null;
        }

        /// <summary>
        /// The scan to queue for a creature just sighted, or null: none in the table, already seen in this save, or already
        /// picked for this moment (a level trigger may play the same conversation).
        /// </summary>
        public static DialogueDefinition Pick(IReadOnlyList<ScanHint> hints, EnemyDefinition enemy, Func<string, bool> isSeen, IReadOnlyList<DialogueDefinition> alreadyPicked)
        {
            DialogueDefinition scan = Find(hints, enemy);
            if (scan == null || scan.Lines.Count == 0 || (isSeen != null && isSeen(scan.Id)))
            {
                return null;
            }

            if (alreadyPicked != null)
            {
                for (int i = 0; i < alreadyPicked.Count; i++)
                {
                    if (alreadyPicked[i] == scan)
                    {
                        return null;
                    }
                }
            }

            return scan;
        }
    }
}
