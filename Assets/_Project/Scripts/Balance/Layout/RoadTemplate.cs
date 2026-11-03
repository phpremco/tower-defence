namespace Game.Balance
{
    /// <summary>The leg skeleton a generated road follows; the seed only jitters lengths and positions inside it.</summary>
    public enum RoadTemplate
    {
        /// <summary>Horizontal legs that switch back across the map, entering at the west edge and ending at the base in the east.</summary>
        Serpentine = 0,

        /// <summary>Vertical legs that wind down and up across the west of the map, then a long run east to the base.</summary>
        Hook = 1,

        /// <summary>A serpentine plus a second road from the south-west that joins the last leg early, at a plaza.</summary>
        Fork = 2,

        /// <summary>A serpentine plus a second road that joins the last leg close to the base: a late second front.</summary>
        LateJoin = 3,
    }
}
