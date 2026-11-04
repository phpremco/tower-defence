namespace Game.Levels
{
    /// <summary>
    /// The modular pieces an environment set places from the grid, named as in the Blender kit
    /// (<c>&lt;Prefix&gt;RoadEdge_Straight</c> and so on). Placement frames are in <see cref="EnvironmentLayout"/>.
    /// </summary>
    public enum EnvironmentPieceKind
    {
        RoadEdgeStraight = 0,
        RoadEdgeOuterCorner = 1,
        RoadEdgeInnerCorner = 2,
        RoadEdgeEndCap = 3,
        RoadEdgePlazaJoin = 4,
        PlazaEdgeStraight = 5,
        PlazaEdgeCorner = 6,
        BuildPad = 7,
        GatePlinth = 8,
        CliffStraight = 9,
        CliffOuterCorner = 10,
        CliffInnerCorner = 11,
        BankStraight = 12,
        BankOuterCorner = 13,
        BankInnerCorner = 14,
        TerraceStep = 15,
        TerraceStepOuter = 16,
        RiftShort = 17,
        RiftMedium = 18,
        RiftLong = 19,
        RiftCap = 20,
        Burrow = 21,
        CrystalVein = 22,

        /// <summary>The end cap and the plaza join mirrored in X, for a gate or plaza on the other side (made by the generator).</summary>
        RoadEdgeEndCapMirrored = 23,
        RoadEdgePlazaJoinMirrored = 24,
    }
}
