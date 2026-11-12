// Round 57: where the Sealed One's bar height comes from: the rest top by renderer bounds (the builder's floor), the baked
// rest top, the covered (75 %) and the highest top of its Idle and Walk samples, and the same with the halo rings as crest
// bones projected by the play camera; per renderer the baked rest top, so the halo's share can be read.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/SealedOne.asset");
float boundsTop = 0f;
var probe = UnityEngine.Object.Instantiate(art.Model);
var baked = new UnityEngine.Mesh();
var sb = new System.Text.StringBuilder();
foreach (var r in probe.GetComponentsInChildren<UnityEngine.Renderer>()) boundsTop = UnityEngine.Mathf.Max(boundsTop, r.bounds.max.y);
foreach (var r in probe.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>()) { r.BakeMesh(baked, true); float top = float.MinValue; foreach (var v in baked.vertices) top = UnityEngine.Mathf.Max(top, r.transform.localToWorldMatrix.MultiplyPoint3x4(v).y); sb.Append(r.name + " rest " + top.ToString("0.00") + " bounds " + r.bounds.max.y.ToString("0.00") + "; "); }
UnityEngine.Object.DestroyImmediate(baked); UnityEngine.Object.DestroyImmediate(probe);
var tops = Game.Editor.CreatureBarHeight.CycleTops(art.Model, art);
var sorted = new System.Collections.Generic.List<float>(tops); sorted.Sort();
var crest = Game.Editor.CreatureBarHeight.CycleTops(art.Model, art, null, new[] { "Ring_1", "Ring_2", "Ring_3" });
return "height " + art.Height.ToString("0.000") + " boundsTop " + boundsTop.ToString("0.000") + " cycles n=" + sorted.Count + " min " + sorted[0].ToString("0.00") + " covered " + Game.Editor.CreatureBarHeight.Covered(tops).ToString("0.000") + " max " + sorted[sorted.Count - 1].ToString("0.00") + " | crest covered " + Game.Editor.CreatureBarHeight.Covered(crest).ToString("0.000") + " | " + sb;
