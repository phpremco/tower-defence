// Round 23a: shows the look before the fix for comparison: switches every display-stage light on and lets it reach every
// layer, as the codex's closed stages did until round 23a. "off" in r23a_oldleak.txt switches them off again.
bool on = System.IO.File.ReadAllText("Tools/PlayTest/r23a_oldleak.txt").Trim() == "on";
int count = 0;
foreach (var preview in UnityEngine.Object.FindObjectsByType<Game.UI.ModelPreview>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    var lights = (System.Collections.Generic.List<UnityEngine.Light>)typeof(Game.UI.ModelPreview).GetField("_lights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(preview);
    foreach (var light in lights) { light.enabled = on; light.cullingMask = on ? -1 : light.cullingMask; count++; }
}
if (on) { UnityEngine.RenderSettings.sun = null; }
return "preview lights " + (on ? "on" : "off") + ": " + count;
