namespace Game.Combat
{
    /// <summary>Something that stands in front of a structure and takes part of the hits aimed at it, such as the hero.</summary>
    public interface IDamageGuard
    {
        /// <summary>Takes its share of the damage and returns what still reaches the guarded structure.</summary>
        float Absorb(float damage);
    }
}
