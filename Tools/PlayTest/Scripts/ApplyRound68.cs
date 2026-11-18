using Game.Balance;
using Game.Editor;

/// <summary>
/// Round 68 without the full generator (Remco's rule): writes what the changed generator now would. The round's data is
/// the growth layer of the combat rules in the waves: plated troops that grow in number past their grace window
/// (BalanceRules.ArmourGrowth, one factor per difficulty since round 84, where a level sends no flyers; the window is
/// Game.Balance.GrowthWindow, the same table the rules asset carries) and the mark on the waves that ask for the counter
/// tower (WaveDefinition.CounterCheck). Both are written by the generator's "Waves" step alone, which is the pipeline's
/// solve stage; this script runs it and prints the growth. Safe to run again. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound68.cs --entry ApplyRound68.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyRound68
{
    public static string Run()
    {
        string solved = BalanceStages.SolveWaves();
        BalanceRules rules = BalanceRules.Default;
        var text = new System.Text.StringBuilder(solved).Append(" Growth past the grace window (plated troops per wave, Easy to Psycho, where a level sends no flyers):");
        for (int d = 0; d < 4; d++)
        {
            text.Append(' ').Append(rules.ArmourGrowthFor(d).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        }

        return text.ToString();
    }
}
