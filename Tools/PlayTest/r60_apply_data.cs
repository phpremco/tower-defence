// Round 60: writes this round's data the way the full generator now would, without running it (Remco's rule): the drones'
// roles and summaries, the swarm species' death levels, and the south air route's start moved off a road mouth
// (RoadGenerator.SouthAirStart). Edit mode; safe to run again.
var sb = new System.Text.StringBuilder();
System.Action<string, string, string> drone = (id, role, summary) =>
{
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Drones.DroneDefinition>("Assets/_Project/Data/Drones/" + id + ".asset");
    var so = new UnityEditor.SerializedObject(asset);
    so.FindProperty("_role").stringValue = role;
    so.FindProperty("_summary").stringValue = summary;
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(asset);
    sb.Append(id).Append(" role ").Append(role).Append("; ");
};
drone("RepairDrone", "Repair", Game.Editor.ProjectGenerator.RepairDroneSummary);
drone("HarvestDrone", "Harvest", Game.Editor.ProjectGenerator.HarvestDroneSummary);
drone("AttackDrone", "Attack", Game.Editor.ProjectGenerator.AttackDroneSummary);

foreach (var species in new[] { "Skitter", "FireLarva", "RotSporePod" })
{
    var voice = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.CreatureVoice>("Assets/_Project/Data/Audio/Voices/Voice_" + species + ".asset");
    var so = new UnityEditor.SerializedObject(voice);
    so.FindProperty("_deathVolume").floatValue = Game.Editor.ProjectGenerator.SwarmDeathVolume;
    so.FindProperty("_deathInterval").floatValue = Game.Editor.ProjectGenerator.SwarmDeathInterval;
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(voice);
    sb.Append("voice ").Append(species).Append("; ");
}

foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Game.Levels.LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
{
    var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    bool mouth = false;
    int south = -1;
    for (int p = 0; p < level.Paths.Length; p++)
    {
        var w = level.Paths[p].Waypoints;
        if (level.Paths[p].Name == "South air route") south = p;
        else if (!level.Paths[p].Flying && w.Length > 0 && w[0].x == 0 && w[0].y <= 3) mouth = true;
    }
    if (south < 0 || !mouth) continue;
    var so = new UnityEditor.SerializedObject(level);
    var first = so.FindProperty("_paths").GetArrayElementAtIndex(south).FindPropertyRelative("Waypoints").GetArrayElementAtIndex(0);
    var before = first.vector2IntValue;
    if (before != new UnityEngine.Vector2Int(0, 0)) { sb.Append(level.name).Append(" south starts at ").Append(before).Append(" (left); "); continue; }
    first.vector2IntValue = new UnityEngine.Vector2Int(3, 0);
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(level);
    sb.Append(level.name).Append(" south air ").Append(before).Append("->(3, 0); ");
}
UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
