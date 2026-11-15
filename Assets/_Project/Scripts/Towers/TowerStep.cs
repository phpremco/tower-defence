namespace Game.Towers
{
    /// <summary>
    /// The one thing an in-level upgrade step improves (round 67, Remco: "each upgrade step improves ONE named thing").
    /// The step's name is data on the level it leads to; the tower's data is checked against it, so a step that says
    /// "fire rate" changes the fire rate and nothing else.
    /// </summary>
    public enum TowerStep
    {
        /// <summary>The level a tower is built at: no step led to it.</summary>
        None = 0,
        Damage = 1,
        FireRate = 2,
        Range = 3,

        /// <summary>A stronger chill (the speed share its hits leave).</summary>
        Slow = 4,

        /// <summary>A stronger boost to the towers around a support mast.</summary>
        Support = 5,

        /// <summary>A structure's refit, several things at once: a gate (health and its gun) and the base's own turret. Never a tower the player builds.</summary>
        Refit = 6,
    }
}
