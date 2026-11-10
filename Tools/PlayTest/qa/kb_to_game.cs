// QA round 32: lets simulated keyboard and mouse events reach the game while the Game view has no focus (in memory only:
// the project has no Input System settings asset, and a domain reload resets it).
var s = UnityEngine.InputSystem.InputSystem.settings;
string was = s.editorInputBehaviorInPlayMode.ToString();
s.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
return "was " + was + ", asset '" + UnityEditor.AssetDatabase.GetAssetPath(s) + "'";
