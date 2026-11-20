using System;

namespace Game.Balance
{
    /// <summary>
    /// The floor under a level's boss (round 86; a floor only since round 87, when the boss got a formula of its own,
    /// <see cref="WaveFormula.BossBar"/>): the toughest ordinary walker the player has met in the level so far, as it
    /// walked in. A boss's bar (body plus armour: the health bar the player sees) is never under
    /// <see cref="BalanceRules.BossOverRegular"/> times that walker's bar. Round 86 also held the boss to that multiple
    /// under light fire (armour priced at the bullet row's factor). With an armour pool as large as the body that row
    /// made the floor the rule on 30 of 42 bosses, the old regime back, and beams and bullets no longer share one armour
    /// factor; the floor reads the bar alone. Flyers are not met here: they are another kind of threat, with guns of
    /// their own.
    /// </summary>
    public sealed class BossYardstick
    {
        private float _bar;

        /// <summary>A creature's bar at book health: body plus armour.</summary>
        public static float Bar(EnemyStats enemy) => enemy.Health + enemy.Armor;

        /// <summary>The ordinary walkers of a wave are met, at the multiplier they walk in with.</summary>
        public void Meet(WaveSpec wave, float multiplier)
        {
            foreach (GroupSpec group in wave.Groups)
            {
                EnemyStats enemy = group.Enemy;
                if (enemy == null || enemy.IsBoss || enemy.Flies)
                {
                    continue;
                }

                _bar = Math.Max(_bar, Bar(enemy) * multiplier);
            }
        }

        /// <summary>True once an ordinary walker has walked in.</summary>
        public bool HasMet => _bar > 0f;

        /// <summary>The bar of the toughest walker met.</summary>
        public float ToughestBar => _bar;

        /// <summary>The least multiple of its book bar at which the boss's bar is <paramref name="over"/> times the toughest walker's.</summary>
        public float Multiple(EnemyStats boss, float over)
        {
            float bar = Bar(boss);
            return bar > 0f ? over * _bar / bar : 0f;
        }

        /// <summary>How many times the toughest walker's bar the boss's bar is at this multiple; zero when none was met.</summary>
        public float Over(EnemyStats boss, float multiple) => _bar > 0f ? Bar(boss) * multiple / _bar : 0f;
    }
}
