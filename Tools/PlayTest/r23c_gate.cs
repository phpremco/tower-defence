// Round 23c: drives the first gate of the level and reports its view state, for the repair-flicker hunt. The action is in
// Tools/PlayTest/r23c_gate.txt: "state", "damage <share of max>", "wreck", "repairfully", "repair <share of max>",
// "restore <share>" (the hero's repair), "focus" (camera onto the gate), "upgrade".
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/r23c_gate.txt").Trim().Split(' ');
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
Game.Towers.Tower gate = null;
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.InstanceID))
{
    if (tower.Definition.BlocksGroundEnemies) { gate = tower; break; }
}
if (gate == null) { return "no gate"; }
float amount = args.Length > 1 ? float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 0f;
switch (args[0])
{
    case "damage": gate.Health.TakeDamage(gate.Health.Max * amount); break;
    case "wreck": gate.Health.TakeDamage(gate.Health.Max * 10f); break;
    case "repairfully": gate.RepairFully(); break;
    case "repair": gate.Repair(gate.Health.Max * amount); break;
    case "restore": gate.Restore(amount); break;
    case "upgrade": gate.Upgrade(); break;
    case "focus":
        var cam = UnityEngine.Camera.main.transform;
        var rig = cam.parent != null ? cam.parent : cam;
        var offset = cam.position - UnityEngine.Vector3.ProjectOnPlane(cam.forward, UnityEngine.Vector3.up) * 0f;
        UnityEngine.Plane ground = new UnityEngine.Plane(UnityEngine.Vector3.up, UnityEngine.Vector3.zero);
        var ray = new UnityEngine.Ray(cam.position, cam.forward);
        ground.Raycast(ray, out float hit);
        UnityEngine.Vector3 look = ray.GetPoint(hit);
        rig.position += gate.transform.position - look;
        UnityEngine.Camera.main.orthographicSize = float.Parse(args.Length > 1 ? args[1] : "3", System.Globalization.CultureInfo.InvariantCulture);
        break;
}
var view = gate.GetComponent<Game.Towers.GateView>();
var sb = new System.Text.StringBuilder();
sb.Append("f").Append(UnityEngine.Time.frameCount).Append(" hp ").Append(gate.Health.Fraction.ToString("0.000")).Append(gate.IsWrecked ? " WRECK" : "");
if (view == null) { return sb.Append(" no GateView").ToString(); }
sb.Append(" view.wrecked ").Append(typeof(Game.Towers.GateView).GetField("_wrecked", flags).GetValue(view));
var art = typeof(Game.Towers.GateView).GetField("_art", flags).GetValue(view) as Game.Towers.GateArtRig;
if (art == null) { return sb.Append(" no art").ToString(); }
var rigType = typeof(Game.Towers.GateArtRig);
var model = rigType.GetField("_model", flags).GetValue(art) as Game.Core.ArtModel;
var wreck = rigType.GetField("_wreck", flags).GetValue(art) as Game.Core.ArtModel;
var overlay = rigType.GetField("_overlay", flags).GetValue(art) as UnityEngine.Renderer[];
var loose = rigType.GetField("_loose", flags).GetValue(art) as UnityEngine.Transform[];
int shown = 0; foreach (var r in overlay) { if (r.enabled) { shown++; } }
int hidden = 0; foreach (var t in loose) { if (t.localScale.sqrMagnitude < 0.01f) { hidden++; } }
sb.Append(" damaged ").Append(art.IsDamaged).Append(" collapsing ").Append(rigType.GetField("_collapsing", flags).GetValue(art));
sb.Append(" model ").Append(model != null && model.Root.gameObject.activeSelf).Append(" wreckModel ").Append(wreck != null && wreck.Root.gameObject.activeSelf);
sb.Append(" overlay ").Append(shown).Append('/').Append(overlay.Length).Append(" looseHidden ").Append(hidden).Append('/').Append(loose.Length);
if (model != null && model.Player != null) { sb.Append(" once ").Append(model.Player.Once < 0 ? "-" : model.Art.Clips[model.Player.Once].Role.ToString()).Append('@').Append(model.Player.OnceTime.ToString("0.00")); }
if (model != null) { float top = float.MinValue; foreach (var r in model.Renderers) { if (r.enabled && r.gameObject.activeInHierarchy && !r.name.EndsWith("_Damage")) { top = UnityEngine.Mathf.Max(top, r.bounds.max.y - gate.transform.position.y); } } sb.Append(" top ").Append(top.ToString("0.00")); }
if (loose.Length > 0) { sb.Append(" loose0 ").Append(loose[0].localScale.x.ToString("0.00")).Append(' ').Append(loose[0].localPosition.ToString("0.00")); }
return sb.ToString();
