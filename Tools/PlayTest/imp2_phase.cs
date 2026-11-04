// Import stage 2 screenshot phases, one CLI call each, so slow CLI round trips do not let the level run on between
// captures: the editor stays paused except between "play" and "pause". Tools/PlayTest/imp2_phase.txt holds one phase:
//   setup <road index>       waves off, the tower lineup (level 1 by the road, level 3 behind), creatures sent
//   view <lineup|wreck> <ortho half height>
//   play | pause             lets the level run in real time, or holds it, between captures
//   hurt <health share>      every lineup tower hurt to that share
//   wreck <name part>        lineup towers whose name contains it are wrecked
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/imp2_phase.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
if (args[0] != "play") UnityEditor.EditorApplication.isPaused = true;
var report = new System.Text.StringBuilder();
var parent = UnityEngine.GameObject.Find("Imp2Towers");
if (args[0] == "setup")
{
    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    controller.enabled = false;
    int start = int.Parse(args[1]);
    UnityEngine.Vector2Int[] road = context.Level.MainRoad;
    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TowerDefinition", new[] { "Assets/_Project/Data/Towers" });
    if (parent != null) UnityEngine.Object.Destroy(parent);
    parent = new UnityEngine.GameObject("Imp2Towers");
    const float Spacing = 1.15f;
    const float RowSpacing = 1.5f;
    // Along the road leg that starts at the chosen waypoint, just off its edge, so creatures on the leg are in range.
    int a = System.Math.Min(start, road.Length - 2);
    UnityEngine.Vector3 legStart = context.Level.Grid.CellToWorld(road[a]);
    UnityEngine.Vector3 along = (context.Level.Grid.CellToWorld(road[a + 1]) - legStart).normalized;
    UnityEngine.Vector3 side = new UnityEngine.Vector3(along.z, 0f, -along.x);
    if (side.z > 0f) side = -side;
    UnityEngine.Vector3 origin = legStart + along * 2f + side * (context.Level.Road.Width * 0.5f + 1.3f);
    int index = 0;
    foreach (string guid in guids)
    {
        var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
        if (definition == null || definition.BlocksGroundEnemies) continue;
        UnityEngine.Vector3 here = origin + along * (index * Spacing);
        index++;
        for (int row = 0; row < 2; row++)
        {
            var go = new UnityEngine.GameObject("Imp2_" + definition.name + "_L" + (row == 0 ? 1 : 3));
            go.transform.SetParent(parent.transform, true);
            go.transform.position = here + side * (row * RowSpacing);
            var tower = go.AddComponent<Game.Towers.Tower>();
            go.AddComponent<Game.Towers.TowerView>();
            tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
            tower.Register(context.Structures);
            if (row == 1) tower.SetLevel(definition.MaxLevelIndex);
        }
    }

    string[] kinds = { "Skitter", "Cragback", "Prowler", "Skyray", "Shalebat", "Roller", "Skitter", "Skyray" };
    var field = typeof(Game.Levels.LevelController).GetField("_spawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var spawner = (Game.Levels.EnemySpawner)field.GetValue(controller);
    foreach (string kind in kinds)
    {
        var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + kind + ".asset");
        if (enemy != null) spawner.Spawn(enemy, 0, Game.Levels.SpawnSource.Gate);
    }

    // Walk them most of the way in at once, so the lineup has something to shoot within seconds of play.
    foreach (var walker in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
    {
        for (int i = 0; i < 160; i++) walker.Step(0.1f);
    }

    report.Append("placed ").Append(index * 2).Append(" at ").Append(origin.ToString("0.0")).Append(", alive ").Append(spawner.Alive).Append("; ");
}
else if (args[0] == "view")
{
    UnityEngine.Vector3 focus = UnityEngine.Vector3.zero;
    int count = 0;
    foreach (UnityEngine.Transform child in parent.transform)
    {
        var tower = child.GetComponent<Game.Towers.Tower>();
        if (args[1] == "wreck" && (tower == null || !tower.IsWrecked)) continue;
        focus += child.position;
        count++;
        if (args[1] == "wreck") break;
    }

    if (count == 0) return "nothing to look at";
    focus /= count;
    var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
    var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
    if (pan != null) pan.enabled = false;
    // SetView takes a zoom factor on the level's fit size; the phase names the orthographic half height it wants.
    float fit = UnityEngine.Camera.main.orthographicSize * rig.Zoom;
    rig.SetView(focus, fit / float.Parse(args[2], inv));
    report.Append("view ").Append(focus.ToString("0.0")).Append("; ");
}
else if (args[0] == "hurt")
{
    float share = float.Parse(args[1], inv);
    foreach (UnityEngine.Transform child in parent.transform)
    {
        var tower = child.GetComponent<Game.Towers.Tower>();
        if (tower != null && !tower.IsWrecked) tower.TakeDamage(tower.Health.Current - tower.Health.Max * share);
    }

}
else if (args[0] == "wreck")
{
    foreach (UnityEngine.Transform child in parent.transform)
    {
        var tower = child.GetComponent<Game.Towers.Tower>();
        if (tower != null && !tower.IsWrecked && child.name.Contains(args[1]))
        {
            tower.TakeDamage(1000000f);
            report.Append("wrecked ").Append(child.name);
            UnityEngine.Transform shell = child.Find("Tower_Wreck");
            if (shell == null) report.Append(" (no wreck model)");
            else
            {
                int shown = 0;
                foreach (var r in shell.GetComponentsInChildren<UnityEngine.Renderer>()) { shown++; report.Append(" ").Append(r.name).Append(r.bounds.center.ToString("0.0")).Append(r.bounds.size.ToString("0.0")); }
                report.Append(" active=").Append(shell.gameObject.activeInHierarchy).Append(" renderers=").Append(shown);
            }

            report.Append("; ");
        }
    }

}

// EditorApplication.Step only queues one frame however often it is called, so time passes by playing between phases.
if (args[0] == "play") UnityEditor.EditorApplication.isPaused = false;
// A tower count and whether any of them failed to build its model, for the log.
int modelled = 0;
if (parent != null)
{
    foreach (var art in parent.GetComponentsInChildren<Game.Towers.TowerArtView>(true)) modelled++;
}

return report.Append(args[0]).Append(", modelled ").Append(modelled).ToString();
