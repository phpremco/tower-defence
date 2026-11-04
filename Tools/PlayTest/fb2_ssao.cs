// Switches the SSAO renderer feature on PC_Renderer on or off for a test ("on"/"off" in Tools/PlayTest/fb2_ssao_arg.txt); in memory only.
var on = System.IO.File.ReadAllText("Tools/PlayTest/fb2_ssao_arg.txt").Trim() == "on";
var data = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
foreach (var f in data.rendererFeatures) if (f != null && f.name.Contains("Ambient")) f.SetActive(on);
return "ssao " + on;
