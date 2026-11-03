// Opens the current cutscene shot's dialogue at once (after a seek, which clears it), for a still of the box.
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var dialogue = player.Sequencer.Current.Dialogue;
if (dialogue == null) return "shot has no dialogue";
player.Dialogue.Enqueue(dialogue);
player.Dialogue.TryStart();
return "showing " + dialogue.name;
