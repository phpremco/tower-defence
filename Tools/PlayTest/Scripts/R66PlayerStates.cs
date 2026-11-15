using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Balance;

/// <summary>
/// Round 66: the progression model's player per cell (research earned and spent, the upgrade levels bought, the mean tower
/// bonus), so a change to the upgrade catalogue can be shown to leave the model's shopping as it was. Writes
/// Tools/PlayTest/r66_player_states_[label].txt; the label is the text of Tools/PlayTest/r66_states_arg.txt ("now" without it).
/// unity command run_script --file Tools/PlayTest/Scripts/R66PlayerStates.cs --entry R66PlayerStates.Run --timeout_ms 120000 --timeout 180
/// </summary>
public static class R66PlayerStates
{
    public static string Run()
    {
        const string ArgPath = "Tools/PlayTest/r66_states_arg.txt";
        string label = File.Exists(ArgPath) ? File.ReadAllText(ArgPath).Trim() : "now";
        Game.Editor.BalanceStages.Loaded loaded = Game.Editor.BalanceStages.Load(true);
        CampaignModel model = loaded.Model;
        var text = new StringBuilder();
        for (int i = 0; i < model.Levels.Count; i++)
        {
            for (int d = 0; d < model.Difficulties.Count; d++)
            {
                PlayerState player = model.PlayerAt(i, d);
                IEnumerable<string> levels = player.UpgradeLevels.OrderBy(p => p.Key, System.StringComparer.Ordinal).Select(p => p.Key + "=" + p.Value);
                text.Append(model.Levels[i].Key).Append(' ').Append(model.Difficulties[d].Id)
                    .Append(" earned ").Append(player.ResearchEarned).Append(" spent ").Append(player.ResearchSpent)
                    .Append(" bonus ").Append(player.MeanTowerBonus.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(" | ").Append(string.Join(",", levels)).Append('\n');
            }
        }

        string path = "Tools/PlayTest/r66_player_states_" + label + ".txt";
        File.WriteAllText(path, text.ToString());
        return path + "\n" + text;
    }
}
