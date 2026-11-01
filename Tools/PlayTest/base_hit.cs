// Hurts the base a little, to see the first-hit alert and hear the alarm.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
context.BaseHealth.TakeDamage(25f);
return "base health " + context.BaseHealth.Current;
