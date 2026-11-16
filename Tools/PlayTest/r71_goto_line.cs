// Round 71: advances the running dialogue until line 1 (the drone line with the button token) is fully shown.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context) || context.Dialogue == null) { return "no context"; }
var player = context.Dialogue;
for (int guard = 0; guard < 6 && player.IsPlaying && !(player.LineIndex == 1 && player.LineComplete); guard++)
{
    player.Advance();
}

return player.IsPlaying ? "line " + player.LineIndex + (player.LineComplete ? " complete" : " typing") : "not playing";
