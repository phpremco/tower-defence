namespace Game.Core
{
    /// <summary>What a clip of an imported model is for, so views ask for a role instead of a clip name.</summary>
    public enum ModelClipRole
    {
        /// <summary>The travel cycle: walk, run, crawl, fly or float. Loops in place; its speed follows the ground speed.</summary>
        Move = 0,

        /// <summary>Standing still. Loops.</summary>
        Idle = 1,

        /// <summary>A short flinch on a hit, blended over the cycle that is playing.</summary>
        Hit = 2,

        /// <summary>The fall, played once and held on its last frame.</summary>
        Death = 3,

        /// <summary>The creature's own strike (a sting, a tail lash), played on an attack.</summary>
        Attack = 4,

        /// <summary>A boss power, matched to the power by its trigger name.</summary>
        Power = 5,

        /// <summary>Curling up into a ball.</summary>
        Curl = 6,

        /// <summary>Rolling as a ball. Loops; its speed follows the ground speed.</summary>
        Roll = 7,

        /// <summary>Opening up out of the ball.</summary>
        Uncurl = 8,

        /// <summary>Settling from floating onto the ground; played backwards to lift off again.</summary>
        Land = 9,

        /// <summary>Resting on the ground after a landing. Loops.</summary>
        Grounded = 10,

        /// <summary>Raising a shield, held while it lasts.</summary>
        Block = 11,
    }
}
