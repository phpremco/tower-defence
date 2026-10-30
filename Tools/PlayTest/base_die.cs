// Destroys the base outright, to watch its explosion and the defeat screen that follows.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

context.BaseHealth.TakeDamage(context.BaseHealth.Max + 1f);
return "base health " + context.BaseHealth.Current + " outcome " + context.Outcome;
