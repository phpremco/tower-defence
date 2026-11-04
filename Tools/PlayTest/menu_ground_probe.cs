// Round menu 2026-11-04: every menu creature's feet against the Blender height map (flyers excluded by height).
var stage = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.UI.MenuStageDefinition>("Assets/_Project/Data/Menu/MainMenuStage.asset");
var terrain = new Game.UI.MenuTerrain(stage.Art);
var sb = new System.Text.StringBuilder();
foreach (var c in UnityEngine.Object.FindObjectsByType<Game.UI.MenuCreature>())
{
    var p = c.transform.position;
    sb.Append(c.name).Append(" (").Append(p.x.ToString("0.0")).Append(",").Append(p.z.ToString("0.0")).Append(") y=").Append(p.y.ToString("0.00")).Append(" ground=").Append(terrain.Height(p.x, p.z).ToString("0.00")).Append(c.gameObject.activeInHierarchy ? "" : " hidden").Append("; ");
}
return sb.ToString();
