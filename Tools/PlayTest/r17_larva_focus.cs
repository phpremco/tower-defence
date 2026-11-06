// Round 17: centres the camera on a live FireLarva at the zoom factor in r16_focus.txt (bigger = closer) and keeps
// following the same one (its object hash is kept in r17_larva_id.txt; delete the file to pick a new one).
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r16_focus.txt").Trim().Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
string idPath = "Tools/PlayTest/r17_larva_id.txt";
int wanted = System.IO.File.Exists(idPath) ? int.Parse(System.IO.File.ReadAllText(idPath).Trim()) : 0;
Game.Enemies.Enemy larva = null;
int count = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
{
    if (!enemy.isActiveAndEnabled || enemy.Definition == null || enemy.Definition.name != "FireLarva") { continue; }
    count++;
    int id = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(enemy);
    if (larva == null || id == wanted) { larva = enemy; }
}
if (larva == null) { return "none t=" + UnityEngine.Time.time.ToString("0.0"); }
System.IO.File.WriteAllText(idPath, System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(larva).ToString());
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
if (pan != null) { pan.enabled = false; }
rig.SetView(larva.transform.position, zoom);
return count + " larvae; focus at " + larva.transform.position.ToString("0.0") + " heading " + larva.Heading.ToString("0.00");
