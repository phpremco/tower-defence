// Starts a ripple at the hero and holds the frame a few steps in, so a screenshot catches the ring mid-way.
var ripples = UnityEngine.Object.FindAnyObjectByType<Game.Levels.RippleEffects>();
if (ripples == null)
{
    return "no ripple effects (low profile or shader missing)";
}

if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

UnityEngine.Vector3 at = context.Hero != null ? context.Hero.transform.position : UnityEngine.Vector3.zero;
ripples.Play(at, 2.5f);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 6; i++)
{
    UnityEditor.EditorApplication.Step();
}

return "ripple at " + at.ToString("0.0") + " enabled=" + ripples.enabled;
