// Round 71: where confirm and cancel sit for the pad family in use (the game's actions and the menus' submit/cancel),
// whether the clock is paused, the options are open, and the dialogue line (index, complete). For the Nintendo swap checks.
var bindings = Game.Core.Services.Get<Game.Platform.IInputBindings>();
var pad = Game.Platform.InputDeviceKind.Gamepad;
var ui = UnityEngine.Object.FindAnyObjectByType<Game.UI.UiConfirmLayout>();
var options = UnityEngine.Object.FindAnyObjectByType<Game.UI.OptionsOverlay>();
string level = "no level";
if (Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    var player = context.Dialogue;
    level = "paused " + context.Paused + ", dialogue " + (player != null && player.IsPlaying ? player.Current.name + " line " + player.LineIndex + (player.LineComplete ? " complete" : " typing") : "none");
}

return "layout " + bindings.ActivePadLayout
    + "; game confirm " + bindings.ControlPath(Game.Platform.GameAction.Confirm, pad) + ", cancel " + bindings.ControlPath(Game.Platform.GameAction.Cancel, pad)
    + "; menu submit " + (ui != null ? ui.SubmitPadPath : "none") + ", cancel " + (ui != null ? ui.CancelPadPath : "none")
    + "; timeScale " + UnityEngine.Time.timeScale + "; options " + (options != null) + "; " + level
    + "; selected " + (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name : "none");
