using System;

namespace Game.Core
{
    /// <summary>Which kinds of enemies a tower or effect can hit.</summary>
    [Flags]
    public enum TargetLayers
    {
        None = 0,
        Ground = 1 << 0,
        Air = 1 << 1,
    }
}
