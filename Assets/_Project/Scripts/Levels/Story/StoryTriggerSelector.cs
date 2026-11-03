using System;
using System.Collections.Generic;

namespace Game.Levels
{
    /// <summary>
    /// Picks the dialogues a level event starts. Pure and allocation-free: the caller owns the per-run fired flags
    /// and the result list, so it can run on every spawn.
    /// </summary>
    public static class StoryTriggerSelector
    {
        /// <summary>
        /// Appends, in trigger order, the dialogue of every trigger that matches the event, has not fired this run and
        /// is not a once-per-save dialogue already seen. Every trigger that matches is marked as fired, played or not.
        /// Returns how many dialogues were appended.
        /// </summary>
        public static int Select(IReadOnlyList<StoryTrigger> triggers, StoryEvent evt, Func<string, bool> isSeen, bool[] firedThisRun, List<DialogueDefinition> results)
        {
            int added = 0;
            for (int i = 0; i < triggers.Count; i++)
            {
                StoryTrigger trigger = triggers[i];
                if (firedThisRun[i] || trigger.Dialogue == null || !Matches(trigger, evt))
                {
                    continue;
                }

                firedThisRun[i] = true;
                if (trigger.Repeat == StoryRepeat.OncePerSave && isSeen(trigger.Dialogue.Id))
                {
                    continue;
                }

                // Two triggers may share a dialogue; one event plays it once.
                if (!results.Contains(trigger.Dialogue))
                {
                    results.Add(trigger.Dialogue);
                    added++;
                }
            }

            return added;
        }

        public static bool Matches(StoryTrigger trigger, StoryEvent evt)
        {
            if (trigger.Kind != evt.Kind)
            {
                return false;
            }

            switch (trigger.Kind)
            {
                case StoryTriggerKind.WaveStart:
                    return trigger.Wave == evt.Wave;
                case StoryTriggerKind.FirstSighting:
                    return trigger.Enemy != null && trigger.Enemy == evt.Enemy;
                case StoryTriggerKind.FirstUse:
                    return trigger.PathObject != null && trigger.PathObject == evt.PathObject;
                case StoryTriggerKind.BossSpawn:
                case StoryTriggerKind.BossDefeated:
                    return trigger.Enemy == null || trigger.Enemy == evt.Enemy;
                default:
                    return true;
            }
        }
    }
}
