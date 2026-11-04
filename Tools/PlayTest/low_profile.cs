// Switches to the Low (Switch) profile for the next level that starts: registers the settings (the registry
// replaces the PC ones) and switches the tilt-shift pass off, as Boot does for that profile. Boot switches it back
// on at the next start with the PC profile.
Game.Core.Services.Register<Game.Platform.IPlatformSettings>(new Game.Platform.SwitchPlatformSettings());
Game.Core.Services.Register(new Game.Core.ModelQuality(true));
UnityEngine.QualitySettings.lodBias = 0.6f;
var data = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
foreach (var feature in data.rendererFeatures)
{
    if (feature is UnityEngine.Rendering.Universal.FullScreenPassRendererFeature) feature.SetActive(false);
}
return "profile " + Game.Core.Services.Get<Game.Platform.IPlatformSettings>().ProfileName;
