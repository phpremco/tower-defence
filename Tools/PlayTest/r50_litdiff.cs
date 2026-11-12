// Round 50: compares the URP default material with a fresh material on the Lit shader (every float, colour and vector).
var a = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial;
var b = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
var sb = new System.Text.StringBuilder("same shader: " + (a.shader == b.shader) + "; queue " + a.renderQueue + "/" + b.renderQueue + "; kw " + string.Join(",", a.shaderKeywords) + "/" + string.Join(",", b.shaderKeywords) + "; ");
var s = a.shader;
for (int i = 0; i < s.GetPropertyCount(); i++)
{
    string n = s.GetPropertyName(i);
    var t = s.GetPropertyType(i);
    if (t == UnityEngine.Rendering.ShaderPropertyType.Color || t == UnityEngine.Rendering.ShaderPropertyType.Vector) { if (a.GetVector(n) != b.GetVector(n)) sb.Append(n + " " + a.GetVector(n) + " vs " + b.GetVector(n) + "; "); }
    else if (t == UnityEngine.Rendering.ShaderPropertyType.Float || t == UnityEngine.Rendering.ShaderPropertyType.Range) { if (a.GetFloat(n) != b.GetFloat(n)) sb.Append(n + " " + a.GetFloat(n) + " vs " + b.GetFloat(n) + "; "); }
    else if (t == UnityEngine.Rendering.ShaderPropertyType.Texture) { if (a.GetTexture(n) != b.GetTexture(n)) sb.Append(n + " tex differs; "); }
}
UnityEngine.Object.DestroyImmediate(b);
return sb.ToString();
