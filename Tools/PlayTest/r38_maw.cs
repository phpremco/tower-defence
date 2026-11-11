// Round 38 (Gravemaw's worm maw): Tools/PlayTest/r38_arg.txt picks the action on the living continent boss.
//   "probe"   every renderer of the boss model: name, enabled, LOD, materials (shows the maw wears its own material)
//   "roar"    plays its Roar clip as a scripted clip (the maw opens wide)
//   "face"    turns it to face the play camera (a diagnostic still of the throat; the fight goes on from there)
//   "lod <n>" forces LOD n on the model's LOD groups (-1 hands the choice back to the camera distance)
string[] arg = System.IO.File.ReadAllText("Tools/PlayTest/r38_arg.txt").Trim().Split(' ');
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain == null) { return "no boss"; }
var view = brain.GetComponentInChildren<Game.Enemies.EnemyView>(true);
if (view == null) { view = brain.transform.parent != null ? brain.transform.parent.GetComponentInChildren<Game.Enemies.EnemyView>(true) : null; }
if (arg[0] == "roar")
{
    view.PlayScripted(Game.Core.ModelClipRole.Roar);
    return "roar on " + view.name + " state " + brain.State;
}

if (arg[0] == "face")
{
    // A diagnostic turn only: the head towards the play camera, so the open maw shows its throat.
    var enemy = brain.GetComponent<Game.Enemies.Enemy>();
    var back = -UnityEngine.Camera.main.transform.forward;
    back.y = 0f;
    enemy.Place(enemy.transform.position, back.normalized);
    return "faced " + back.normalized.ToString("0.00");
}

if (arg[0] == "lod")
{
    int lod = int.Parse(arg[1]);
    int groups = 0;
    foreach (var group in view.GetComponentsInChildren<UnityEngine.LODGroup>(true)) { group.ForceLOD(lod); groups++; }
    return "forced LOD " + lod + " on " + groups + " group(s)";
}

string report = "state " + brain.State + ";";
foreach (var renderer in view.GetComponentsInChildren<UnityEngine.Renderer>(true))
{
    if (!(renderer is UnityEngine.SkinnedMeshRenderer)) { continue; }
    var names = new System.Collections.Generic.List<string>();
    foreach (var material in renderer.sharedMaterials) { names.Add(material != null ? material.name + "<" + (material.GetTexture("_BaseMap") != null ? material.GetTexture("_BaseMap").name : "none") + ">" : "null"); }
    report += " " + renderer.name + (renderer.enabled ? "" : "(off)") + (renderer.isVisible ? " vis" : "") + "=[" + string.Join(",", names) + "]";
}

return report;
