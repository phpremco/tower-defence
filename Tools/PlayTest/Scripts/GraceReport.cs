using System.IO;
using Game.Editor;

/// <summary>
/// Round 67, the growth layer: writes Tools/PlayTest/grace_windows.txt from the stored waves and the yardstick records
/// (calibration_records.txt). Per cell and counter trait (armour, air): the wave the trait first appears in and how long
/// every row of one tower type that is not made for it held after that, against the rules' grace window. No asset is
/// written. Run after a measure (balance_pipeline.sh grace):
/// unity command run_script --file Tools/PlayTest/Scripts/GraceReport.cs --entry GraceReport.Run --timeout_ms 120000 --timeout 180
/// </summary>
public static class GraceReport
{
    public static string Run()
    {
        string report = GraceWindows.Report(RoleMatrix.Rules());
        File.WriteAllText(GraceWindows.ReportPath, report);
        return report.Substring(report.LastIndexOf('\n', report.Length - 2) + 1);
    }
}
