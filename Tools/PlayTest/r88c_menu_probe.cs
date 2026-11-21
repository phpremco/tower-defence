// Round 88c: the creatures of the menu backdrop. Says for every boss among them whether it shows, where it is on the
// screen (viewport x y, so a still can be cropped on it) and how many boss ground marks (BossAuraView) exist in the scene.
var inv = System.Globalization.CultureInfo.InvariantCulture;
var camera = UnityEngine.Camera.main;
var text = new System.Text.StringBuilder();
int auras = UnityEngine.Object.FindObjectsByType<Game.Enemies.BossAuraView>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None).Length;
int marks = 0;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (t.name == "BossAura") marks++;
}
text.Append("aura components ").Append(auras).Append(", BossAura objects ").Append(marks).Append("; ");
foreach (var creature in UnityEngine.Object.FindObjectsByType<Game.UI.MenuCreature>(UnityEngine.FindObjectsSortMode.None))
{
    var enemy = creature.GetComponent<Game.Enemies.Enemy>();
    if (enemy == null || enemy.Definition == null || !enemy.Definition.IsBoss) continue;
    bool shown = false;
    foreach (var r in creature.GetComponentsInChildren<UnityEngine.Renderer>(true)) shown |= r.enabled && r.gameObject.activeInHierarchy;
    UnityEngine.Vector3 vp = camera != null ? camera.WorldToViewportPoint(creature.transform.position) : UnityEngine.Vector3.zero;
    text.Append(enemy.Definition.name).Append(shown && enemy.IsAlive ? " SHOWN" : " hidden").Append(" vp ").Append(vp.x.ToString("0.000", inv)).Append(' ').Append(vp.y.ToString("0.000", inv)).Append("; ");
}
return text.ToString();
