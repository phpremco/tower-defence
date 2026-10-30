// Spawns every creature listed in Tools/PlayTest/spawn_list.txt, one per line as "Name [Gate|Water|Lava|Quicksand|Nest]",
// through the level's spawner, and reports how many came up. Pair with no_waves.cs to watch them undisturbed.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
var field = typeof(Game.Levels.LevelController).GetField("_spawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var spawner = (Game.Levels.EnemySpawner)field.GetValue(controller);
var report = new System.Text.StringBuilder();
foreach (string line in System.IO.File.ReadAllLines("Tools/PlayTest/spawn_list.txt"))
{
    string[] args = line.Trim().Split(' ');
    if (args[0].Length == 0)
    {
        continue;
    }

    var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + args[0] + ".asset");
    if (definition == null)
    {
        report.Append("missing ").Append(args[0]).Append("; ");
        continue;
    }

    var source = args.Length > 1 ? (Game.Levels.SpawnSource)System.Enum.Parse(typeof(Game.Levels.SpawnSource), args[1]) : Game.Levels.SpawnSource.Gate;
    spawner.Spawn(definition, 0, source);
    report.Append(args[0]).Append(' ');
}

return "spawned " + report + "alive " + spawner.Alive;
