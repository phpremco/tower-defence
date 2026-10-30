// Raises the boss-wave alarm by hand, so the marker and the approach sounds can be watched without playing the level through.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

context.BossWave = true;
context.BossWaveStarted.Raise();
return "alarm raised";
