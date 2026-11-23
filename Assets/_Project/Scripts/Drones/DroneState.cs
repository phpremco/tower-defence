namespace Game.Drones
{
    /// <summary>What a drone is doing, in the four states its card on the HUD tells apart (round 88g).</summary>
    public enum DroneState
    {
        /// <summary>Waiting at home with nothing to do.</summary>
        Idle,

        /// <summary>On its way to a job, to its prey or home with a load.</summary>
        Flying,

        /// <summary>At its job: repairing, mining or shooting.</summary>
        Working,

        /// <summary>Knocked out: at home, or limping there, until its battery is full again.</summary>
        Charging,
    }
}
