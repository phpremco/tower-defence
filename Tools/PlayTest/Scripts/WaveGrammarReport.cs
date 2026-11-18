/// <summary>
/// Round 79: the wave grammar's plan of every campaign level and difficulty, as the solve stage composes it (nothing is
/// written to the assets): one letter per wave for the front pattern (s stream, C spread clusters, W swarm, T tank escort,
/// X combo swarm, A air raid), the clusters it is cut into, "!" for a rift surprise, "^" for a flank, "*" for a surprise the level wrote itself and "B" for a boss.
/// Writes Tools/PlayTest/wave_grammar.txt. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/WaveGrammarReport.cs --entry WaveGrammarReport.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class WaveGrammarReport
{
    public static string Run()
    {
        Game.Editor.BalanceStages.Loaded loaded = Game.Editor.BalanceStages.Load(true);
        string[] names = { "Easy", "Medium", "Hard", "Psycho" };
        string letters = "sCWTXA";
        var text = new System.Text.StringBuilder();
        text.AppendLine("# wave grammar plan: pattern letter + clusters per wave; ! rift surprise, ^ flank, * authored surprise, B boss");
        foreach (Game.Balance.WaveScriptSpec script in Game.Editor.WaveScripts.All)
        {
            int index = loaded.Model.IndexOf(script.Key);
            if (index < 0)
            {
                continue;
            }

            // The solve itself, so the flyer floors of Easy and Medium are the ones the stored waves were composed with.
            System.Collections.Generic.List<System.Collections.Generic.List<Game.Balance.WaveSpec>> sets = loaded.Model.BuildWaves(index, script, out _);
            for (int d = 0; d < names.Length; d++)
            {
                text.Append(script.Key).Append(' ').Append(names[d].PadRight(7));
                var counts = new int[letters.Length];
                int surprises = 0;
                for (int number = 1; number <= script.Waves; number++)
                {
                    Game.Balance.WaveSpec wave = sets[d][number - 1];
                    counts[(int)wave.Pattern]++;
                    surprises += wave.Surprise != Game.Balance.SurpriseKind.None ? 1 : 0;
                    text.Append(' ').Append(letters[(int)wave.Pattern]).Append(wave.Beats)
                        .Append(wave.Surprise == Game.Balance.SurpriseKind.Rift ? "!" : wave.Surprise == Game.Balance.SurpriseKind.Flank ? "^" : wave.Surprise == Game.Balance.SurpriseKind.Authored ? "*" : string.Empty)
                        .Append(Game.Balance.WaveComposer.BossOn(script, d, number) != null ? "B" : string.Empty);
                }

                text.Append("   | stream ").Append(counts[0]).Append(", clusters ").Append(counts[1]).Append(", swarm ").Append(counts[2]).Append(", tank escort ").Append(counts[3])
                    .Append(", combo ").Append(counts[4]).Append(", raid ").Append(counts[5]).Append(", surprises ").Append(surprises).AppendLine();
            }
        }

        System.IO.File.WriteAllText("Tools/PlayTest/wave_grammar.txt", text.ToString());
        return text.ToString();
    }
}
