// Round 16: skips whatever conversation is on screen (a boss intro), so the field is not dimmed in a still.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context) || context.Dialogue == null) { return "no context"; }
bool was = context.Dialogue.IsPlaying;
if (was) { context.Dialogue.SkipConversation(); }
return "dialogue was " + was + ", now " + context.Dialogue.IsPlaying;
