// Round menu 2026-11-04: reports the menu island import (mesh parts and sub-meshes, height map format and a few
// heights, the models of the layout and which are rigged).
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.UI.MenuDioramaArt>("Assets/_Project/Data/Art/Menu/MenuDioramaArt.asset");
if (art == null) return "no art";
var sb = new System.Text.StringBuilder();
sb.Append(art.Island.name).Append(" sub=").Append(art.Island.subMeshCount).Append(" top=").Append(art.Island.GetSubMesh(0).indexCount / 3).Append(" cliff=").Append(art.Island.GetSubMesh(1).indexCount / 3).Append(" b=").Append(art.Island.bounds).Append("; ");
var hm = art.HeightMap; sb.Append("height ").Append(hm.format).Append(" ").Append(hm.width).Append(" readable=").Append(hm.isReadable).Append("; ");
var t = new Game.UI.MenuTerrain(art);
foreach (var p in new[] { new UnityEngine.Vector2(4.8f, 1.7f), new UnityEngine.Vector2(4.8f, 2.4f), new UnityEngine.Vector2(4.8f, 6.5f), new UnityEngine.Vector2(1.4f, -2.2f), new UnityEngine.Vector2(8.6f, -0.9f), new UnityEngine.Vector2(3.6f, 2.2f), new UnityEngine.Vector2(4.8f, 3.66f) })
    sb.Append("h(").Append(p.x).Append(",").Append(p.y).Append(")=").Append(t.Height(p.x, p.y).ToString("0.000")).Append(" ");
sb.Append("; models ").Append(art.Models.Length).Append(": ");
foreach (var m in art.Models) sb.Append(m.Name).Append(m.IsRigged ? "[R " + (m.Pose != null ? m.Pose.name : "-") + "]" : "").Append(" ");
foreach (var p in art.Pools) sb.Append("| pool ").Append(p.Kind).Append(" ").Append(p.Theme).Append(" area=").Append(Game.Levels.LiquidContour.SignedArea(p.Outline).ToString("0.0"));
foreach (var l in art.Lights) sb.Append("| light ").Append(l.Name).Append(" ").Append(l.Theme).Append(" ").Append(l.Colour);
return sb.ToString();
