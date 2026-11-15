namespace Game.Core
{
    /// <summary>
    /// The one rule armour follows (round 64): damage that neither explodes nor pierces is reduced by armour, the rest is
    /// not. The definitions check their own armour factor against it when the data is saved, the tests check every weapon
    /// asset, and the Codex words come from it; the factors themselves are data (the sheet's table per type).
    /// </summary>
    public static class DamageTypeRule
    {
        /// <summary>True for every type a creature's armour turns to a fraction: all but explosions and slugs.</summary>
        public static bool ReducedByArmour(DamageType type) => type != DamageType.Explosive && type != DamageType.Piercing;

        /// <summary>True when a weapon's armour factor agrees with its type: under one when armour reduces it, one or more when not.</summary>
        public static bool Agrees(DamageType type, float armourFactor) => ReducedByArmour(type) ? armourFactor < 1f : armourFactor >= 1f;

        /// <summary>The words for a data warning.</summary>
        public static string Describe(DamageType type) =>
            ReducedByArmour(type) ? $"{type} damage is reduced by armour: its armour factor must be under one" : $"{type} damage works on armour in full: its armour factor must be one or more";
    }
}
