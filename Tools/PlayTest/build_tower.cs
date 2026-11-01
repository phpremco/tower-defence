// Places one probe tower of the definition named in Tools/PlayTest/tower_arg.txt beside the main road near the
// spawn, outside the real placement path, so one weapon can be watched on its own. Editor only.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
string name = System.IO.File.ReadAllText("Tools/PlayTest/tower_arg.txt").Trim();
var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>("Assets/_Project/Data/Towers/" + name + ".asset");
if (definition == null) { return "no tower named " + name; }
UnityEngine.Vector2Int[] road = context.Level.MainRoad;
UnityEngine.Vector2Int cell = road[3] + UnityEngine.Vector2Int.up * 2;
var go = new UnityEngine.GameObject("ProbeTower_" + definition.DisplayName);
go.transform.position = context.Level.Grid.CellToWorld(cell);
var tower = go.AddComponent<Game.Towers.Tower>();
go.AddComponent<Game.Towers.TowerView>();
tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
tower.Register(context.Structures);
return "placed " + definition.DisplayName + " at " + cell;
