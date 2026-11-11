// Round 37: the mothership's layout in its own space for the orbit fixes: each renderer's bounds, the Bay socket and its
// out direction, the nozzles, the light sockets, and any sockets or child names that look like a logo or decal panel.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
player.Seek(2, 0.2f);
var ship = UnityEngine.GameObject.Find("Mothership").transform;
var sb = new System.Text.StringBuilder();
sb.Append("ship pos ").Append(ship.position.ToString("0.00")).Append(" rot ").Append(ship.rotation.eulerAngles.ToString("0.0")).Append(" lossy ").Append(ship.lossyScale.ToString("0.00")).Append('\n');
foreach (var r in ship.GetComponentsInChildren<UnityEngine.Renderer>(true))
{
    var b = r.bounds;
    sb.Append("R ").Append(r.name).Append(' ').Append(r.GetType().Name).Append(" localC ").Append(ship.InverseTransformPoint(b.center).ToString("0.00")).Append(" worldSize ").Append(b.size.ToString("0.00"));
    if (r is UnityEngine.MeshRenderer) { var mf = r.GetComponent<UnityEngine.MeshFilter>(); if (mf && mf.sharedMesh) sb.Append(" meshB ").Append(mf.sharedMesh.bounds.center.ToString("0.00")).Append('/').Append(mf.sharedMesh.bounds.size.ToString("0.00")).Append(" mats ").Append(r.sharedMaterials.Length); }
    if (r is UnityEngine.SkinnedMeshRenderer smr) sb.Append(" localB ").Append(smr.localBounds.center.ToString("0.00")).Append('/').Append(smr.localBounds.size.ToString("0.00")).Append(" mats ").Append(r.sharedMaterials.Length).Append(" ").Append(r.sharedMaterial ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "");
    sb.Append('\n');
}
foreach (var t in ship.GetComponentsInChildren<UnityEngine.Transform>(true))
{
    string n = t.name;
    if (n.StartsWith("Bay") || n.StartsWith("Nozzle") || n.StartsWith("Light") || n.ToLower().Contains("logo") || n.ToLower().Contains("decal") || n.ToLower().Contains("mark") || n.ToLower().Contains("panel"))
        sb.Append("S ").Append(n).Append(" local ").Append(ship.InverseTransformPoint(t.position).ToString("0.00")).Append(" fwd ").Append(ship.InverseTransformDirection(t.forward).ToString("0.00")).Append(" up ").Append(ship.InverseTransformDirection(t.up).ToString("0.00")).Append('\n');
}
var cam = UnityEngine.Camera.main.transform;
sb.Append("cam local ").Append(ship.InverseTransformPoint(cam.position).ToString("0.0")).Append(" camFwd local ").Append(ship.InverseTransformDirection(cam.forward).ToString("0.00")).Append(" dist ").Append(UnityEngine.Vector3.Distance(cam.position, ship.position).ToString("0.0")).Append(" fov ").Append(UnityEngine.Camera.main.fieldOfView.ToString("0.0")).Append('\n');
player.Seek(2, 0.9f);
sb.Append("cam@0.9 local ").Append(ship.InverseTransformPoint(cam.position).ToString("0.0")).Append(" fwd ").Append(ship.InverseTransformDirection(cam.forward).ToString("0.00")).Append(" fov ").Append(UnityEngine.Camera.main.fieldOfView.ToString("0.0")).Append('\n');
return sb.ToString();
