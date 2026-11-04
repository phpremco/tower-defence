// Import stage 3b: instantiates each FBX listed in imp3_inspect.txt in its bind pose and prints the world position of
// its named nodes (to read which way the export faces) and its renderer bounds.
var sb = new System.Text.StringBuilder();
string[] names = { "Toe_R", "Foot_R", "Head", "Weapon_R", "Weapon_L", "Holster_R", "Back", "Hips", "Spine1", "Lid_L", "Jaw", "Gaze", "Core", "Halo", "Exit", "Ramp", "Jet_1", "Leg_1", "ShieldCentre", "Beacon", "BitTip", "Block", "Dust", "Exhaust", "Bay", "Nozzle_1", "Ring", "Lamp", "Ground", "Weld_L", "Rotor_1", "Rotor_2", "Cargo", "Operator", "Radar", "Lens", "StatusLight", "Centre", "Log", "Smoke" };
foreach (string line in System.IO.File.ReadAllLines("Tools/PlayTest/imp3_inspect.txt"))
{
    string path = line.Trim();
    if (path.Length == 0) continue;
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (asset == null) continue;
    var go = UnityEngine.Object.Instantiate(asset);
    sb.Append("## ").Append(asset.name).Append(": ");
    foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>(true))
        if (System.Array.IndexOf(names, t.name) >= 0) sb.Append(t.name).Append(t.position.ToString("0.00")).Append(' ');
    foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>(true))
        sb.Append("[").Append(r.name).Append(" c").Append(r.bounds.center.ToString("0.00")).Append(" s").Append(r.bounds.size.ToString("0.00")).Append("] ");
    UnityEngine.Object.DestroyImmediate(go);
    sb.Append(" || ");
}
return sb.ToString();
