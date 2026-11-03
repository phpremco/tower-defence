// Reports the material of the first glow quad named in Tools/PlayTest/look_arg.txt (shader, textures, keywords, scale).
var name = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var go = UnityEngine.GameObject.Find(name);
if (go == null) return "no " + name;
var r = go.GetComponent<UnityEngine.MeshRenderer>();
var m = r.sharedMaterial;
var t = m.GetTexture("_BaseMap");
var e = m.GetTexture("_EmissionMap");
return name + " shader=" + m.shader.name + " base=" + (t == null ? "null" : t.name + t.width) + " emis=" + (e == null ? "null" : e.name + e.width) + " kw=" + string.Join(",", m.shaderKeywords) + " scale=" + go.transform.lossyScale + " queue=" + m.renderQueue + " surf=" + m.GetFloat("_Surface");
