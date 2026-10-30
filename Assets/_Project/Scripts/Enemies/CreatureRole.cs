namespace Game.Enemies
{
    /// <summary>What a moving part of a procedural creature is, which decides how the view animates it.</summary>
    public enum CreatureRole
    {
        /// <summary>A jointed leg: hip and knee swing with the gait, or hang when the creature flies.</summary>
        Leg = 0,

        /// <summary>A jaw, pincer or raptorial arm that opens and snaps shut on a strike.</summary>
        Mandible = 1,

        /// <summary>A feeler that twitches.</summary>
        Antenna = 2,

        /// <summary>A wing that beats.</summary>
        Wing = 3,

        /// <summary>The hind body that breathes.</summary>
        Abdomen = 4,

        /// <summary>A tail that sways.</summary>
        Tail = 5,

        /// <summary>One band of a roller's shell, laid out in a ring when it curls up. Index orders them head to tail.</summary>
        Band = 6,

        /// <summary>A floating orb that bobs and turns above the creature.</summary>
        Orb = 7,

        /// <summary>An armour plate that falls off as the armour is chipped away. Index orders which goes first.</summary>
        Plate = 8,

        /// <summary>A broad fin that beats slowly and ripples, like a ray's wing.</summary>
        Fin = 9,

        /// <summary>A weapon arm held high that comes down on a strike.</summary>
        Blade = 10,

        /// <summary>A tail held over the back that snaps forward on a strike, or lashes sideways for a whip.</summary>
        Stinger = 11,

        /// <summary>A single lower jaw hinged under the head that drops open in the wind-up and slams shut on the bite.</summary>
        Jaw = 12,
    }
}
