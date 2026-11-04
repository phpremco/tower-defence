// Import stage 4b: the round-15 species in a level. Tools/PlayTest/imp4b_creatures.txt holds one phase:
//   spawn                 a column of Shardbacks and Mireleeches walked a little way down the main road; each Shardback
//                         after the first has lost more of its armour (shed prism clusters)
//   view <ortho half>     the camera on the second Shardback
// Run after imp4_phase.cs "setup" (waves off). The editor stays paused as in imp4_phase.cs.
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/imp4b_creatures.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var report = new System.Text.StringBuilder();
var shardbacks = new System.Collections.Generic.List<Game.Enemies.Enemy>();
if (args[0] == "spawn")
{
    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    var field = typeof(Game.Levels.LevelController).GetField("_spawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var spawner = (Game.Levels.EnemySpawner)field.GetValue(controller);
    var before = new System.Collections.Generic.HashSet<Game.Enemies.Enemy>(UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>());
    string[] kinds = { "Shardback", "Mireleech", "Shardback", "Mireleech", "Shardback", "Shardback" };
    foreach (string kind in kinds)
    {
        var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + kind + ".asset");
        spawner.Spawn(enemy, 0, Game.Levels.SpawnSource.Gate);
    }

    int k = 0;
    foreach (var walker in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
    {
        if (before.Contains(walker)) continue;
        for (int i = 0; i < 60 + k * 14; i++) walker.Step(0.1f);
        if (walker.Definition.name == "Shardback")
        {
            // 0, 1/3, 2/3 and all of the armour gone: 0, 2, 4 and 6 clusters shed.
            int n = shardbacks.Count;
            float armour = walker.Health.ArmorMax * n / 3f;
            if (armour > 0f) walker.Health.TakeDamage(armour / (1f - walker.Health.ArmorReduction) + 0.01f);
            report.Append("shardback armour ").Append(walker.Health.ArmorFraction.ToString("0.00", inv)).Append("; ");
            shardbacks.Add(walker);
        }

        k++;
    }
}
else if (args[0] == "view")
{
    Game.Enemies.Enemy target = null;
    int seen = 0;
    foreach (var walker in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
    {
        if (walker.Definition.name != "Shardback") continue;
        target = walker;
        if (++seen == 2) break;
    }

    if (target == null) return "no shardback";
    var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
    var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
    if (pan != null) pan.enabled = false;
    float fit = UnityEngine.Camera.main.orthographicSize * rig.Zoom;
    rig.SetView(target.Position, fit / float.Parse(args[1], inv));
    report.Append("view ").Append(target.Position.ToString("0.0"));
}

return report.ToString();
