// Round 88a: builds a tower from its asset on a cell whatever the save has unlocked (for play checks of a gun the fresh
// stage save does not own). Tools/PlayTest/r88_tower_arg.txt: "<TowerId> <x> <y>".
string[] a = System.IO.File.ReadAllText("Tools/PlayTest/r88_tower_arg.txt").Trim().Split(' ');
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c)) { return "no context"; }
var wanted = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>("Assets/_Project/Data/Towers/" + a[0] + ".asset");
if (wanted == null) { return "no tower " + a[0]; }
var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var type = typeof(Game.Levels.PlacementController);
var cell = new UnityEngine.Vector2Int(int.Parse(a[1]), int.Parse(a[2]));
c.Wallet.Add(wanted.BuildCost);
c.SelectedPlacedTower = null;
type.GetField("_cursor", flags).SetValue(placement, cell);
typeof(Game.Levels.LevelContext).GetProperty("SelectedTower", flags).SetValue(c, wanted);
typeof(Game.Levels.LevelContext).GetProperty("SelectedPathObject", flags).SetValue(c, null);
type.GetMethod("TryPlace", flags).Invoke(placement, null);
var towers = (System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, Game.Towers.Tower>)type.GetField("_towers", flags).GetValue(placement);
return towers.TryGetValue(cell, out var built) ? "built " + built.Definition.name + " at " + cell + " targets " + built.Definition.Targets : "could not build at " + cell;
