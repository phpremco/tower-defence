// QA round 33: the dialogue on show (id, line, Codex link) and whether the Codex is open.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c)) return "no context";
var p = c.Dialogue;
var codex = UnityEngine.Object.FindAnyObjectByType<Game.UI.CodexView>();
string cur = p != null && p.Current != null ? p.Current.Id + "#" + p.LineIndex + (p.CanOpenCodex ? " codex" : "") : "none";
return "dlg=" + cur + " busy=" + c.StoryBusy + " held=" + c.Held + " paused=" + c.Paused + " wave=" + c.WaveNumber + " codexOpen=" + (codex != null && codex.IsOpen) + " ts=" + UnityEngine.Time.timeScale;
