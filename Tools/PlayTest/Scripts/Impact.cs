/// <summary>
/// The impact calculator of the balance pipeline (round 25): Snapshot takes the paper baseline on the data before a change,
/// Run compares the current data's paper with it (Game.Editor.BalanceStages.Impact; also Tools/Generate/Balance/8 and 9). Run:
/// unity command run_script --file Tools/PlayTest/Scripts/Impact.cs --entry Impact.Run --timeout_ms 600000 --timeout 660
/// unity command run_script --file Tools/PlayTest/Scripts/Impact.cs --entry Impact.Snapshot --timeout_ms 900000 --timeout 960
/// </summary>
public static class Impact
{
    public static string Run()
    {
        Game.Editor.BalanceStages.ImpactReport report = Game.Editor.BalanceStages.Impact();
        return (report.Stale ? "STALE; " : string.Empty) + report.FlaggedCells.Count + " cells flagged" + (report.Filter.Length > 0 ? " (" + report.Filter + ")" : string.Empty) + "; see " + Game.Editor.BalanceStages.ImpactPath;
    }

    public static string Snapshot() => Game.Editor.BalanceStages.SnapshotBaseline();
}
