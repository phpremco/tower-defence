using System;

namespace Game.Balance
{
    /// <summary>
    /// How dangerous a creature is, in threat points, and what it pays. Threat prices armour for most of its hit points,
    /// flying for a little more (fewer guns reach it) and a creature that hurts the base or structures for more still,
    /// so a glass cannon is not priced as fodder. Kill money follows threat, so a new creature needs no hand-set reward.
    /// </summary>
    public static class Threat
    {
        public static float Of(EnemyStats enemy, BalanceRules rules) =>
            (enemy.Health + enemy.Armor * rules.ArmorThreatShare) / rules.HealthPerThreat
            * (enemy.Flies ? rules.AirThreatFactor : 1f)
            * (1f + enemy.BaseDamage * rules.BaseDamageThreat + enemy.AttackDamage * rules.AttackDamageThreat);

        /// <summary>Kill money of a creature: grows a little slower than its threat; bosses pay a multiple.</summary>
        public static int Reward(EnemyStats enemy, BalanceRules rules)
        {
            double money = rules.MoneyPerThreat * Math.Pow(Of(enemy, rules), rules.RewardExponent);
            if (enemy.IsBoss)
            {
                money *= rules.BossRewardFactor;
            }

            return Math.Max(1, (int)Math.Round(money, MidpointRounding.AwayFromZero));
        }
    }
}
