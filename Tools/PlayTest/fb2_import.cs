// Planet pass 2026-11-04: the targeted import (the full generator would sync exports Blender agents are still working on).
// Reimports the mothership maps (2048 override), rebuilds the mothership (with its LOD1) and Voss (2048, blend shapes,
// 1.65 m) and her bust, then syncs and wires the S-69-CRYS planet maps. Run with Play Mode stopped.
foreach (var file in System.IO.Directory.GetFiles("Assets/_Project/Art/Models/Blender/Cutscene_Mothership", "*.png", System.IO.SearchOption.AllDirectories))
{
    UnityEditor.AssetDatabase.ImportAsset(file.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar), UnityEditor.ImportAssetOptions.ForceUpdate);
}
int rebuilt = Game.Editor.BlenderStoryBuilder.RebuildOnly("Cutscene_Mothership", "Voss");
var set = Game.Editor.BlenderPlanetBuilder.SyncAndBuild();
bool applied = Game.Editor.BlenderPlanetBuilder.Apply(set);
UnityEditor.AssetDatabase.SaveAssets();
return "rebuilt " + rebuilt + ", planet " + (set != null && set.IsComplete) + ", applied " + applied;
