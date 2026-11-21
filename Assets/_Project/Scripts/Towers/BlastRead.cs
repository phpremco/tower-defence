using Game.Core;

namespace Game.Towers
{
    /// <summary>
    /// How many creatures one blast of a splash gun hurts at a level, in the words the placement line, the tower menu, the
    /// upgrade preview and the Codex show (round 88b: the Cannon's shell hurts two creatures and more with every star, and
    /// the player has to be able to see that an upgrade buys it). Read from the tower's data (TowerDefinition.SplashTargetsAt).
    /// </summary>
    public static class BlastRead
    {
        /// <summary>"Blast hits up to N" for a capped splash gun at a level; empty for a gun without a blast or without a cap.</summary>
        public static string Label(TowerDefinition tower, int levelIndex)
        {
            int cap = Cap(tower, levelIndex);
            return cap > 0 ? Loc.Format("tower.blast.up_to", cap) : string.Empty;
        }

        /// <summary>"blast hits up to 2 > 3" when the step to the next level raises the cap; empty when it does not.</summary>
        public static string Step(TowerDefinition tower, int levelIndex)
        {
            int now = Cap(tower, levelIndex);
            int next = Cap(tower, levelIndex + 1);
            return now > 0 && next > now ? Loc.Format("tower.step.blast", now, next) : string.Empty;
        }

        /// <summary>The cap at a level, zero for a tower without a blast, without a cap or without that level.</summary>
        public static int Cap(TowerDefinition tower, int levelIndex)
        {
            if (tower == null || tower.SplashRadius <= 0f || levelIndex < 0 || levelIndex > tower.MaxLevelIndex)
            {
                return 0;
            }

            return tower.SplashTargetsAt(levelIndex);
        }
    }
}
