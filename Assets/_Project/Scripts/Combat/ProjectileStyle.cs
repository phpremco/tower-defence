namespace Game.Combat
{
    /// <summary>How a shot looks and sounds. The logic is the same for all of them; only the view and the audio differ.</summary>
    public enum ProjectileStyle
    {
        /// <summary>A short bright streak of energy.</summary>
        Bolt = 0,

        /// <summary>A dark shell with a hot tracer and a puff of smoke.</summary>
        Shell = 1,

        /// <summary>A missile with a flame and a smoke trail.</summary>
        Rocket = 2,

        /// <summary>A beam of light from the muzzle to the target.</summary>
        Laser = 3,

        /// <summary>A shard of ice shedding frost.</summary>
        Frost = 4,

        /// <summary>A jagged, flickering arc.</summary>
        Lightning = 5,

        /// <summary>A long white-hot slug with a streak behind it.</summary>
        Rail = 6,

        /// <summary>A heavy shell lobbed on a high arc.</summary>
        Mortar = 7,

        /// <summary>A white-hot flak tracer with an amber edge and no smoke, bursting near a flyer.</summary>
        Flak = 8,
    }
}
