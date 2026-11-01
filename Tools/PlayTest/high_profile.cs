// Switches the tilt-shift pass back on and saves the renderer, after low_profile.cs switched it off for a test.
var data = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
foreach (var feature in data.rendererFeatures) if (feature is UnityEngine.Rendering.Universal.FullScreenPassRendererFeature) { feature.SetActive(true); UnityEditor.EditorUtility.SetDirty(feature); }
UnityEditor.EditorUtility.SetDirty(data); UnityEditor.AssetDatabase.SaveAssets();
return "tilt-shift on";
