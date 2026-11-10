// Round 35: the opening cutscene's field after the fixes: how many eye pairs found a rock, where the drill stands against
// the level's colony rig spot, and whether any landed builder sits inside a piece of decor.
var sb = new System.Text.StringBuilder();
var eyes = UnityEngine.GameObject.Find("FieldEyes");
sb.Append("eyes ").Append(eyes != null ? eyes.transform.childCount / 2 : -1).Append(" active ").Append(eyes != null && eyes.activeInHierarchy);
var drill = UnityEngine.GameObject.Find("Drill");
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var level = catalog.Nodes[0].Level;
var clearance = new Game.Levels.StructureClearance(level);
if (drill != null) sb.Append(" | drill ").Append(drill.transform.position.ToString("0.0")).Append(" scale ").Append(drill.transform.localScale.x.ToString("0.00"));
sb.Append(" rig ").Append(clearance.HasColonyRig ? clearance.ColonyRigPosition.ToString("0.0") : "none");
var instancers = UnityEngine.Object.FindObjectsByType<Game.Levels.EnvironmentInstancer>(UnityEngine.FindObjectsSortMode.None);
int formations = 0;
foreach (var i in instancers) formations += i.Formations.Count;
sb.Append(" | formations ").Append(formations);
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None))
{
    if (!t.name.StartsWith("Builder") || t.parent == null || t.parent.name.StartsWith("Builder")) continue;
    bool clear = true;
    foreach (var i in instancers) clear &= i.IsClear(t.position, 0.6f, 0.25f);
    sb.Append(" | ").Append(t.name).Append(' ').Append(t.position.ToString("0.0")).Append(clear ? " clear" : " IN DECOR");
}
return sb.ToString();
