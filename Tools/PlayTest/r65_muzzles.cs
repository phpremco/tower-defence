// Round 65: the rest-pose height of every shot origin per tower and tier (the lowest and the highest), for the line-of-fire check.
var sb = new System.Text.StringBuilder();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:TowerDefinition", new[] { "Assets/_Project/Data/Towers" }))
{
    var tower = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    sb.Append(tower.name + ":");
    for (int level = 0; level <= tower.MaxLevelIndex; level++)
    {
        sb.Append(" L" + level + " range " + tower.GetLevel(level).Range.ToString("0.00"));
        if (tower.Art == null) { sb.Append(" (no art)"); continue; }
        var art = tower.Art.ArtFor(level);
        if (art == null || art.Model == null) { sb.Append(" (no model)"); continue; }
        float low = float.MaxValue, high = float.MinValue; int n = 0;
        foreach (var node in art.Model.GetComponentsInChildren<UnityEngine.Transform>(true))
        {
            if (!node.name.StartsWith(tower.Art.MuzzlePrefix)) continue;
            float y = node.position.y * art.Scale; low = System.Math.Min(low, y); high = System.Math.Max(high, y); n++;
        }
        sb.Append(" muzzles " + n + " y " + low.ToString("0.00") + ".." + high.ToString("0.00"));
    }
    sb.Append("\n");
}
return sb.ToString();
