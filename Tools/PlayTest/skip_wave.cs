// Starts the next wave at once.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

context.SkipWaveRequested.Raise();
return "wave " + context.WaveNumber;
