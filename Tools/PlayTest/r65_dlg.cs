// Round 65: the conversation on show. Tools/PlayTest/r65_dlg_arg.txt: "state", "next" (completes the line, then moves on
// as a confirm does) or "skip".
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c)) return "no context";
string mode = System.IO.File.Exists("Tools/PlayTest/r65_dlg_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r65_dlg_arg.txt").Trim() : "state";
var p = c.Dialogue;
if (mode == "next" && p.IsPlaying) { if (!p.LineComplete) p.Advance(); p.Advance(); }
if (mode == "skip" && p.IsPlaying) p.SkipConversation();
var seen = Game.Core.Services.Get<Game.Economy.ProgressService>();
return mode + " dlg=" + (p.Current != null ? p.Current.Id + "#" + p.LineIndex : "none") + " queued=" + p.QueuedCount + " story=" + c.StoryEnabled + " seenHighGround=" + seen.HasSeenStory("Dlg_Tip_HighGround");
