// Round 39: where the parked haulers and landed builders stand (drill shot), to check spacing and slope.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
player.Seek(5, 0.5f);
var sb = new System.Text.StringBuilder();
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None))
    if ((t.name.StartsWith("Hauler") || t.name.StartsWith("Builder")) && t.parent != null && t.parent.name == "Set_Field" && t.gameObject.activeInHierarchy)
        sb.Append(t.name).Append(' ').Append(t.position.ToString("F2")).Append(" yaw ").Append(t.eulerAngles.y.ToString("0")).Append('\n');
return sb.ToString();
