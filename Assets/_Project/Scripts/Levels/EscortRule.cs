using Game.Balance;
using Game.Enemies;

namespace Game.Levels
{
    /// <summary>
    /// The escort rule of a level boss, as the spawner, the lab and the tests read it (round 88b): the difficulty's escort
    /// worth with the growth, the plated share, the cap and the bounty of the balance rules, which boss it binds, and which
    /// creature may walk in as the plated part of a brood.
    /// </summary>
    public static class EscortRule
    {
        public static EscortSettings For(DifficultyProfile difficulty, BalanceRules rules) => new EscortSettings(
            difficulty.EscortWorth,
            rules.EscortStartWeight,
            rules.EscortEndWeight,
            rules.EscortHeavyShare,
            rules.EscortMostOfWorth,
            rules.EscortBountyShare);

        /// <summary>
        /// True for a level boss: a boss without boss data. A continent boss stages its own calls (its Spawn power keeps its
        /// count and the difficulty's boss escort share) and is never put under the rule.
        /// </summary>
        public static bool Binds(EnemyDefinition definition) => definition != null && definition.IsBoss && definition.Boss == null;

        /// <summary>The creature a level boss's brood is made of: the minion of its Spawn power, or null when it carries none.</summary>
        public static EnemyDefinition CreatureOf(EnemyDefinition boss)
        {
            foreach (BossAbilityDefinition ability in boss.Abilities)
            {
                if (ability != null && ability.Kind == BossAbilityKind.Spawn && ability.Minion != null)
                {
                    return ability.Minion;
                }
            }

            return null;
        }

        /// <summary>True for a creature that can be the plated part of a brood: an ordinary walker with armour.</summary>
        public static bool IsPlatedWalker(EnemyDefinition definition) =>
            definition != null && !definition.IsBoss && !definition.Flies && definition.Armor > 0f;

        /// <summary>
        /// The plated walker of a brood is the toughest one (body plus armour) the level has sent so far: a creature the
        /// player has met on this map, never a new one. Returns the tougher of the two; either may be null.
        /// </summary>
        public static EnemyDefinition Tougher(EnemyDefinition best, EnemyDefinition candidate)
        {
            if (!IsPlatedWalker(candidate))
            {
                return best;
            }

            return best == null || candidate.MaxHealth + candidate.Armor > best.MaxHealth + best.Armor ? candidate : best;
        }
    }
}
