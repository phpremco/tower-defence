// Knocks the hero down (all its health at once), so the fall animation and sound can be checked.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

context.Hero.Health.TakeDamage(context.Hero.Health.Max * 2f);
return "hero down: " + context.Hero.IsDown;
