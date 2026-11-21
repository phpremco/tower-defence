// Round 88d: sets up the stills and the play check of the round. Tools/PlayTest/r88d_scene_arg.txt, one line of words:
//   towers:<TowerId>:<count>   builds guns on the build cells nearest the first gate, through the real placement path
//   wreckgate                  wrecks the first gate (its rebuild badge shows)
//   hurtgates:<fraction>       every standing gate keeps this share of its health (its bar shows)
//   hurttowers:<fraction>      every standing gun keeps this share of its health
//   focusgate:<zoom>           the play camera on the first gate at the play zoom times <zoom>
//   cursorgate / cursoraway    the placement cursor on the first gate's cell / on the cell five to the side
//   turrets                    mounts the base turrets the save has not bought (the level's own base turret, two sides)
//   wreckturret                wrecks the first base turret
//   focusturret:<zoom>         the play camera on the first base turret
//   alertin                    the air raid note past its fade-in (real seconds barely pass while frames are stepped)
//   state                      gates, base turrets (health, wrecked), repair drones (activity), the wallet, the badges on the HUD
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c)) return "no context";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var type = typeof(Game.Levels.PlacementController);
var towers = (System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, Game.Towers.Tower>)type.GetField("_towers", flags).GetValue(placement);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig != null ? rig.GetComponent<Game.Levels.CameraPanZoom>() : null;
Game.Towers.Tower gate = null;
UnityEngine.Vector2Int gateCell = default;
Game.Towers.Tower turret = null;
foreach (var pair in towers)
{
    if (pair.Value == null) continue;
    if (pair.Value.Definition.BlocksGroundEnemies && (gate == null || pair.Key.x + pair.Key.y * 1000 < gateCell.x + gateCell.y * 1000)) { gate = pair.Value; gateCell = pair.Key; }
    if (pair.Value.Definition.IsBaseDefence && turret == null) turret = pair.Value;
}
var text = new System.Text.StringBuilder();
foreach (string word in System.IO.File.ReadAllText("Tools/PlayTest/r88d_scene_arg.txt").Trim().Split(' '))
{
    if (word.StartsWith("towers:"))
    {
        string[] p = word.Split(':');
        Game.Towers.TowerDefinition definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>("Assets/_Project/Data/Towers/" + p[1] + ".asset");
        if (definition == null || gate == null) { text.Append("no tower or gate; "); continue; }
        int wanted = int.Parse(p[2]), built = 0;
        for (int ring = 1; ring <= 4 && built < wanted; ring++)
        {
            for (int dx = -ring; dx <= ring && built < wanted; dx++)
            {
                for (int dy = -ring; dy <= ring && built < wanted; dy++)
                {
                    if (UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(dx), UnityEngine.Mathf.Abs(dy)) != ring) continue;
                    var at = new UnityEngine.Vector2Int(gateCell.x + dx, gateCell.y + dy);
                    if (towers.ContainsKey(at) || !(bool)type.GetMethod("CanBuildTower", flags).Invoke(placement, new object[] { at })) continue;
                    c.Wallet.Add(definition.BuildCost);
                    c.SelectedPlacedTower = null;
                    type.GetField("_cursor", flags).SetValue(placement, at);
                    c.SelectTower(definition);
                    type.GetMethod("TryPlace", flags).Invoke(placement, null);
                    if (towers.ContainsKey(at)) { built++; text.Append(at.x).Append(',').Append(at.y).Append(' '); }
                }
            }
        }
        c.SelectTower(null);
        c.Mode = Game.Levels.PlacementMode.Browsing;
        type.GetMethod("Refresh", flags).Invoke(placement, null);
        text.Append("built ").Append(built).Append(' ').Append(definition.name).Append(" round the gate at ").Append(gateCell).Append("; ");
    }
    else if (word == "wreckgate")
    {
        if (gate != null) { gate.TakeDamage(1e9f); text.Append("gate wrecked, rebuild ").Append(gate.RepairCost).Append("; "); }
    }
    else if (word.StartsWith("hurtgates:") || word.StartsWith("hurttowers:"))
    {
        bool gates = word.StartsWith("hurtgates:");
        float keep = float.Parse(word.Substring(word.IndexOf(':') + 1), inv);
        int hurt = 0;
        foreach (var tower in towers.Values)
        {
            if (tower == null || tower.IsWrecked || tower.Definition.BlocksGroundEnemies != gates) continue;
            tower.TakeDamage(tower.Health.Current - tower.Health.Max * keep);
            hurt++;
        }
        text.Append(gates ? "gates" : "towers").Append(" hurt ").Append(hurt).Append("; ");
    }
    else if (word.StartsWith("focusgate:") || word.StartsWith("focusturret:"))
    {
        Game.Towers.Tower on = word.StartsWith("focusgate:") ? gate : turret;
        if (on == null || rig == null) { text.Append("nothing to focus on; "); continue; }
        if (pan != null) pan.enabled = false;
        rig.SetView(on.Position, (pan != null ? pan.PlayZoom : 10f) * float.Parse(word.Substring(word.IndexOf(':') + 1), inv));
        text.Append("focus ").Append(on.Position.ToString("0.0")).Append("; ");
    }
    else if (word == "cursorgate" || word == "cursoraway")
    {
        var at = word == "cursorgate" ? gateCell : new UnityEngine.Vector2Int(gateCell.x + 5, gateCell.y + 5);
        type.GetField("_cursor", flags).SetValue(placement, at);
        c.CursorCell = at;
        type.GetMethod("Refresh", flags).Invoke(placement, null);
        text.Append("cursor ").Append(c.CursorCell).Append("; ");
    }
    else if (word == "turrets")
    {
        var art = c.Level.Theme != null ? c.Level.Theme.BaseArt : null;
        if (turret != null || c.Level.BaseTurret == null || art == null) { text.Append("turrets: ").Append(turret != null ? "there already" : "no base turret or art").Append("; "); continue; }
        type.GetMethod("MountBaseTurrets", flags).Invoke(placement, new object[] { c.Level.BaseTurret, art, UnityEngine.Mathf.Min(2, art.TurretMounts.Length), 0 });
        foreach (var tower in towers.Values) if (tower != null && tower.Definition.IsBaseDefence && turret == null) turret = tower;
        text.Append("turrets mounted: ").Append(turret != null).Append("; ");
    }
    else if (word == "wreckturret")
    {
        if (turret != null) { turret.TakeDamage(1e9f); text.Append("turret wrecked; "); }
    }
    else if (word == "alertin")
    {
        // The note fades in over real seconds, which barely pass while frames are stepped: put it past its fade for a still.
        var alert = UnityEngine.Object.FindAnyObjectByType<Game.UI.AirRaidAlertView>();
        if (alert != null) { typeof(Game.UI.AirRaidAlertView).GetField("_shown", flags).SetValue(alert, 1f); text.Append("alert past its fade; "); }
    }
    else if (word == "state")
    {
        text.Append("money ").Append(c.Wallet.Money).Append(" wave ").Append(c.WaveNumber).Append("; ");
        foreach (var tower in towers.Values)
        {
            if (tower == null || !(tower.Definition.BlocksGroundEnemies || tower.Definition.IsBaseDefence)) continue;
            text.Append(tower.Definition.name).Append(' ').Append(tower.Health.Current.ToString("0", inv)).Append('/').Append(tower.Health.Max.ToString("0", inv)).Append(tower.IsWrecked ? " WRECK" : "").Append(tower.AwaitsCrew ? " awaits crew" : "").Append("; ");
        }
        foreach (var drone in UnityEngine.Object.FindObjectsByType<Game.Drones.Drone>(UnityEngine.FindObjectsSortMode.None))
        {
            text.Append("drone ").Append(drone.Definition.Kind).Append(' ').Append(drone.Activity).Append(drone.WorkPoint.HasValue ? " at " + drone.WorkPoint.Value.ToString("0.0") : "").Append("; ");
        }
        foreach (var label in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsSortMode.None))
        {
            if (label.transform.parent != null && label.transform.parent.name == "GateRebuildBadge") text.Append("badge '").Append(label.text.Replace("\n", " ")).Append("' ").Append(((UnityEngine.RectTransform)label.transform.parent).sizeDelta.ToString("0")).Append("; ");
        }
    }
}
return text.ToString();
