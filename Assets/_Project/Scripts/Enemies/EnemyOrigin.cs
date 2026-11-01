namespace Game.Enemies
{
    /// <summary>Where an enemy entered the field, which decides how it climbs into view.</summary>
    public enum EnemyOrigin
    {
        Gate = 0,
        Water = 1,
        Lava = 2,
        Quicksand = 3,
        Ground = 4,

        /// <summary>An element nest: the creature bursts out in the colour of its element.</summary>
        Nest = 5,
        Ice = 6,
        Bog = 7,
    }
}
