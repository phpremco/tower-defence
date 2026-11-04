// Night 1, item 3: arms the cutscene skip. Arg n1_skip_arg.txt: "press" (one real press: the window runs out after two
// seconds) or "hold" (the window pinned open with its bar full, for a still).
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene";
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
string arg = System.IO.File.Exists("Tools/PlayTest/n1_skip_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/n1_skip_arg.txt").Trim() : "press";
if (arg == "hold")
{
    var skip = typeof(Game.UI.CutscenePlayer).GetField("_skip", flags).GetValue(player);
    typeof(Game.Levels.SkipConfirm).GetField("_armedAt", flags).SetValue(skip, UnityEngine.Time.unscaledTime + 2.5f);
    return "skip pinned, share " + ((Game.Levels.SkipConfirm)skip).ArmedShare(UnityEngine.Time.unscaledTime);
}
typeof(Game.UI.CutscenePlayer).GetMethod("OnSkipPressed", flags).Invoke(player, null);
return "skip pressed";
