namespace Game.Levels
{
    /// <summary>
    /// The place a camera key is written relative to: x to the right of the anchor, y up, z along its facing. Sets
    /// resolve anchors from the level data, so the same shot frames the same moment on any field level.
    /// </summary>
    public enum CameraAnchor
    {
        /// <summary>World space.</summary>
        World = 0,

        /// <summary>The pathfinder capsule and the beacon beside it, facing the base.</summary>
        Landing = 1,

        /// <summary>The base, facing out along its road.</summary>
        Base = 2,

        /// <summary>The drill on its deposit, facing the base.</summary>
        Drill = 3,

        /// <summary>The new dig site where the commander stands when the ground starts to move, facing the entrance.</summary>
        DigSite = 4,

        /// <summary>The commander post in front of the first gate, facing the way the creatures come.</summary>
        Gate = 5,

        /// <summary>Where the road enters the field and the creatures break out, facing along the road.</summary>
        Entrance = 6,

        /// <summary>The base's energy core (the procedural cage beside the base, or the imported base's crystal), facing as the base.</summary>
        Core = 7,

        /// <summary>The commander as he reacts to the swarm: his spot and the way he faces, turned towards the camera's side.</summary>
        Commander = 8,
    }
}
