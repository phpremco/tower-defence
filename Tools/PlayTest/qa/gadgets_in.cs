// QA round 32: hands the running level two street weapons (in memory, for the bar cycling test; the HUD bar is not rebuilt).
var pc = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var c = (Game.Levels.LevelContext)typeof(Game.Levels.PlacementController).GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(pc);
var list = new System.Collections.Generic.List<Game.PathObjects.PathObjectDefinition>();
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:PathObjectDefinition"))
{
    var d = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.PathObjects.PathObjectDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (d != null && list.Count < 2) list.Add(d);
}
c.SetLoadout(list);
return "street weapons: " + list.Count + " " + (list.Count > 0 ? list[0].DisplayName : "") + " " + (list.Count > 1 ? list[1].DisplayName : "") + " towers " + c.AvailableTowers.Count;
