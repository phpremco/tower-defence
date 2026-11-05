using Game.Levels;

namespace Game.Bosses
{
    /// <summary>
    /// How often a boss has shown each of its signature moments in this fight, read from its record, so a watcher sees a
    /// moment when its count goes up (the hero's barks, Docs/BOSS_INTROS.md). Pure lookup.
    /// </summary>
    public static class BossMoments
    {
        public static int Count(BossRecord record, BossMoment moment)
        {
            switch (moment)
            {
                case BossMoment.Throw:
                    return record.Thrown.Count;
                case BossMoment.Burrow:
                    return record.Burrows;
                case BossMoment.Smash:
                    return record.Smashed.Count;
                case BossMoment.Eggs:
                    return record.EggsLaid;
                case BossMoment.GateFreeze:
                    return record.GatesFrozen;
                case BossMoment.ArmourBreak:
                    return record.ArmourBroken ? 1 : 0;
                case BossMoment.Overgrow:
                    return record.PadsOvergrown;
                case BossMoment.Uproot:
                    return record.Uprooted ? 1 : 0;
                case BossMoment.Split:
                    return record.Splits;
                case BossMoment.Rejoin:
                    return record.Rejoined ? 1 : 0;
                case BossMoment.Adapt:
                    return record.Learned.Count;
                case BossMoment.OnBase:
                    return record.BaseCrushes;
                default:
                    return 0;
            }
        }
    }
}
