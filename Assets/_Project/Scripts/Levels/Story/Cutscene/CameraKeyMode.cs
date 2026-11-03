namespace Game.Levels
{
    /// <summary>Where a camera key takes its pose from.</summary>
    public enum CameraKeyMode
    {
        /// <summary>The position and look-at point written in the key.</summary>
        Free = 0,

        /// <summary>The pose the level camera starts in, seen through a long lens, so the cutscene hands over to play.</summary>
        MatchLevelRig = 1,
    }
}
