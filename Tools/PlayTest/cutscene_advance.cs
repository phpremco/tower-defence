// Advances the open cutscene dialogue (as a confirm press would).
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
player.Dialogue.Advance();
return "advanced; " + (player.Dialogue.Current != null ? player.Dialogue.Current.Id + "#" + player.Dialogue.LineIndex : "none");
