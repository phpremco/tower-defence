// Round 43: the dialogue box sounds: per dialogue presenter, the openers and blips played so far and whether its source sounds;
// in the cutscene also the line on show (dialogue#line, visible/total, complete).
var sb = new System.Text.StringBuilder();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player != null && player.Dialogue.Current != null)
{
    var d = player.Dialogue;
    sb.Append("[").Append(d.Current.Id).Append("#").Append(d.LineIndex).Append(" ").Append(d.VisibleCharacters).Append("/").Append(d.Current.Lines[d.LineIndex].CharacterCount).Append(d.LineComplete ? " complete" : " typing").Append("] ");
}
else
{
    sb.Append("[").Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name).Append(", no cutscene line] ");
}
foreach (var view in UnityEngine.Object.FindObjectsByType<Game.UI.DialogueView>(UnityEngine.FindObjectsSortMode.None))
{
    var voice = (Game.UI.DialogueVoice)typeof(Game.UI.DialogueView).GetField("_voice", flags).GetValue(view);
    sb.Append("DialogueView@").Append(view.gameObject.name).Append(" openers=").Append(voice.OpenerCount).Append(" blips=").Append(voice.BlipCount).Append(" ");
}
foreach (var box in UnityEngine.Object.FindObjectsByType<Game.UI.BossAnswerBox>(UnityEngine.FindObjectsSortMode.None))
{
    var voice = (Game.UI.DialogueVoice)typeof(Game.UI.BossAnswerBox).GetField("_voice", flags).GetValue(box);
    sb.Append("BossAnswerBox openers=").Append(voice.OpenerCount).Append(" blips=").Append(voice.BlipCount).Append(" ");
}
return sb.ToString();
