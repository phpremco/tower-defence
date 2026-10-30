// Presses the briefing's Deploy button, so the recon fly-over starts.
var briefing = UnityEngine.Object.FindAnyObjectByType<Game.UI.BriefingView>();
if (briefing == null)
{
    return "no briefing";
}

typeof(Game.UI.BriefingView).GetMethod("Deploy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(briefing, null);
return "deployed";
