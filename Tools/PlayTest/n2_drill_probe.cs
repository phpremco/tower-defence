// Night 2: samples the drill's Sink and Spin clips and reports where the bit tip and the block sit, plus the deck bounds.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/Story/Cutscene_Drill.asset");
var go = UnityEngine.Object.Instantiate(art.Model);
var sb = new System.Text.StringBuilder();
UnityEngine.Transform Find(UnityEngine.Transform t, string n) { if (t.name == n) return t; for (int i = 0; i < t.childCount; i++) { var f = Find(t.GetChild(i), n); if (f != null) return f; } return null; }
var tip = Find(go.transform, "BitTip"); var str = Find(go.transform, "String"); var block = Find(go.transform, "Block");
foreach (var c in art.Clips) {
  foreach (float s in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }) { c.Clip.SampleAnimation(go, s * c.Length); sb.Append(c.Role + " " + s + ": tip y " + tip.position.y.ToString("0.00") + " block y " + block.position.y.ToString("0.00") + " str rot " + str.localEulerAngles.ToString("0") + "; "); }
}
var r = go.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
sb.Append(" mesh bounds " + r.sharedMesh.bounds.ToString("0.00") + " scale " + art.Scale + " yaw " + art.Yaw);
UnityEngine.Object.DestroyImmediate(go);
return sb.ToString();
