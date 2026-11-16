// Round 71: one confirm on the dialogue as the keyboard would give it (finish the line, or go to the next one).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context) || context.Dialogue == null) { return "no context"; }
context.Dialogue.Advance();
return "line " + context.Dialogue.LineIndex + (context.Dialogue.LineComplete ? " complete" : " typing");
