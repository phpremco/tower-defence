namespace Game.Balance
{
    /// <summary>A hero perk as the balance model sees it: how it changes the blaster's pace (one changes nothing).</summary>
    public sealed class HeroPerkStats
    {
        public string Id = string.Empty;
        public float IntervalMultiplier = 1f;
        public float ReloadMultiplier = 1f;
    }
}
