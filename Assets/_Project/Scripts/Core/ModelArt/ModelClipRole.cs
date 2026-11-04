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

        /// <summary>A machine's shot from one barrel (its recoil), matched to the barrel by the muzzle bone in its socket.</summary>
        Fire = 12,

        /// <summary>A machine winding up before a shot or a beam (prongs closing, rails drawing back).</summary>
        Charge = 13,

        /// <summary>A machine's cycle while it keeps firing (a beam's hum, a spinning barrel cluster, alternating barrels). Loops.</summary>
        Firing = 14,

        /// <summary>A structure putting its broken parts back (a gate popping its panels back in), played once.</summary>
        Repair = 15,

        /// <summary>A structure rising from its wreck when it is rebuilt, played once.</summary>
        Build = 16,

        /// <summary>A drone at its job: welding, drilling or firing. Loops while it works.</summary>
        Work = 17,

        /// <summary>A placed object setting itself up (a mine arming, an emitter unfolding, a ball dropping), played once.</summary>
        Deploy = 18,

        /// <summary>The slow travel cycle beside the run (a hero walking a few steps). Loops; its speed follows the ground speed.</summary>
        Walk = 19,

        /// <summary>A hand weapon fired once (the kick), on the upper body over whatever the legs do.</summary>
        Shoot = 20,

        /// <summary>Changing the magazine, on the upper body.</summary>
        Reload = 21,

        /// <summary>A cut with a hand weapon, on the upper body.</summary>
        Slash = 22,

        /// <summary>A hero ability (a shockwave punched into the ground), the whole body.</summary>
        Ability = 23,

        /// <summary>Going down when the health runs out, held on its last frame.</summary>
        Down = 24,

        /// <summary>Getting back up from the last frame of <see cref="Down"/>.</summary>
        Revive = 25,

        /// <summary>A character speaking (jaw, gaze bar). Loops while the line types.</summary>
        Talk = 26,

        /// <summary>A blink, laid over the idle now and then.</summary>
        Blink = 27,

        /// <summary>A hologram breaking up for a moment.</summary>
        Glitch = 28,

        /// <summary>A machine opening up (a capsule lowering its ramp), played once and held.</summary>
        Open = 29,

        /// <summary>Going under the ground (a burrower digging in, a worm diving), played once; the model is hidden after it.</summary>
        Burrow = 30,

        /// <summary>Breaking out of the ground or a liquid (a burrower surfacing, a sealed titan rising), played once.</summary>
        Emerge = 31,

        /// <summary>The travel cycle through a liquid (a swimmer on a flooded road). Loops; its speed follows the ground speed.</summary>
        Swim = 32,

        /// <summary>A boss rearing up and roaring, for its entrance and the alarm, played once.</summary>
        Roar = 33,

        /// <summary>A boss going into its rage when its health runs low, played once before <see cref="EnragedMove"/>.</summary>
        Enrage = 34,

        /// <summary>The travel cycle of an enraged boss. Loops; its speed follows the ground speed.</summary>
        EnragedMove = 35,

        /// <summary>A boss breaking into its next phase (shells blown off), matched to the phase by its trigger, played once.</summary>
        PhaseBreak = 36,

        /// <summary>A creature bursting all its shed parts at once (a full shatter), played once.</summary>
        Shatter = 37,

        /// <summary>A spawn origin shut (one frame, the bind pose).</summary>
        Closed = 38,

        /// <summary>A spawn origin shut but marked, breathing faintly. Loops.</summary>
        Marked = 39,

        /// <summary>A creature coming out of a spawn origin or a route gate (the throat swells), played once.</summary>
        Spawn = 40,
    }
}
