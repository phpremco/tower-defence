// Reports whether the unit shader is found and supported, and how many passes it has.
var shader = UnityEngine.Shader.Find(Game.Core.SurfaceMaterials.UnitShaderName);
if (shader == null) { return "UnitLit not found"; }
return "UnitLit supported=" + shader.isSupported + " passes=" + shader.passCount + " errors=" + UnityEditor.ShaderUtil.ShaderHasError(shader);
