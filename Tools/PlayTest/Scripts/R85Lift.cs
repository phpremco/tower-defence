using System.Collections.Generic;
using System.Text;
using Game.Balance;
using Game.Editor;
using static System.FormattableString;

/// <summary>
/// Round 85, a diagnosis on paper (no asset written): Glass Meadows Psycho with and without its surprises, waves 6 to
/// 13, with the boss multiple of round 85 on and off. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/R85Lift.cs --entry R85Lift.Run --timeout_ms 300000 --timeout 360
/// </summary>
public static class R85Lift
{
    public static string Run()
    {
        var text = new StringBuilder();
        foreach (float over in new[] { 3f, 0f })
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            CampaignModel model = loaded.Model;
            model.Rules.BossOverRegular = new[] { over, over, over, over };
            int index = model.IndexOf("C1_02");
            List<List<WaveSpec>> sets = model.BuildWaves(index, WaveScripts.ByKey("C1_02"), out _);
            model.Rules.Grammar.Surprises = false;
            List<List<WaveSpec>> calm = model.BuildWaves(index, WaveScripts.ByKey("C1_02"), out _);
            model.Rules.Grammar.Surprises = true;
            text.Append(Invariant($"BossOverRegular {over}:\n"));
            for (int i = 5; i < 13 && i < sets[3].Count; i++)
            {
                WaveSpec a = sets[3][i];
                WaveSpec b = calm[3][i];
                text.Append(Invariant($"  w{i + 1} {a.Pattern} surprise {a.Surprise}: x{a.HealthMultiplier:0.000} boss x{a.BossHealthScale:0.000} | calm x{b.HealthMultiplier:0.000} boss x{b.BossHealthScale:0.000} | ratio {a.HealthMultiplier / b.HealthMultiplier:0.000}\n"));
            }
        }

        return text.ToString();
    }
}
