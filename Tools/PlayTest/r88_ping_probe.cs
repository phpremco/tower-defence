// Round 88a: the surprise pings as shown: which points the level watches, how many markers the HUD and the radar show.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var watch = Game.Levels.SurpriseWatch.For(context.Level);
var sb = new System.Text.StringBuilder("watched paths");
for (int p = 0; p < context.Level.Paths.Length; p++) { if (watch.WatchesPath(p)) { sb.Append(' ').Append(p); } }
sb.Append("; watched nests");
for (int n = 0; n < context.Level.Nests.Length; n++) { if (watch.WatchesNest(n)) { sb.Append(' ').Append(n + 1).Append(context.Level.Nests[n].Cell); } }
var hud = UnityEngine.Object.FindAnyObjectByType<Game.UI.SurprisePingView>();
var radar = UnityEngine.Object.FindAnyObjectByType<Game.UI.RadarView>();
sb.Append("; hud markers ").Append(hud != null ? hud.Showing.ToString() : "none").Append("; radar pings ").Append(radar != null ? radar.SurprisesShowing.ToString() : "none");
return sb.ToString();
