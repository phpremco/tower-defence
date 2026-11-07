/// <summary>
/// Stage "finale" of the balance pipeline (round 22f): writes the continent bosses' health and the difficulties' boss health
/// share from the finale probe's report (Tools/PlayTest/finale_probe.txt, FinaleProbeTests). Both are inputs of the
/// calibration hash, so the pipeline reports the calibration stale afterwards: measure, fit and apply follow. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyFinale.cs --entry ApplyFinale.Run --timeout_ms 120000 --timeout 180
/// </summary>
public static class ApplyFinale
{
    public static string Run() => Game.Editor.BalanceStages.ApplyFinale();
}
