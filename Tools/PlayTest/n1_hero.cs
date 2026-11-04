// Night 1, item 1: opens the hero panel. Arg n1_hero_arg.txt: "full" (at the hero's post) or "prompt" (at another
// post: the blockade's "move hero here" pop-up), "close". The camera centres on that post.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
string arg = System.IO.File.ReadAllText("Tools/PlayTest/n1_hero_arg.txt").Trim();
var hero = context.Hero;
if (arg == "close") { context.HeroMenuPost = -1; context.Changed.Raise(); return "closed"; }
int post = arg == "full" ? hero.PostIndex : (hero.PostIndex + 1) % hero.PostCount;
context.HeroMenuPost = post;
context.Changed.Raise();
var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
if (pan != null) pan.SetView(hero.StandAt(post), pan.PlayZoom);
return "hero panel " + arg + " post " + post + " (" + context.HeroPostNames[post] + ") hero at " + hero.PostIndex;
