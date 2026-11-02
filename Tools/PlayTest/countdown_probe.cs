// Reports the HUD wave line and the countdown line, with the context countdown, to check the timer ticks.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
string wave = "", count = "";
foreach (var text in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsSortMode.None))
{
    if (text.name == "Wave") { wave = text.text; }
    if (text.name == "Countdown") { count = text.text + " colour " + text.color.ToString("0.00"); }
}
return "wave='" + wave + "' countdown='" + count + "' context=" + context.NextWaveCountdown.ToString("0.0");
