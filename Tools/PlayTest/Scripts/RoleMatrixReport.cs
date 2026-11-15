using System.IO;
using Game.Core;
using Game.Editor;

/// <summary>
/// Round 67: writes the role matrix (Tools/PlayTest/r67_role_matrix.txt) from the data as it is: every tower at level 1,
/// level 3 and level 3 with every permanent upgrade against the reference fight of every creature trait, the helpers over
/// the helper horizon, every role break the tests would report, and the bestiary with its traits. No asset is written. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/RoleMatrixReport.cs --entry RoleMatrixReport.Run --timeout_ms 120000 --timeout 180
/// </summary>
public static class RoleMatrixReport
{
    public static string Run()
    {
        CombatRules rules = RoleMatrix.Rules();
        string report = RoleChecks.Report(rules) + "\n### Creatures and their traits\n\n" + RoleChecks.Bestiary(rules);
        File.WriteAllText(RoleChecks.ReportPath, report);
        return "written to " + RoleChecks.ReportPath;
    }
}
