namespace Game.Combat
{
    /// <summary>A target with flat per-hit plating (see <see cref="Health.ThroughPlating"/>), so a light gun can look past it.</summary>
    public interface IPlated
    {
        /// <summary>Share of a hit of this size that gets through the plates (1 without plating).</summary>
        float ShareThrough(float hit);
    }
}
