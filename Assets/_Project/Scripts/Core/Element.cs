namespace Game.Core
{
    /// <summary>
    /// The natural force a creature is born from. Element creatures rise from nests in the matching terrain and
    /// return to it when they die: fire goes out, water steams off, wood burns away, sand blows off, ice shatters.
    /// </summary>
    public enum Element
    {
        None = 0,
        Fire = 1,
        Water = 2,
        Wood = 3,
        Sand = 4,
        Ice = 5,

        /// <summary>Marsh gas and rot: the element of the bog.</summary>
        Bog = 6,
    }
}
