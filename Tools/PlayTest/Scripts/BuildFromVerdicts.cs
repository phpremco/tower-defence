/// <summary>
/// The builder on the stage-B verdicts (round 22d): one lever move per cell from the recorded runs, then the waves solved.
/// unity command run_script --file Tools/PlayTest/Scripts/BuildFromVerdicts.cs --entry BuildFromVerdicts.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class BuildFromVerdicts
{
    public static string Run() => Game.Editor.BalanceStages.BuildFromVerdicts();
}
