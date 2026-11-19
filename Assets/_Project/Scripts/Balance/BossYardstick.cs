using System;

namespace Game.Balance
{
    /// <summary>
    /// What a boss of a level is held against (round 86): the toughest ordinary creatures the player has met in the level
    /// so far, as they walked in, and the one rule that sizes the boss from them. A boss costs the guns of the player
    /// <see cref="BalanceRules.BossOverRegular"/> times what the toughest ordinary creature cost them, under every row of
    /// the combat rule table at once: under shells, which strip armour point for point and land in full in the air (the
    /// health bar, body plus armour), and under light fire, which pays 1 / <see cref="BalanceRules.LightFireOnArmour"/>
    /// points for a point of armour and lands <see cref="BalanceRules.LightFireInTheAir"/> of itself on whatever flies.
    /// The two rows may name different creatures. Round 85 held the boss on the bar alone: the stone heavy of the first
    /// levels is three quarters armour and a boss a fifth, so under a Gun Turret or a Laser a boss of three bars was 1.4
    /// of those heavies. The creatures of an air raid are not met here (a raid is another kind of wave, with a multiplier
    /// of its own that the band does not hold). A walking boss is held against the walkers; a flying boss against
    /// everything met in the ground waves, and for it the shells decide as a rule: light fire already pays five times for
    /// a boss in the air and once for the walkers it is compared with.
    /// </summary>
    public sealed class BossYardstick
    {
        private readonly float _light;
        private readonly float _lightAir;
        private float _walkerBar;
        private float _walkerLight;
        private float _anyBar;
        private float _anyLight;

        public BossYardstick(BalanceRules rules)
        {
            _light = Math.Max(0.01f, rules.LightFireOnArmour);
            _lightAir = Math.Max(0.01f, rules.LightFireInTheAir);
        }

        /// <summary>What a creature costs a gun that works on armour at this share: its body plus its armour at that price.</summary>
        public static float Cost(EnemyStats enemy, float armourFactor) => enemy.Health + enemy.Armor / Math.Max(0.01f, armourFactor);

        /// <summary>What a creature costs under shells: its bar.</summary>
        public static float UnderShells(EnemyStats enemy) => Cost(enemy, 1f);

        /// <summary>What a creature costs under light fire: armour at its price, and all of it at the air share when it flies.</summary>
        public float UnderLightFire(EnemyStats enemy) => Cost(enemy, _light) / (enemy.Flies ? _lightAir : 1f);

        /// <summary>The ordinary creatures of a wave are met, at the multiplier they walk in with.</summary>
        public void Meet(WaveSpec wave, float multiplier)
        {
            foreach (GroupSpec group in wave.Groups)
            {
                EnemyStats enemy = group.Enemy;
                if (enemy == null || enemy.IsBoss)
                {
                    continue;
                }

                float bar = UnderShells(enemy) * multiplier;
                float light = UnderLightFire(enemy) * multiplier;
                _anyBar = Math.Max(_anyBar, bar);
                _anyLight = Math.Max(_anyLight, light);
                if (!enemy.Flies)
                {
                    _walkerBar = Math.Max(_walkerBar, bar);
                    _walkerLight = Math.Max(_walkerLight, light);
                }
            }
        }

        /// <summary>True once a creature this boss is held against has walked in.</summary>
        public bool HasMet(bool bossFlies) => (bossFlies ? _anyBar : _walkerBar) > 0f;

        /// <summary>The toughest creature met under shells (its bar) for a boss that walks or flies.</summary>
        public float ToughestBar(bool bossFlies) => bossFlies ? _anyBar : _walkerBar;

        /// <summary>The toughest creature met under light fire for a boss that walks or flies.</summary>
        public float ToughestLight(bool bossFlies) => bossFlies ? _anyLight : _walkerLight;

        /// <summary>
        /// The multiple of its book health and armour the boss walks in with: the least at which it costs
        /// <paramref name="over"/> times the toughest creature met under shells and under light fire both.
        /// </summary>
        public float Multiple(EnemyStats boss, float over)
        {
            float bar = UnderShells(boss);
            float light = UnderLightFire(boss);
            if (bar <= 0f || light <= 0f)
            {
                return 0f;
            }

            return over * Math.Max(ToughestBar(boss.Flies) / bar, ToughestLight(boss.Flies) / light);
        }

        /// <summary>How many times the toughest creature met the boss costs at this multiple, under shells and under light fire.</summary>
        public (float Bar, float Light) Over(EnemyStats boss, float multiple)
        {
            float bar = ToughestBar(boss.Flies);
            float light = ToughestLight(boss.Flies);
            return (bar > 0f ? UnderShells(boss) * multiple / bar : 0f, light > 0f ? UnderLightFire(boss) * multiple / light : 0f);
        }
    }
}
