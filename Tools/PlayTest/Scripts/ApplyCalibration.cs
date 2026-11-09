/// <summary>
/// Stage "apply" of the balance pipeline (round 22): writes the fit of Tools/PlayTest/calibration.txt into the
/// BalanceCalibration asset (Game.Editor.BalanceStages.ApplyCalibration; also Tools/Generate/Balance/3). A fit below the
/// calibration bar is refused (round 25); Force applies it anyway when the curator accepts a lower bar. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyCalibration.cs --entry ApplyCalibration.Run --timeout_ms 120000 --timeout 180
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyCalibration.cs --entry ApplyCalibration.Force --timeout_ms 120000 --timeout 180
/// </summary>
public static class ApplyCalibration
{
    public static string Run() => Game.Editor.BalanceStages.ApplyCalibration();

    public static string Force() => Game.Editor.BalanceStages.ApplyCalibration(true);
}
