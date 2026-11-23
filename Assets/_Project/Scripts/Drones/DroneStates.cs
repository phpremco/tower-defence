namespace Game.Drones
{
    /// <summary>Folds what a drone is busy with into the four states the HUD shows. Pure logic.</summary>
    public static class DroneStates
    {
        public static DroneState Of(DroneActivity activity)
        {
            switch (activity)
            {
                case DroneActivity.Repairing:
                case DroneActivity.Mining:
                case DroneActivity.Fighting:
                    return DroneState.Working;
                case DroneActivity.Flying:
                case DroneActivity.Hauling:
                    return DroneState.Flying;
                case DroneActivity.Recharging:
                    return DroneState.Charging;
                default:
                    return DroneState.Idle;
            }
        }
    }
}
