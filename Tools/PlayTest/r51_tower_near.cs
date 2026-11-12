// Round 51: places one probe tower of the definition named in Tools/PlayTest/tower_arg.txt (as build_tower.cs) a few cells
// off the road beside the hero's gate, where the spawned creatures end up, so its shots and their trails can be caught.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
string name = System.IO.File.ReadAllText("Tools/PlayTest/tower_arg.txt").Trim();
var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>("Assets/_Project/Data/Towers/" + name + ".asset");
if (definition == null) { return "no tower named " + name; }
var gate = context.Hero.GuardedPosition;
UnityEngine.Vector2Int cell = context.Level.Grid.WorldToCell(gate + new UnityEngine.Vector3(-5f, 0f, 3f));
var go = new UnityEngine.GameObject("ProbeTower_" + definition.DisplayName);
go.transform.position = context.Level.Grid.CellToWorld(cell);
var tower = go.AddComponent<Game.Towers.Tower>();
go.AddComponent<Game.Towers.TowerView>();
tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
tower.Register(context.Structures);
return "placed " + definition.DisplayName + " at " + cell + " " + go.transform.position.ToString("0.0");
