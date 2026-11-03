// Finishes the typewriter of the line on screen (one confirm), so a screenshot shows the whole line.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context) || context.Dialogue == null)
{
    return "no context";
}

if (!context.Dialogue.LineComplete)
{
    context.Dialogue.Advance();
}

return "playing " + context.Dialogue.IsPlaying + " visible " + context.Dialogue.VisibleCharacters + " held " + context.Held;
