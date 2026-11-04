// Import stage 4a: what each continent-1 level holds for the environment (grid, plazas, terrain kinds, nests, theme).
var report = new System.Text.StringBuilder();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
{
    var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    var kinds = new System.Collections.Generic.Dictionary<Game.Levels.TerrainKind, int>();
    foreach (var pair in level.Terrain.Cells) { kinds.TryGetValue(pair.Value, out int n); kinds[pair.Value] = n + 1; }
    report.Append(level.name).Append(" grid ").Append(level.GridSize).Append(" road ").Append(level.Road.Cells.Count).Append(" plaza ").Append(level.PlazaCells.Length);
    foreach (var pair in kinds) report.Append(' ').Append(pair.Key).Append('=').Append(pair.Value);
    report.Append(" nests");
    foreach (var nest in level.Nests) report.Append(' ').Append(nest.Element).Append(nest.Cell).Append(level.Terrain.KindAt(nest.Cell));
    report.Append(" theme ").Append(level.Theme.name).Append(" props ").Append(string.Join(",", level.Theme.PropKinds)).Append(" | ");
}
return report.ToString();
