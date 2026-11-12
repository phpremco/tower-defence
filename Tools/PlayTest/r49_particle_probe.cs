// Round 49 smoke material fix: lists every particle renderer in the open scenes with the material it draws with, flagging
// the URP default particle material (editor-only: null in a player build) and a missing material. Groups by
// "parent/name material" with the renderer count and the live particle count, so a damaged tower's smoke shows up.
var fallback = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
var groups = new System.Collections.Generic.SortedDictionary<string, int[]>();
int flagged = 0;
foreach (var renderer in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystemRenderer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    var material = renderer.sharedMaterial;
    // A renderer switched off draws nothing (an impact part the effect has no sheet for), whatever its material.
    string tag = !renderer.enabled ? "off" : material == null ? "NULL" : material == fallback ? "DEFAULT" : material.name;
    bool bad = renderer.enabled && (material == null || material == fallback);
    flagged += bad ? 1 : 0;
    var parent = renderer.transform.parent;
    string key = (bad ? "!! " : "   ") + (parent != null ? parent.name : "-") + "/" + renderer.name + " " + tag;
    var system = renderer.GetComponent<UnityEngine.ParticleSystem>();
    if (!groups.TryGetValue(key, out int[] counts)) { counts = new int[2]; groups[key] = counts; }
    counts[0]++;
    counts[1] += system != null ? system.particleCount : 0;
}

var report = new System.Text.StringBuilder("flagged " + flagged + " of " + groups.Count + " groups");
foreach (var pair in groups)
{
    report.Append(" | ").Append(pair.Key).Append(" x").Append(pair.Value[0]).Append(" p").Append(pair.Value[1]);
}

return report.ToString();
