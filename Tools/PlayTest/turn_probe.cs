// Sends the hero to the post in Tools/PlayTest/level_arg.txt (0 = first post, 1 = base) and samples, every few
// stepped frames, the figure yaw, the leg swing and the turn-step blend, so the arrival turn can be read as numbers.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var hero = context.Hero;
var view = hero.GetComponent<Game.Hero.HeroView>();
var type = typeof(Game.Hero.HeroView);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var figure = (UnityEngine.Transform)type.GetField("_figure", flags).GetValue(view);
var swingField = type.GetField("_legSwing", flags);
var turningField = type.GetField("_turning", flags);
var rateField = type.GetField("_turnRate", flags);
var sb = new System.Text.StringBuilder();
UnityEditor.EditorApplication.isPaused = true;
// Skip the walk itself: sample from just before the arrival.
int walked = 0;
while (hero.IsWalking && walked < 3000) { UnityEditor.EditorApplication.Step(); walked++; }
sb.Append("arrived after ").Append(walked).Append(" frames | ");
for (int i = 0; i < 100; i++)
{
    UnityEditor.EditorApplication.Step();
    if (i % 3 == 0)
    {
        sb.Append(i).Append(": yaw ").Append(figure.eulerAngles.y.ToString("0")).Append(" swing ").Append(((float)swingField.GetValue(view)).ToString("0")).Append(" turning ").Append(((float)turningField.GetValue(view)).ToString("0.00")).Append(" rate ").Append(((float)rateField.GetValue(view)).ToString("0")).Append(" walking ").Append(hero.IsWalking ? 1 : 0).Append(" | ");
    }
}
UnityEditor.EditorApplication.isPaused = false;
return sb.ToString();
