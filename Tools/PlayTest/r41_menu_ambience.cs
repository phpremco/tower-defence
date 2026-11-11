// Round 41: points the menu stage at the recorded wind bed (the generator does the same on its next run).
UnityEditor.AssetDatabase.Refresh();
var stage = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.UI.MenuStageDefinition>("Assets/_Project/Data/Menu/MainMenuStage.asset");
var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>("Assets/_Project/Audio/Approved/Environment/wind_plain_loop.wav");
if (stage == null || clip == null) return "missing " + (stage == null ? "stage " : "") + (clip == null ? "clip" : "");
var so = new UnityEditor.SerializedObject(stage);
so.FindProperty("_ambience").objectReferenceValue = clip;
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.SaveAssets();
return "menu ambience = " + clip.name + " " + clip.length.ToString("0.0") + "s";
