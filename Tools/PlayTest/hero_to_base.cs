// Sends the hero to its last post, the base.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

context.Hero.MoveTo(context.Hero.PostCount - 1);
return "hero sent to post " + (context.Hero.PostCount - 1);
