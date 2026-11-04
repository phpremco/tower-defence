namespace Game.Core
{
    /// <summary>
    /// The flat, glowing pieces painted in Blender (ArtSource README round 25b): ground decals, the placement and range
    /// markers, hero posts, route light trails, health bars, projectiles and beams. Each is a mesh plus a tinted
    /// channel-pack material in <see cref="MarkerArt"/>.
    /// </summary>
    public enum MarkerKind
    {
        GlowPool,
        GlowBlade,
        EnergyField,
        BlobShadow,
        PlacementCursor,
        FreeCell,
        RangeBand,
        RangeFill,
        HeroSpot,
        HeroRing,
        HeroBeacon,
        HeroEmblem,
        SlowField,
        RoadCue,
        FlightCue,
        RoutePulse,
        BarPlate,
        BarFill,
        BarArmour,
        Bolt,
        BoltGlow,
        RailSlug,
        RailSlugGlow,
        EnemyBolt,
        EnemyBoltGlow,
        PulseRing,
        ToolBeam,
        ScoutTrail,
    }
}
