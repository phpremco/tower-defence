using System.Text;

/// <summary>
/// Round 66 without the full generator (Remco's rule): writes what the changed generator now would, and nothing else.
///  1. The permanent upgrades (the generator's "Upgrades" step): ten tiers each, priced by the rules' cost curve with the
///     first three as they were, Spencer's two gear upgrades (Hero_RepairDrone, Hero_Armour), his health upgrade shown as
///     "Health", and the tier caps per continent on the upgrade catalogue.
///  2. The hero's gear art (the generator's "Hero gear art" step): the companion drone's model, the armour plates seated
///     on his suit in the idle pose, the top-tier blade's look, hung on his art.
///  3. The story's hints (the generator's "Scan hints" step): Dr. Voss's explanation of the upgrade screen, with its loading tip.
/// The waves are not touched: the progression model's shopper buys the same upgrades as before in every existing cell
/// (R66PlayerStates.cs shows it), so only the calibration's input hash moves (balance_pipeline.sh apply-keep re-stamps it).
/// Safe to run again. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound66.cs --entry ApplyRound66.Run --timeout_ms 300000 --timeout 360
/// </summary>
public static class ApplyRound66
{
    public static string Run()
    {
        var report = new StringBuilder();
        report.Append(Game.Editor.ProjectGenerator.ApplyUpgradeTiers());
        report.Append(Game.Editor.ProjectGenerator.ApplyHeroGearArt());
        report.Append(Game.Editor.ProjectGenerator.ApplyRound66Story());
        return report.ToString();
    }
}
