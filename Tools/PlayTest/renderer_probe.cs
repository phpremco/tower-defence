// Reports the renderer features on the active pipeline renderer and whether the tilt-shift pass is active.
var asset = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
var data = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
var sb = new System.Text.StringBuilder();
foreach (var f in data.rendererFeatures) sb.Append((f ? f.name + " active=" + f.isActive : "null") + "; ");
var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/_Project/Art/Materials/TiltShift.mat");
sb.Append("material shader=" + (m ? m.shader.name + " supported=" + m.shader.isSupported : "none"));
return sb.ToString();
