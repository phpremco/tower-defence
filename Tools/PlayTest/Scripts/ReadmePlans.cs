/// <summary>
/// Tools/PlayTest/Plans/README.md from the replayed plans that won in stage B (round 22d).
/// unity command run_script --file Tools/PlayTest/Scripts/ReadmePlans.cs --entry ReadmePlans.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ReadmePlans
{
    public static string Run() => Game.Editor.BalanceStages.WriteWinningPlans();
}
