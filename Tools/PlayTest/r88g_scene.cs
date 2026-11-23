// Round 88g: sets up the stills and the play check of the round (the drone strip, the repair focus). Guns are numbered by
// their cell (x, then y), gates and base turrets are not guns. Tools/PlayTest/r88g_scene_arg.txt, one line of words:
//   drones:<Repair|Harvest|Attack>:<count>   buys drones of the kind the level offers, through the real request
//   menu:<gun>  /  closemenu                 opens the tower menu on a gun (as a confirm on it does) / closes it
//   focus:<gun>                              presses the repair-focus toggle of that gun's menu (the menu stays open)
//   focusgate / focusturret                  the same on the first gate / the first base turret
//   upgrade:<gun>  sell:<gun>  wreck:<gun>   the real upgrade and sell requests (money added) / a blow that wrecks it
//   hurt:<gun>:<share>                       the gun keeps this share of its health
//   hurtall:<share>                          every standing gun keeps this share
//   downdrone:<index>                        that drone is shot down (it goes home to charge)
//   hurtdrone:<index>:<share>                that drone keeps this share of its hull
//   cam:<gun>:<zoom>  /  camdrones:<zoom>    the play camera on a gun / on the middle of the drones
//   chip:<index>                             presses that chip of the drone strip (the camera glides to its drone)
//   state                                    guns (health, stars, mark), drones (state, battery, job), the strip's chips, the badges
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c)) return "no context";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var type = typeof(Game.Levels.PlacementController);
var towers = (System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, Game.Towers.Tower>)type.GetField("_towers", flags).GetValue(placement);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig != null ? rig.GetComponent<Game.Levels.CameraPanZoom>() : null;
var cells = new System.Collections.Generic.List<UnityEngine.Vector2Int>(towers.Keys);
cells.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
var guns = new System.Collections.Generic.List<Game.Towers.Tower>();
Game.Towers.Tower gate = null, turret = null;
foreach (var cell in cells)
{
    var tower = towers[cell];
    if (tower == null) continue;
    if (tower.Definition.BlocksGroundEnemies) { if (gate == null) gate = tower; }
    else if (tower.Definition.IsBaseDefence) { if (turret == null) turret = tower; }
    else guns.Add(tower);
}
var drones = new System.Collections.Generic.List<Game.Drones.Drone>(UnityEngine.Object.FindAnyObjectByType<Game.Levels.DroneController>().Drones);
System.Func<string, int, Game.Towers.Tower> gun = (word, part) =>
{
    int index = int.Parse(word.Split(':')[part]);
    return index >= 0 && index < guns.Count ? guns[index] : null;
};
System.Action<Game.Towers.Tower> open = tower =>
{
    c.SelectTower(null);
    c.SelectedPlacedTower = tower;
    c.Mode = Game.Levels.PlacementMode.TowerMenu;
    c.Changed.Raise();
};
var text = new System.Text.StringBuilder();
foreach (string word in System.IO.File.ReadAllText("Tools/PlayTest/r88g_scene_arg.txt").Trim().Split(' '))
{
    if (word.StartsWith("drones:"))
    {
        string[] p = word.Split(':');
        Game.Drones.DroneDefinition definition = null;
        foreach (var offered in c.Level.Drones) if (offered.Kind.ToString() == p[1]) definition = offered;
        if (definition == null) { text.Append("the level offers no ").Append(p[1]).Append(" drone; "); continue; }
        for (int i = 0; i < int.Parse(p[2]); i++) { c.Wallet.Add(definition.Cost); c.DroneRequested.Raise(definition); }
        text.Append(p[1]).Append(" drones out ").Append(c.ActiveDrones(definition)).Append('/').Append(c.MaxDrones(definition)).Append("; ");
    }
    else if (word.StartsWith("menu:")) { var t = gun(word, 1); if (t != null) { open(t); text.Append("menu on gun ").Append(word.Split(':')[1]).Append("; "); } }
    else if (word == "closemenu") { c.SelectedPlacedTower = null; c.Mode = Game.Levels.PlacementMode.Browsing; c.Changed.Raise(); text.Append("menu closed; "); }
    else if (word.StartsWith("focus:") || word == "focusgate" || word == "focusturret")
    {
        var t = word == "focusgate" ? gate : word == "focusturret" ? turret : gun(word, 1);
        if (t == null) { text.Append("nothing to mark; "); continue; }
        open(t);
        c.RepairFocusRequested.Raise();
        text.Append(t.Definition.name).Append(" mark ").Append(t.RepairFocus ? "ON" : "off").Append("; ");
    }
    else if (word.StartsWith("upgrade:")) { var t = gun(word, 1); if (t != null) { open(t); c.Wallet.Add(t.CanUpgrade ? t.NextUpgradeCost : 0); c.UpgradeRequested.Raise(); text.Append("gun upgraded to level ").Append(t.LevelIndex + 1).Append(" health ").Append(t.Health.Max.ToString("0", inv)).Append("; "); } }
    else if (word.StartsWith("sell:")) { var t = gun(word, 1); if (t != null) { open(t); c.SellRequested.Raise(); text.Append("gun sold; "); } }
    else if (word.StartsWith("wreck:")) { var t = gun(word, 1); if (t != null) { t.TakeDamage(1e9f); text.Append("gun wrecked, mark ").Append(t.RepairFocus ? "ON" : "off").Append("; "); } }
    else if (word.StartsWith("hurt:")) { var t = gun(word, 1); if (t != null) { t.TakeDamage((t.Health.Current - t.Health.Max * float.Parse(word.Split(':')[2], inv)) / c.Structures.GunDamageShare); text.Append("gun hurt; "); } }
    else if (word.StartsWith("hurtall:"))
    {
        float keep = float.Parse(word.Split(':')[1], inv);
        foreach (var t in guns) if (!t.IsWrecked) t.TakeDamage((t.Health.Current - t.Health.Max * keep) / c.Structures.GunDamageShare);
        text.Append("guns hurt ").Append(guns.Count).Append("; ");
    }
    else if (word.StartsWith("downdrone:")) { int i = int.Parse(word.Split(':')[1]); if (i < drones.Count) { drones[i].TakeDamage(1e9f); text.Append("drone ").Append(i).Append(" down; "); } }
    else if (word.StartsWith("hurtdrone:")) { int i = int.Parse(word.Split(':')[1]); if (i < drones.Count) { drones[i].TakeDamage(drones[i].Health.Current - drones[i].Health.Max * float.Parse(word.Split(':')[2], inv)); text.Append("drone ").Append(i).Append(" hurt; "); } }
    else if (word.StartsWith("cam:") || word.StartsWith("camdrones:"))
    {
        string[] p = word.Split(':');
        UnityEngine.Vector3 at = UnityEngine.Vector3.zero;
        if (word.StartsWith("cam:")) { var t = gun(word, 1); if (t == null) continue; at = t.Position; }
        else { foreach (var d in drones) at += d.Position / UnityEngine.Mathf.Max(1, drones.Count); }
        if (pan != null) pan.enabled = false;
        rig.SetView(at, (pan != null ? pan.PlayZoom : 10f) * float.Parse(p[p.Length - 1], inv));
        text.Append("camera ").Append(at.ToString("0.0")).Append("; ");
    }
    else if (word.StartsWith("chip:"))
    {
        int i = int.Parse(word.Split(':')[1]), seen = 0;
        foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsSortMode.None))
        {
            if (button.name != "DroneChip") continue;
            if (seen++ == i) { button.onClick.Invoke(); text.Append("chip pressed; "); }
        }
    }
    else if (word == "state")
    {
        text.Append("money ").Append(c.Wallet.Money).Append(" mode ").Append(c.Mode).Append("; ");
        for (int i = 0; i < guns.Count; i++)
        {
            var t = guns[i];
            text.Append("gun").Append(i).Append(' ').Append(t.Definition.name).Append(" L").Append(t.LevelIndex + 1).Append(' ').Append(t.Health.Current.ToString("0", inv)).Append('/').Append(t.Health.Max.ToString("0", inv))
                .Append(t.IsWrecked ? " WRECK" : "").Append(t.RepairFocus ? " MARKED" : "").Append("; ");
        }
        if (gate != null) text.Append("gate ").Append(gate.Health.Current.ToString("0", inv)).Append('/').Append(gate.Health.Max.ToString("0", inv)).Append(gate.RepairFocus ? " MARKED" : "").Append("; ");
        if (turret != null) text.Append("turret ").Append(turret.Health.Current.ToString("0", inv)).Append('/').Append(turret.Health.Max.ToString("0", inv)).Append(turret.IsWrecked ? " WRECK" : "").Append(turret.RepairFocus ? " MARKED" : "").Append("; ");
        for (int i = 0; i < drones.Count; i++)
        {
            var d = drones[i];
            var job = (Game.Combat.IStructure)typeof(Game.Drones.Drone).GetField("_repairTarget", flags).GetValue(d);
            int on = -1;
            for (int g = 0; g < guns.Count; g++) if (ReferenceEquals(job, guns[g])) on = g;
            text.Append("drone").Append(i).Append(' ').Append(d.Definition.Kind).Append(' ').Append(d.State).Append('(').Append(d.Activity).Append(") battery ").Append(d.Battery.ToString("0.00", inv))
                .Append(job == null ? "" : on >= 0 ? " job gun" + on : ReferenceEquals(job, gate) ? " job gate" : ReferenceEquals(job, turret) ? " job turret" : " job other").Append("; ");
        }
        var corners = new UnityEngine.Vector3[4];
        foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsSortMode.None))
        {
            if (button.name != "DroneChip") continue;
            var fill = (UnityEngine.RectTransform)button.transform.Find("Back/Battery");
            var nav = button.navigation;
            text.Append("chip '").Append(button.GetComponentInChildren<UnityEngine.UI.Text>().text).Append("' bar ").Append(fill.anchorMax.x.ToString("0.00", inv))
                .Append(" down>").Append(nav.selectOnDown != null ? nav.selectOnDown.name : "-").Append(" up>").Append(nav.selectOnUp != null ? nav.selectOnUp.name : "-").Append("; ");
        }
        var strip = UnityEngine.Object.FindAnyObjectByType<Game.UI.DroneStatusView>();
        if (strip != null && strip.Panel.gameObject.activeInHierarchy)
        {
            strip.Panel.GetWorldCorners(corners);
            var canvas = strip.Panel.GetComponentInParent<UnityEngine.Canvas>().rootCanvas;
            var root = (UnityEngine.RectTransform)canvas.transform;
            UnityEngine.Vector2 min = root.InverseTransformPoint(corners[0]), max = root.InverseTransformPoint(corners[2]);
            text.Append("strip ").Append(strip.Count).Append(" chips, canvas rect x ").Append(min.x.ToString("0", inv)).Append("..").Append(max.x.ToString("0", inv)).Append(" y ").Append(min.y.ToString("0", inv)).Append("..").Append(max.y.ToString("0", inv))
                .Append(" of ").Append(root.rect.width.ToString("0", inv)).Append('x').Append(root.rect.height.ToString("0", inv)).Append("; ");
        }
        var badges = UnityEngine.Object.FindAnyObjectByType<Game.UI.RepairFocusBadges>();
        if (badges != null) text.Append("focus badges showing ").Append(badges.Showing).Append("; ");
        var menu = UnityEngine.Object.FindAnyObjectByType<Game.UI.TowerMenuView>();
        if (menu != null && menu.Panel.gameObject.activeSelf)
        {
            text.Append("menu icons:");
            foreach (var button in menu.Panel.GetComponentsInChildren<UnityEngine.UI.Button>(false)) text.Append(' ').Append(((UnityEngine.RectTransform)button.transform).anchoredPosition.x.ToString("0", inv)).Append(button.interactable ? "" : "(off)");
            foreach (var label in menu.Panel.GetComponentsInChildren<UnityEngine.UI.Text>(false)) if (label.text.Contains("focus")) text.Append(" label '").Append(label.text.Replace("\n", " ")).Append("'");
            text.Append("; ");
        }
    }
}
return text.ToString();
