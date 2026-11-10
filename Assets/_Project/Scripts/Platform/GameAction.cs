namespace Game.Platform
{
    /// <summary>
    /// The player actions a prompt can name. The input provider maps each to its bindings, so a prompt shows the key or
    /// pad button of the device in use instead of a fixed letter. Values are only ever appended.
    /// </summary>
    public enum GameAction
    {
        Confirm = 0,
        Cancel = 1,
        Pause = 2,
        Speed = 3,
        NextTower = 4,
        PreviousTower = 5,
        SkipWave = 6,
        Upgrade = 7,
        Repair = 8,
        Sell = 9,
        HeroPost = 10,
        HeroAbility1 = 11,
        HeroAbility2 = 12,
        Overview = 13,
        Codex = 14,
    }
}
