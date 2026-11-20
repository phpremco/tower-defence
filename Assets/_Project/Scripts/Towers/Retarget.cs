namespace Game.Towers
{
    /// <summary>
    /// What a turret pays for a new target (round 87, Remco: a Gun Turret "can't keep up with many fast small
    /// creatures"). A turret has to swing its barrel: the first shot at a creature other than the one it shot last comes
    /// its retarget time later, on top of its reload. A tower that had nothing to shoot at has had the time to turn, so
    /// its first shot is free. The time is data on the tower (TowerDefinition.RetargetSeconds); a beam's is its sweep
    /// (<see cref="BeamBudget"/>). It is added to the cooldown like a reload, so it does not depend on the frame length.
    /// Pure, so it is tested without a scene and the stream duel (Game.Editor.StreamDuel) plays the same rule.
    /// </summary>
    public static class Retarget
    {
        /// <param name="hadTarget">False for a tower that was idle: nothing to swing away from.</param>
        /// <param name="sameTarget">True when the shot goes to the creature the last one went to.</param>
        public static float Seconds(bool hadTarget, bool sameTarget, float retargetSeconds) =>
            hadTarget && !sameTarget && retargetSeconds > 0f ? retargetSeconds : 0f;
    }
}
