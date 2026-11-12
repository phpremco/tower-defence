// Round 50: hooks the hero's Drawing and Struck events and writes the running counts (with the game time of each) to
// Tools/PlayTest/r50_hero_counts.txt, so r50_hero_state.cs can show when the draw began and when the first shot fell.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var hero = context.Hero;
var log = new System.Text.StringBuilder();
System.IO.File.WriteAllText("Tools/PlayTest/r50_hero_counts.txt", "");
hero.Drawing += weapon => { log.Append("draw " + weapon.name + "@" + UnityEngine.Time.time.ToString("0.00") + " "); System.IO.File.WriteAllText("Tools/PlayTest/r50_hero_counts.txt", log.ToString()); };
hero.Struck += (position, weapon) => { if (log.Length < 600) { log.Append("shot@" + UnityEngine.Time.time.ToString("0.00") + " "); System.IO.File.WriteAllText("Tools/PlayTest/r50_hero_counts.txt", log.ToString()); } };
return "hooked";
