namespace Game.Levels
{
    /// <summary>Where a region lies: on the surface of the planet, or deep below it in the Core.</summary>
    public enum MapLayer
    {
        Surface = 0,

        /// <summary>Under the surface: the globe shows it only as a crack, never as painted land.</summary>
        Core = 1,
    }
}
