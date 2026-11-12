// Round 54: hides ("on") or shows again ("off", from Tools/PlayTest/r54_boss_hidden.txt) every renderer and world canvas
// of the living Shattered, so a still shows the RuinGate's iris mid-fold behind the giant that stands in it. Visual only.
bool hide = System.IO.File.ReadAllText("Tools/PlayTest/r54_boss_hidden.txt").Trim() == "on";
int count = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != "Shattered") { continue; }
    foreach (var r in enemy.GetComponentsInChildren<UnityEngine.Renderer>(true)) { r.forceRenderingOff = hide; count++; }
    foreach (var c in enemy.GetComponentsInChildren<UnityEngine.Canvas>(true)) { c.enabled = !hide; }
}

return (hide ? "hid " : "showed ") + count + " renderers";
