// Round 49 smoke material fix: places a probe tower (Tools/PlayTest/tower_arg.txt, as build_tower.cs) beside the main road,
// knocks it down to 20% health so its damage smoke and sparks run at nearly full rate, and frames the play camera on it at
// the half height in zoom_size.txt (time keeps running: step frames afterwards so the smoke builds up, then render a still).
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
tower.TakeDamage(tower.Health.Max * 0.8f);
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
if (pan != null) { pan.enabled = false; }
var camera = UnityEngine.Camera.main;
var centre = go.transform.position + UnityEngine.Vector3.up * 0.8f;
var ray = camera.transform.forward;
rig.SetView(centre - ray * (centre.y / ray.y), zoom);
camera.orthographicSize = zoom;
return "placed " + definition.DisplayName + " at " + cell + " health " + tower.Health.Fraction.ToString("0.00");
