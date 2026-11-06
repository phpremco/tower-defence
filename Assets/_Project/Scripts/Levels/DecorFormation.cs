using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// One formation of decoration standing on a blocked cell of the field, as it was drawn: its bounds and what a boss
    /// does with it (its mesh's <see cref="DecorContact"/>). The climb layer reads these, so a boss smashes, avoids or
    /// climbs exactly the piece the player sees.
    /// </summary>
    public readonly struct DecorFormation
    {
        public DecorFormation(Vector3 centre, Vector3 extents, DecorContact contact)
        {
            Centre = centre;
            Extents = extents;
            Contact = contact;
        }

        public Vector3 Centre { get; }
        public Vector3 Extents { get; }
        public DecorContact Contact { get; }

        /// <summary>Height of its top above the world's ground plane.</summary>
        public float Top => Centre.y + Extents.y;

        /// <summary>Its reach on the ground: the larger of its half widths.</summary>
        public float PlanRadius => Mathf.Max(Extents.x, Extents.z);
    }
}
