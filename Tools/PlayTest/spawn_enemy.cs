// Spawns one enemy by definition asset name, read from Tools/PlayTest/enemy_arg.txt as "Name [Gate|Water|Lava|Quicksand]",
// through the level's spawner (so bosses use the level's entrance), and reports where it came up.
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ');
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + args[0] + ".asset");
if (definition == null)
{
    return "no enemy named " + args[0];
}

var source = args.Length > 1 ? (Game.Levels.SpawnSource)System.Enum.Parse(typeof(Game.Levels.SpawnSource), args[1]) : Game.Levels.SpawnSource.Gate;
var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
var field = typeof(Game.Levels.LevelController).GetField("_spawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var spawner = (Game.Levels.EnemySpawner)field.GetValue(controller);
spawner.Spawn(definition, 0, source);
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition == definition)
    {
        return "spawned " + definition.name + " origin " + enemy.Origin + " at " + enemy.Position.ToString("0.0");
    }
}

return "spawned " + definition.name;
