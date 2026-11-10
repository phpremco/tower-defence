// Round 36: the targeted import of the capsule's deorbit booster (Tools/Blender/build_booster36.py): syncs and builds only
// Cutscene_Booster (the full generator would sync exports other Blender agents are still working on) and points the cutscene
// cast at it, as BlenderStoryBuilder.Apply does on a full run. Run with Play Mode stopped.
int rebuilt = Game.Editor.BlenderStoryBuilder.RebuildOnly("Cutscene_Booster");
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(Game.Editor.BlenderStoryBuilder.StoryArtFolder + "/Cutscene_Booster.asset");
int pointed = 0;
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:CutsceneCast"))
{
    var cast = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CutsceneCast>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    var so = new UnityEditor.SerializedObject(cast);
    so.FindProperty("_boosterArt").objectReferenceValue = art;
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(cast);
    pointed++;
}

UnityEditor.AssetDatabase.SaveAssets();
return "rebuilt " + rebuilt + ", art " + (art != null) + ", casts " + pointed;
