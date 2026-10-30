// Cuts the revive timer short, so the stand-up animation can be checked without waiting fifteen seconds.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var field = typeof(Game.Hero.Commander).GetField("_reviveRemaining", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
field.SetValue(context.Hero, 0.01f);
return "revive in " + field.GetValue(context.Hero);
