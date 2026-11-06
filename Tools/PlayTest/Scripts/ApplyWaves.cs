/// <summary>
/// Stage "solve" of the balance pipeline: only the generator's "Difficulties" and "Waves" steps (profiles; level payouts and
/// the solved, human-calibrated waves of every campaign level and difficulty) against the calibrated model, for a balance
/// change while the full generator must not run (it syncs the Blender exports from ArtSource). Same code as those steps of
/// Tools/Generate/Scenes and Starter Data and of Tools/Generate/Balance/4. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyWaves.cs --entry ApplyWaves.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyWaves
{
    public static string Run() => Game.Editor.BalanceStages.SolveWaves();
}
