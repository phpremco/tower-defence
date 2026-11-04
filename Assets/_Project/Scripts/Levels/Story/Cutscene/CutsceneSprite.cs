namespace Game.Levels
{
    /// <summary>
    /// The glow, trail and crack sprites of the cutscenes, painted in Blender (README round 26, Cutscene FX). Greyscale
    /// with RGB = A, drawn additively and tinted by the set's own HDR colours. Values are only appended.
    /// </summary>
    public enum CutsceneSprite
    {
        GlowSoft = 0,
        BeamSoft = 1,
        SunCore = 2,
        SunHalo = 3,
        SunStreak = 4,
        Glint = 5,
        EngineGlow = 6,
        ExhaustPlume = 7,
        RunningLight = 8,
        PlasmaTrail = 9,
        DrillCracks = 10,
        CrackDrillA = 11,
        CrackDrillB = 12,
        CrackSwarmA = 13,
        CrackSwarmB = 14,
        ScreamRing = 15,
        GateField = 16,
    }
}
