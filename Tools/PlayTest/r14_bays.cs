// Round 14: the base's bays (drone, home since, lid open share, depth) and the shuttle's clock. With r14_arg.txt
// "shuttle <seconds>" it first sets the shuttle's clock (rest 16, flight 5, away 12 by default).
var view = UnityEngine.Object.FindAnyObjectByType<Game.Levels.BaseDockView>();
var shuttle = UnityEngine.Object.FindAnyObjectByType<Game.Levels.BaseShuttleView>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
string arg = System.IO.File.Exists("Tools/PlayTest/r14_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r14_arg.txt").Trim() : "";
if (shuttle != null && arg.StartsWith("shuttle "))
{
    typeof(Game.Levels.BaseShuttleView).GetField("_clock", flags).SetValue(shuttle, float.Parse(arg.Substring(8), System.Globalization.CultureInfo.InvariantCulture));
    typeof(Game.Levels.BaseShuttleView).GetMethod("Update", flags).Invoke(shuttle, null);
}

var sb = new System.Text.StringBuilder();
if (view != null)
{
    var drones = (Game.Drones.Drone[])typeof(Game.Levels.BaseDockView).GetField("_drones", flags).GetValue(view);
    var open = (float[])typeof(Game.Levels.BaseDockView).GetField("_open", flags).GetValue(view);
    var depth = (float[])typeof(Game.Levels.BaseDockView).GetField("_depth", flags).GetValue(view);
    for (int i = 0; i < drones.Length; i++)
    {
        sb.Append("bay").Append(i + 1).Append(' ').Append(drones[i] != null ? drones[i].Definition.name + "/" + drones[i].Activity + (drones[i].IsHome ? "/home" : "") : "-")
          .Append(" open ").Append(open[i].ToString("0.00")).Append(" depth ").Append(depth[i].ToString("0.00")).Append(" | ");
    }
}

if (shuttle != null)
{
    sb.Append("shuttle clock ").Append(((float)typeof(Game.Levels.BaseShuttleView).GetField("_clock", flags).GetValue(shuttle)).ToString("0.0"));
}

sb.Append(" t=").Append(UnityEngine.Time.time.ToString("0.0"));
return sb.ToString();
