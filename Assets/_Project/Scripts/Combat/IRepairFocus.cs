namespace Game.Combat
{
    /// <summary>
    /// A structure the player can mark for the repair crew (round 88g: the "Repair focus" toggle on a tower's menu). While
    /// it is marked and has work for the crew, it goes before everything that is not marked.
    /// </summary>
    public interface IRepairFocus
    {
        /// <summary>True while the player wants the repair crew on this structure first.</summary>
        bool RepairFocus { get; }
    }
}
