var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.BaseArtDefinition>("Assets/_Project/Data/Art/Colony/BaseArt.asset");
var sb = new System.Text.StringBuilder();
var holder = new UnityEngine.GameObject("Probe");
var model = Game.Core.ArtModel.CreateStill(holder.transform, art.Model);
foreach (var t in model.Root.GetComponentsInChildren<UnityEngine.Transform>(true))
{
    var r = t.GetComponent<UnityEngine.Renderer>();
    sb.Append(t.name).Append(" parent=").Append(t.parent != null ? t.parent.name : "-").Append(" pos=").Append(t.position.ToString("0.00")).Append(" lrot=").Append(t.localRotation.eulerAngles.ToString("0")).Append(r != null ? " R " + r.bounds.center.ToString("0.00") + " size " + r.bounds.size.ToString("0.00") : "").Append('\n');
}
sb.Append("lod1 ").Append(art.Model.Lod1 != null).Append(" mat ").Append(art.Model.Material.name).Append(" tex ").Append(art.Model.Material.GetTexture("_BaseMap") is UnityEngine.Texture2D tx ? tx.width.ToString() : "-").Append('\n');
var low = art.Model.MaterialFor(true);
sb.Append("low ").Append(low != null ? low.name : "-").Append(" tex ").Append(low != null && low.GetTexture("_BaseMap") is UnityEngine.Texture2D lt ? lt.width.ToString() : "-").Append('\n');
UnityEngine.Object.DestroyImmediate(holder);
System.IO.File.WriteAllText("Tools/PlayTest/base2_probe.txt", sb.ToString());
return "ok";
