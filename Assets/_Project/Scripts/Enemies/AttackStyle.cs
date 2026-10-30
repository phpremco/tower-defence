namespace Game.Enemies
{
    /// <summary>How a creature's attack looks. Logic deals the damage the same way; the view plays the style.</summary>
    public enum AttackStyle
    {
        /// <summary>Lunges and snaps its jaws.</summary>
        Bite = 0,

        /// <summary>Whips its tail over its back and sprays from the sting.</summary>
        Sting = 1,

        /// <summary>Spits a glowing bolt that flies to the target.</summary>
        Bolt = 2,

        /// <summary>Lashes a tail sideways.</summary>
        Whip = 3,

        /// <summary>Throws its whole body at the target.</summary>
        Slam = 4,

        /// <summary>Raises a blade and brings it down.</summary>
        Slash = 5,
    }
}
