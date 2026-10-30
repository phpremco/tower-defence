int props = 0; foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None)) { if (t.name == "Props") props = t.childCount; }
var field = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
return "prop children " + props + " renderers " + UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>(UnityEngine.FindObjectsSortMode.None).Length;
