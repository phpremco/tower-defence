using System;

namespace Game.Balance
{
    /// <summary>
    /// The main roads the front of a wave walks in on (round 82). A level that forks has two; the wave grammar picks a
    /// subset per wave and difficulty (<see cref="WaveGrammar.RoadsOn"/>) and a level may write its own
    /// (<see cref="RoadSpec"/>). A level with one road always uses <see cref="First"/>.
    /// </summary>
    [Flags]
    public enum MainRoads
    {
        None = 0,
        First = 1,
        Second = 2,
        Both = First | Second,
    }
}
