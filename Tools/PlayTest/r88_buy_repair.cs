// Round 88a: launches the repair drone from its asset, whatever the level and the save offer (for stills of its beam).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var drone = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Drones.DroneDefinition>("Assets/_Project/Data/Drones/RepairDrone.asset");
if (drone == null) { return "no repair drone asset"; }
context.Wallet.Add(10000);
context.DroneRequested.Raise(drone);
return "repair drones: " + context.ActiveDrones(drone);
