namespace Game.Hero
{
    /// <summary>What a hero ability does when it is used. Values are only ever appended (Field Repair follows later).</summary>
    public enum HeroAbilityKind
    {
        /// <summary>A ring of force around the hero that hurts everything within its radius.</summary>
        Shockwave = 0,

        /// <summary>Gives the guarded gate (or the base) back a share of its health.</summary>
        GateRepair = 1,
    }
}
