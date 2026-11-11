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

        /// <summary>A boss clambering up a ledge, played once; the pivot stays put and the owner moves it by the clip's end offset.</summary>
        ClimbUp = 41,

        /// <summary>The braced crawl on top of a ledge, a gate or the base roof. Loops; its speed follows the ground speed.</summary>
        ClimbLoop = 42,

        /// <summary>A boss climbing down off a ledge, played once, moved by its end offset at the end.</summary>
        ClimbDown = 43,

        /// <summary>A boss diving from the bank into a pool, played once, moved by its end offset at the end.</summary>
        DiveIn = 44,

        /// <summary>Hidden under a liquid's surface. Loops in place while the owner moves it.</summary>
        Submerged = 45,

        /// <summary>Bursting out of a pool onto the bank, played once, moved by its end offset at the end.</summary>
        SurfaceBurst = 46,

        /// <summary>Drilling down into the ground, played once, moved by its end offset at the end.</summary>
        BurrowDown = 47,

        /// <summary>Travelling under the ground with only the crest showing. Loops in place while the owner moves it.</summary>
        BurrowTravel = 48,

        /// <summary>Breaking out of the ground beside a structure and flinging it up, played once.</summary>
        SurfaceThrow = 49,

        /// <summary>A boss's long hunting stride off the road. Loops; its speed follows the ground speed.</summary>
        HuntMove = 50,

        /// <summary>A boss's blow that turns the structure ahead into a wreck at its moment, played once.</summary>
        Smash = 51,

        /// <summary>A boss laying eggs from its tail: the cue is the first egg, the moment the last, played once.</summary>
        LayEggs = 52,

        /// <summary>A boss breathing frost over the gate ahead: it becomes an ice block at the moment, played once.</summary>
        FreezeGate = 53,

        /// <summary>A boss breaking through the frozen gate ahead: the gate shatters at the moment, played once and moved by its end offset.</summary>
        ShatterThrough = 54,

        /// <summary>A boss's armour plates bursting off for good (the moment: all off), played once.</summary>
        ArmourBreak = 55,

        /// <summary>An egg cracking open: the creature inside leaves at the moment, played once and held.</summary>
        Hatch = 56,

        /// <summary>A boss standing rooted, creeping on by growing roots. Loops; its speed follows the ground speed.</summary>
        Rooted = 57,

        /// <summary>A rooted boss tearing itself out of the ground, played once.</summary>
        Uproot = 58,

        /// <summary>A boss opening up and firing a volley of pods: the cue is the first shot, the moment the last, played once.</summary>
        SporeVolley = 59,

        /// <summary>A boss bursting into parts that go their own way: the moment is the burst, played once.</summary>
        Split = 60,

        /// <summary>Parts pulling back together into the whole (or a part flying back into it): the moment is when it forms, played once.</summary>
        Rejoin = 61,

        /// <summary>A boss pulling itself up onto the base: the moment is when the base takes the blow, played once.</summary>
        RiseOntoBase = 62,

        /// <summary>A boss adapting to what hurt it at a phase change: the moment is the pulse, played once.</summary>
        Adapt = 63,

        /// <summary>A boss lying on the base it has climbed onto, striking it. Loops in place.</summary>
        OnBaseLoop = 64,

        /// <summary>Climbing up onto a gate (not a rock ledge): front feet on the gate top, body pulled over the wall.</summary>
        GateMount = 65,

        /// <summary>The loop while it straddles a gate, clinging to its top and sides (in place, never a walk).</summary>
        GateStraddle = 66,

        /// <summary>Climbing down off a gate on its far side.</summary>
        GateDismount = 67,

        /// <summary>A character recoiling from something huge: half a step back, weapon up, a look up (the cue: the flinch), played once.</summary>
        Startled = 68,

        /// <summary>The tense hold after <see cref="Startled"/>: quick breaths, the muzzle trembling. Loops.</summary>
        StartledHold = 69,

        /// <summary>A character who cannot believe his eyes: a hand to the chin guard, a slow head shake (the cue: the hand up), held at its end.</summary>
        Disbelief = 70,

        /// <summary>Pointing and shouting on the upper body over whatever the legs do (the cue: the point), played once.</summary>
        PointShout = 71,

        /// <summary>Lowering the aimed weapon and breathing out (the cue: lowered), played once.</summary>
        LowerWeapon = 72,

        /// <summary>The strike of a creature settled on the ground (a grounded wisp); <see cref="Attack"/> is its floating one.</summary>
        GroundedAttack = 73,

        /// <summary>Drawing the sidearm from its holster to aim (the cue: the grab, where the weapon changes socket), played once.</summary>
        DrawPistol = 74,

        /// <summary>Putting the sidearm back in its holster from the aim (the cue: the release), played once.</summary>
        StowPistol = 75,

        /// <summary>Drawing the blade from the back to the guard (the cue: the grab; the second moment: the blade lights), played once.</summary>
        DrawBlade = 76,

        /// <summary>Putting the blade back on the back from the guard (the cue: the release; the second moment: the blade goes out), played once.</summary>
        StowBlade = 77,

        /// <summary>A jetpack take-off on the spot (the cue: liftoff, where the owner starts to move; the second moment: the thrust lights), played once.</summary>
        JetTakeoff = 78,

        /// <summary>The jetpack flight, in place: the owner moves the body along the flight. Loops.</summary>
        JetFly = 79,

        /// <summary>A jetpack landing (the cue: touchdown, where the owner reaches the ground; the second moment: the thrust goes out), played once.</summary>
        JetLand = 80,

        /// <summary>Stepping out of the landing capsule's hatch and down its ramp to the ground, in place (the owner moves the body along the ramp), played once.</summary>
        CapsuleExit = 81,

        /// <summary>The startle at the brood: a flinch and three uneven steps back, the hand to the holstered pistol's grip, played once (the owner moves the root back by the clip's travel at its first frame).</summary>
        StartleBackstep = 82,

        /// <summary>A nervous draw into a close two-handed aim (the cue: the grab, where the pistol goes to the hand), played once.</summary>
        DrawAimNervous = 83,

        /// <summary>The nervous two-handed aim held: breaths, weight shifts, a trembling muzzle. Loops.</summary>
        AimHoldNervous = 84,

        /// <summary>A look and a quarter turn to the owner's left, head first, then the feet (the owner turns the root by the turn at its first frame), played once.</summary>
        TurnLookLeft = 85,

        /// <summary>A look and a quarter turn to the owner's right (see <see cref="TurnLookLeft"/>), played once.</summary>
        TurnLookRight = 86,
    }
}
