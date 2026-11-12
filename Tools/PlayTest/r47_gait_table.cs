// Round 47: for every enemy definition with art, the travel cycle's playback rate at its base speed (speed / (distance
// per loop x scale) x clip length), at half speed (a chill) and at 1.6x (a haste), against CreatureClips' clamp
// [0.3, 4]: a rate outside it means the feet slide. Writes Logs/r47/gait_table.txt.
var sb = new System.Text.StringBuilder();
sb.AppendLine("enemy | speed | scale | clip | dist/loop | length | rate x1 | rate x0.5 | rate x1.6 | flags");
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:EnemyDefinition"))
{
    var def = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (def == null || def.Art == null) continue;
    var art = def.Art;
    float scale = def.Scale * art.Scale;
    foreach (var clip in art.Clips)
    {
        if (clip.DistancePerLoop <= 0f) continue;
        float rate = def.Speed / (clip.DistancePerLoop * scale) * clip.Length;
        string flags = "";
        if (rate * 0.5f < 0.3f) flags += "slow-clamp ";
        if (rate * 1.6f > 4f) flags += "fast-clamp ";
        if (rate < 0.3f || rate > 4f) flags += "BASE-CLAMPED ";
        sb.AppendLine($"{def.name} | {def.Speed:0.00} | {scale:0.00} | {clip.Role} | {clip.DistancePerLoop:0.00} | {clip.Length:0.00} | {rate:0.00} | {rate * 0.5f:0.00} | {rate * 1.6f:0.00} | {flags}");
    }
}

System.IO.File.WriteAllText("Logs/r47/gait_table.txt", sb.ToString());
return "written";
