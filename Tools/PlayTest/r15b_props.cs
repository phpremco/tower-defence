// Round 15b: stages a boss prop on the field to eye-check it after the ArtModel root-turn fix. r15b_arg.txt holds
// "<ice|vines> <zoom>": "ice" freezes the first gate in an ice block (IceBlockView, as the Frost Warden's breath does),
// "vines" grows the Rotbloom's overgrowth over the first tower (VinesView). The camera centres on it at that zoom.
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/r15b_arg.txt").Trim().Split(' ');
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
Game.Towers.Tower target = null;
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.InstanceID))
{
    if (tower.Definition.BlocksGroundEnemies == (args[0] == "ice") && !tower.IsWrecked) { target = tower; break; }
}
if (target == null) { return "no " + (args[0] == "ice" ? "gate" : "tower"); }
string made;
if (args[0] == "ice")
{
    var along = context.Level.Road.DirectionAt(context.Level.Grid.WorldToCell(target.Position));
    Game.Bosses.IceBlockView.Create(null, target, along, null, context.Projectiles);
    made = "ice on gate at " + target.Position.ToString("0.0");
}
else
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/RotOvergrowth.asset");
    var vines = Game.Bosses.VinesView.Create(null, art);
    vines.Grow(target.Position, 0.3f);
    made = "vines on " + target.Definition.name + " at " + target.Position.ToString("0.0");
}
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
float size = float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
if (size <= 0f && pan != null) { size = pan.PlayZoom; }
if (pan != null) { pan.enabled = false; }
rig.SetView(target.Position, size);
return made + " zoom " + size.ToString("0.00");
