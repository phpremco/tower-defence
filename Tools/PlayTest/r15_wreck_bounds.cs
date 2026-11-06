// Round 15: lists the colony base wreck bounds (FBX asset frame, edit mode), to place idle spots for extra drones.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.BaseArtDefinition>("Assets/_Project/Data/Art/Colony/BaseArt.asset");
var prefab = art.Wreck.Model;
string report = "yaw " + art.Model.Yaw + " scale " + art.Model.Scale + "\n";
foreach (var f in prefab.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
{
    if (f.sharedMesh == null) { continue; }
    var m = f.transform.localToWorldMatrix;
    var lb = f.sharedMesh.bounds;
    var b = new UnityEngine.Bounds(m.MultiplyPoint3x4(lb.center), UnityEngine.Vector3.zero);
    for (int c = 0; c < 8; c++)
    {
        var p = new UnityEngine.Vector3((c & 1) == 0 ? lb.min.x : lb.max.x, (c & 2) == 0 ? lb.min.y : lb.max.y, (c & 4) == 0 ? lb.min.z : lb.max.z);
        b.Encapsulate(m.MultiplyPoint3x4(p));
    }
    report += f.name + " x " + b.min.x.ToString("0.00") + ".." + b.max.x.ToString("0.00") + " y " + b.min.y.ToString("0.00") + ".." + b.max.y.ToString("0.00") + " z " + b.min.z.ToString("0.00") + ".." + b.max.z.ToString("0.00") + "\n";
}
foreach (var t in prefab.GetComponentsInChildren<UnityEngine.Transform>(true))
{
    if (t.name.StartsWith("Socket_")) { report += t.name + " " + t.position.ToString("0.00") + "\n"; }
}
return report;
