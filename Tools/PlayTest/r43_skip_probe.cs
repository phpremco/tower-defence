// Round 43: confirm mid-line in the cutscene: finishes the line on show, moves to the next line (or conversation) and at
// once confirms again, so that line jumps to its end with nothing typed; the blip count must not move on the jump.
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null || player.Dialogue.Current == null) return "no cutscene line";
var view = UnityEngine.Object.FindAnyObjectByType<Game.UI.DialogueView>();
var voice = (Game.UI.DialogueVoice)typeof(Game.UI.DialogueView).GetField("_voice", flags).GetValue(view);
var d = player.Dialogue;
if (!d.LineComplete) d.Advance();
d.Advance();
if (d.Current == null) return "the conversation ended; try again on a longer one";
int blipsAtLineStart = voice.BlipCount;
string line = d.Current.Id + "#" + d.LineIndex + " visible " + d.VisibleCharacters + "/" + d.Current.Lines[d.LineIndex].CharacterCount;
d.Advance();
return "skip on " + line + ": blips before the jump " + blipsAtLineStart + ", after " + voice.BlipCount + ", visible now " + d.VisibleCharacters + ", complete " + d.LineComplete;
