/// <summary>
/// Round 82: what the ledger reads of single waves, as the solve stage composes them (nothing is written to the assets).
/// Per wave named in Tools/PlayTest/wave_load_arg.txt ("C1_03:2:7" = level key, difficulty index, wave; one per line): the
/// groups (creature health, count, path, source, start), the roads of the front, and the load (stream health, reach,
/// span, gate seconds, window). First the level's lanes with their trunk share. Writes Tools/PlayTest/wave_load.txt. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/WaveLoadProbe.cs --entry WaveLoadProbe.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class WaveLoadProbe
{
    public static string Run()
    {
        Game.Editor.BalanceStages.Loaded loaded = Game.Editor.BalanceStages.Load(true);
        var text = new System.Text.StringBuilder();
        var solved = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Collections.Generic.List<Game.Balance.WaveSpec>>>();
        foreach (string line in System.IO.File.ReadAllLines("Tools/PlayTest/wave_load_arg.txt"))
        {
            string[] parts = line.Trim().Split(':');
            if (parts.Length != 3)
            {
                continue;
            }

            string key = parts[0];
            int d = int.Parse(parts[1]);
            int number = int.Parse(parts[2]);
            int index = loaded.Model.IndexOf(key);
            Game.Balance.WaveScriptSpec script = Game.Editor.WaveScripts.ByKey(key);
            if (!solved.TryGetValue(key, out var sets))
            {
                sets = loaded.Model.BuildWaves(index, script, out _);
                solved[key] = sets;
                Game.Balance.LevelInput shape = loaded.Model.InputFor(index, d);
                text.Append(key).Append(" second road ").Append(loaded.Model.Facts[index].SecondRoad).Append("; lanes:");
                foreach (Game.Balance.LevelShape.Lane lane in shape.Shape.Lanes)
                {
                    text.Append(" path ").Append(lane.PathIndex).Append(" length ").Append(lane.Length.ToString("0.0")).Append(" trunk ").Append(lane.TrunkShare.ToString("0.00"));
                }

                text.Append("; gates:");
                foreach (Game.Balance.LevelShape.Gate gate in shape.Shape.Gates)
                {
                    text.Append(" path ").Append(gate.PathIndex).Append(" at ").Append(gate.Progress.ToString("0.00")).Append(gate.OnTrunk ? " trunk" : " own");
                }

                text.AppendLine();
            }

            Game.Balance.LevelInput level = loaded.Model.InputFor(index, d);
            Game.Balance.WaveSpec wave = sets[d][number - 1];
            Game.Balance.WaveLoad load = Game.Balance.WaveLoad.Of(level, wave, loaded.Model.Difficulties[d], loaded.Model.Rules);
            text.Append(key).Append(" d").Append(d).Append(" w").Append(number).Append(" x").Append(wave.HealthMultiplier.ToString("0.000")).Append(" roads ").Append(wave.Roads)
                .Append(" surprise ").Append(wave.Surprise).Append(" | stream ").Append(load.StreamHealth.ToString("0")).Append(" reach ").Append(load.StreamReach.ToString("0.00"))
                .Append(" span ").Append(load.Span.ToString("0.0")).Append(" gate s ").Append(load.GateSeconds.ToString("0.0")).Append(" window ").Append(load.Window.ToString("0.0"))
                .Append(" density ").Append(load.GroundDensity.ToString("0.00")).Append(" money ").Append(load.KillMoney).AppendLine();
            text.Append("    ledger: ").Append(Game.Balance.Ledger.Evaluate(level, sets[d], loaded.Model.Difficulties[d], loaded.Model.Rules)[number - 1]).AppendLine();
            foreach (Game.Balance.GroupSpec group in wave.Groups)
            {
                text.Append("    ").Append(group.Count).Append(" x hp ").Append(group.Enemy.EffectiveHealth.ToString("0")).Append(group.Enemy.Armor > 0f ? " plated" : string.Empty)
                    .Append(group.Enemy.IsBoss ? " boss" : string.Empty).Append(" path ").Append(group.Path).Append(" source ").Append(group.Source)
                    .Append(" start ").Append((group.Delay + group.Stagger).ToString("0.0")).Append(" every ").Append(group.Interval.ToString("0.00")).AppendLine();
            }
        }

        System.IO.File.WriteAllText("Tools/PlayTest/wave_load.txt", text.ToString());
        return "written Tools/PlayTest/wave_load.txt";
    }
}
