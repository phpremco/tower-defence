// Round 66: Dr. Voss's explanation on the upgrade screen. Tools/PlayTest/r66_tip_arg.txt: "state", "next" (Confirm), "skip" (Cancel).
string act = System.IO.File.ReadAllText("Tools/PlayTest/r66_tip_arg.txt").Trim();
var guide = UnityEngine.Object.FindAnyObjectByType<Game.UI.UpgradeScreenGuide>();
var progress = Game.Core.Services.Get<Game.Economy.ProgressService>();
string seen = " seen " + progress.HasSeenStory("Dlg_Tip_UpgradeScreen");
if (guide == null) return "no guide on this screen;" + seen;
var player = (Game.Levels.DialoguePlayer)typeof(Game.UI.UpgradeScreenGuide).GetField("_player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(guide);
if (act == "next") { player.Advance(); player.Advance(); }
if (act == "skip") player.SkipConversation();
var group = UnityEngine.Object.FindAnyObjectByType<Game.UI.UpgradeScreen>().GetComponentInChildren<UnityEngine.CanvasGroup>();
return act + ": playing " + player.IsPlaying + " line " + player.LineIndex + " ownsInput " + guide.OwnsInput + " screen interactable " + (group != null ? group.interactable.ToString() : "?") + seen;
