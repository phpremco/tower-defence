using Game.Combat;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>A place the hero can hold: the structure it guards, where it stands and which way it faces.</summary>
    public sealed class HeroPost
    {
        public HeroPost(IGuardable guarded, Vector3 stand, Vector3 facing)
        {
            Guarded = guarded;
            Stand = stand;
            Facing = facing;
        }

        public IGuardable Guarded { get; }

        /// <summary>Where the hero stands: in front of a blockade, on the side the enemies come from.</summary>
        public Vector3 Stand { get; }

        /// <summary>Direction the hero looks when nothing is in reach.</summary>
        public Vector3 Facing { get; }
    }
}
