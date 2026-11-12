// Round 57: writes Tools/PlayTest/r50_orbit.txt so r50_still.cs orbits the camera round to the face of the first living enemy
// named in Tools/PlayTest/enemy_arg.txt, at the extra yaw in its second word (degrees), turning about the height in its third
// (default 1). Unlike r45_face.txt this leaves the creature where it is, so effects it already threw (a moment's sparks in
// world space) stay on it in the still.
var spec = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
float extra = spec.Length > 1 ? float.Parse(spec[1], inv) : 0f;
float height = spec.Length > 2 ? float.Parse(spec[2], inv) : 1f;
var camera = UnityEngine.Camera.main;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != spec[0]) { continue; }
    var view = enemy.GetComponentInChildren<Game.Enemies.EnemyView>(true);
    var facing = view != null ? view.transform.forward : enemy.transform.forward;
    var look = new UnityEngine.Vector3(camera.transform.forward.x, 0f, camera.transform.forward.z);
    // The imported creature model is turned half round under its view (heads at -Z in the export), so the view's forward is its back.
    var face = new UnityEngine.Vector3(facing.x, 0f, facing.z);
    float yaw = UnityEngine.Vector3.SignedAngle(look, face, UnityEngine.Vector3.up) + extra;
    System.IO.File.WriteAllText("Tools/PlayTest/r50_orbit.txt", yaw.ToString("0.0", inv) + " 0 " + height.ToString("0.00", inv));
    return "orbit " + yaw.ToString("0.0") + " about " + height.ToString("0.00");
}

return "no living " + spec[0];
