using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// One formation of decoration standing on a blocked cell of the field, as it was drawn: its bounds and whether it
    /// breaks under weight. The climb layer reads these, so a boss climbs exactly the rock the player sees.
    /// </summary>
    public readonly struct DecorFormation
    {
        public DecorFormation(Vector3 centre, Vector3 extents, bool fragile)
        {
            Centre = centre;
            Extents = extents;
            Fragile = fragile;
        }

        public Vector3 Centre { get; }
        public Vector3 Extents { get; }
        public bool Fragile { get; }

        /// <summary>Height of its top above the world's ground plane.</summary>
        public float Top => Centre.y + Extents.y;
    }
}
