namespace Game.Enemies
{
    /// <summary>The special powers a boss can carry.</summary>
    public enum BossAbilityKind
    {
        /// <summary>A shockwave that damages every structure within a radius.</summary>
        Pulse = 0,

        /// <summary>A beam that locks onto the nearest structure or the hero and burns it for a while.</summary>
        Laser = 1,

        /// <summary>Pushes smaller creatures out of its body, which continue down the boss's road.</summary>
        Spawn = 2,

        /// <summary>Hurls a bolt at each of the nearest few structures in reach.</summary>
        Volley = 3,

        /// <summary>A call that hurries every living creature on the field for a while.</summary>
        Rally = 4,

        /// <summary>Grows a share of its own armour back.</summary>
        Regrow = 5,

        /// <summary>Sheds minions each time its health drops past another share of the whole.</summary>
        Split = 6,
    }
}
