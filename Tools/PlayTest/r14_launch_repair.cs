// Round 14: launches only the level's repair drone (filling the purse first).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
context.Wallet.Add(10000);
context.DroneRequested.Raise(context.Level.RepairDrone);
return "launched repair " + context.ActiveDrones(context.Level.RepairDrone);
