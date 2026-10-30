// Opens the base menu (energy and drones) as if the player had confirmed on the base's doorstep.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

context.BaseMenuOpen = true;
context.Changed.Raise();
return "base menu open";
