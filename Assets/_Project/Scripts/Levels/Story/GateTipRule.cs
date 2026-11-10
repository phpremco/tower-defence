using System;
using System.Collections.Generic;

namespace Game.Levels
{
    /// <summary>
    /// When Dr. Voss's gate tips play: the upgrade tip once a hurt gate can be upgraded with the money in hand (the lesson
    /// lands when it can be acted on), the move tip once another gate is hurt while the hero's own gate stands quiet. Each
    /// plays once per save (the seen-dialogue flag, set when it ends or is skipped). Pure and allocation-free.
    /// </summary>
    public static class GateTipRule
    {
        /// <param name="heroGate">Index of the gate the hero guards, or -1 at the base.</param>
        /// <param name="heroReady">The hero stands and could be sent (not down, not in flight).</param>
        public static bool Applies(GateTip tip, IReadOnlyList<GateTipGate> gates, int heroGate, bool heroReady)
        {
            if (gates == null)
            {
                return false;
            }

            switch (tip.Kind)
            {
                case GateTipKind.UpgradeGate:
                    for (int i = 0; i < gates.Count; i++)
                    {
                        if (gates[i].Standing && gates[i].UpgradeAffordable && gates[i].Health < tip.HurtBelow)
                        {
                            return true;
                        }
                    }

                    return false;
                case GateTipKind.MoveHero:
                    if (!heroReady || heroGate < 0 || heroGate >= gates.Count || !gates[heroGate].Standing || gates[heroGate].Health < tip.QuietAbove)
                    {
                        return false;
                    }

                    for (int i = 0; i < gates.Count; i++)
                    {
                        if (i != heroGate && gates[i].Standing && gates[i].Health < tip.HurtBelow)
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        /// <summary>The first tip whose moment has come and that is neither seen in this save nor already picked, or null.</summary>
        public static DialogueDefinition Pick(IReadOnlyList<GateTip> tips, IReadOnlyList<GateTipGate> gates, int heroGate, bool heroReady, Func<string, bool> isSeen, IReadOnlyList<DialogueDefinition> alreadyPicked)
        {
            if (tips == null)
            {
                return null;
            }

            for (int i = 0; i < tips.Count; i++)
            {
                DialogueDefinition dialogue = tips[i].Dialogue;
                if (dialogue == null || dialogue.Lines.Count == 0 || (isSeen != null && isSeen(dialogue.Id)) || Contains(alreadyPicked, dialogue))
                {
                    continue;
                }

                if (Applies(tips[i], gates, heroGate, heroReady))
                {
                    return dialogue;
                }
            }

            return null;
        }

        private static bool Contains(IReadOnlyList<DialogueDefinition> list, DialogueDefinition dialogue)
        {
            if (list == null)
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == dialogue)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
