namespace Game.Towers
{
    /// <summary>
    /// Which upgrade path a tower look belongs to. Level 1 and 2 looks are shared; level 3 has a look per path. Until
    /// the data has upgrade paths, every tower takes its art's default path (A).
    /// </summary>
    public enum TowerArtPath
    {
        Shared = 0,
        A = 1,
        B = 2,
    }
}
