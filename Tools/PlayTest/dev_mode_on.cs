var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.PlaytestSettings>("Assets/_Project/Settings/PlaytestSettings.asset");
var so = new UnityEditor.SerializedObject(settings);
string arg = System.IO.File.Exists("Tools/PlayTest/dev_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/dev_arg.txt").Trim() : "on";
so.FindProperty("_developerMode").boolValue = arg != "off";
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(settings);
UnityEditor.AssetDatabase.SaveAssets();
return "developer mode data flag " + so.FindProperty("_developerMode").boolValue + ", on now: " + settings.DeveloperModeOn;
