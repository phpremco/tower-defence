using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A continent on the planet. Levels belong to one region each and the campaign fills the regions one after
    /// another; a region without levels is shown on the globe as land still to be explored.
    /// </summary>
    [Serializable]
    public struct MapRegion
    {
        public string DisplayName;

        /// <summary>The middle of the continent, in degrees: longitude -180..180 (east positive), latitude -90..90.</summary>
        public float Longitude;
        public float Latitude;

        /// <summary>How far the land reaches from the middle, in degrees of arc. Levels of the region stay inside it.</summary>
        public float RadiusDegrees;

        /// <summary>The tint of the continent on the globe.</summary>
        public Color Colour;

        /// <summary>Stable id of the continent (C1 to C6), for data and saves; the display name may change.</summary>
        public string Key;

        /// <summary>The surface, or the Core under it. Spacing between levels is only compared within one layer.</summary>
        public MapLayer Layer;

        /// <summary>The point on a unit sphere the middle sits on.</summary>
        public Vector3 Direction => PlanetCoordinates.ToDirection(Longitude, Latitude);
    }
}
