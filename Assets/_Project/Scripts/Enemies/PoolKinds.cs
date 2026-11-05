using System;

namespace Game.Enemies
{
    /// <summary>Which liquids a boss may dive into. Mirrors the liquid terrain kinds of a level, as flags.</summary>
    [Flags]
    public enum PoolKinds
    {
        None = 0,
        Water = 1,
        Lava = 2,
        Quicksand = 4,
        Ice = 8,
        Bog = 16,
    }
}
