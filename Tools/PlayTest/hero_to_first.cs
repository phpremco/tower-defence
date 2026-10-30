// Sends the hero back to its first post, the gate nearest the road entrance.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

context.Hero.MoveTo(0);
return "hero sent to post 0";
