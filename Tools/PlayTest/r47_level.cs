// Round 47: in a running level, places probe towers along the main road (so creatures get hit and die), starts the
// wave in r47_wave.txt (1-based) and sets the game speed from r47_speed.txt (1, 2 or 3) through the TimeController.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
int wave = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r47_wave.txt").Trim());
int speed = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r47_speed.txt").Trim());
string[] names = { "PulseRifle", "Cannon", "Rocket", "Laser", "PulseRifle", "Tesla" };
UnityEngine.Vector2Int[] road = context.Level.MainRoad;
int placed = 0;
if (UnityEngine.GameObject.Find("ProbeTower_0") == null)
{
    for (int i = 0; i < names.Length; i++)
    {
        var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>("Assets/_Project/Data/Towers/" + names[i] + ".asset");
        UnityEngine.Vector2Int cell = road[System.Math.Min(road.Length - 1, 6 + i * road.Length / (names.Length + 2))] + UnityEngine.Vector2Int.up * 3;
        var go = new UnityEngine.GameObject("ProbeTower_" + i);
        go.transform.position = context.Level.Grid.CellToWorld(cell);
        var tower = go.AddComponent<Game.Towers.Tower>();
        go.AddComponent<Game.Towers.TowerView>();
        tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
        tower.Register(context.Structures);
        placed++;
    }
}

var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
typeof(Game.Levels.LevelController).GetField("_nextWaveIndex", flags).SetValue(controller, wave - 1);
typeof(Game.Levels.LevelController).GetMethod("StartNextWave", flags).Invoke(controller, null);
var time = UnityEngine.Object.FindAnyObjectByType<Game.Levels.TimeController>();
for (int i = 0; i < 3 && time.CurrentSpeed != speed; i++) { time.CycleSpeed(); }
return "placed " + placed + ", wave " + wave + " started, speed " + time.CurrentSpeed + ", frame " + UnityEngine.Time.frameCount;
