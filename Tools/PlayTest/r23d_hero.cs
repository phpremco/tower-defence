// Round 23d: Spencer's jetpack and weapons in play, one CLI call each (the editor is held paused between calls).
// Tools/PlayTest/r23d_hero.txt holds one command:
//   state                 where he is, the flight, the view's flight phase and weapon switch, the clips playing
//   view <ortho> [lift]   the camera on the hero at that ortho half height (lift: metres above his feet, default 0.8)
//   base <ortho>          the camera on the base at that ortho half height
//   move <post>           sends him to a post
//   draw melee|ranged|none  the view draws or stows that weapon now (as a strike would)
//   face <yaw>            turns the figure to that world yaw (a close-up of the face)
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/r23d_hero.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
UnityEditor.EditorApplication.isPaused = true;
var hero = context.Hero;
var art = hero.GetComponent<Game.Hero.HeroArtView>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var report = new System.Text.StringBuilder();
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
if (args[0] == "view" || args[0] == "base")
{
    var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
    if (pan != null) pan.enabled = false;
    float fit = UnityEngine.Camera.main.orthographicSize * rig.Zoom;
    float lift = args.Length > 2 ? float.Parse(args[2], inv) : 0.8f;
    UnityEngine.Vector3 at = args[0] == "view" ? hero.transform.position + UnityEngine.Vector3.up * lift : context.Base.Position;
    rig.SetView(at, fit / float.Parse(args[1], inv));
    report.Append("view at ").Append(at.ToString("0.0")).Append(' ');
}
else if (args[0] == "flag")
{
    var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
    if (pan != null) pan.enabled = false;
    UnityEngine.Renderer cloth = null;
    foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.SkinnedMeshRenderer>()) if (r.transform.parent != null && r.transform.parent.name.StartsWith("NTI_Flag") || r.name.StartsWith("NTI_Flag")) cloth = r;
    if (cloth == null) return "no flag";
    float fit = UnityEngine.Camera.main.orthographicSize * rig.Zoom;
    rig.SetView(cloth.bounds.center, fit / float.Parse(args[1], inv));
    report.Append("flag at ").Append(cloth.bounds.center.ToString("0.00")).Append(" size ").Append(cloth.bounds.size.ToString("0.00")).Append(' ');
}
else if (args[0] == "fps")
{
    // Fixed game time per stepped frame, so a flight is seen in small steps (0 lets the clock run free again).
    UnityEngine.Time.captureFramerate = int.Parse(args[1]);
}
else if (args[0] == "move")
{
    hero.MoveTo(int.Parse(args[1]));
}
else if (args[0] == "draw" && art != null)
{
    var handType = typeof(Game.Hero.HeroArtView).GetNestedType("Hand", System.Reflection.BindingFlags.NonPublic);
    object hand = System.Enum.Parse(handType, args[1] == "melee" ? "Melee" : args[1] == "ranged" ? "Ranged" : "None");
    typeof(Game.Hero.HeroArtView).GetField("_combatUntil", flags).SetValue(art, UnityEngine.Time.time + 30f);
    var draw = typeof(Game.Hero.HeroArtView).GetMethod("Draw", flags);
    draw.Invoke(art, new object[] { hand, true, 0f });
}
else if (args[0] == "face" && art != null)
{
    var figure = (UnityEngine.Transform)typeof(Game.Hero.HeroArtView).GetField("_figure", flags).GetValue(art);
    figure.rotation = UnityEngine.Quaternion.Euler(0f, float.Parse(args[1], inv), 0f);
}

report.Append("pos ").Append(hero.transform.position.ToString("0.00")).Append(" post ").Append(hero.PostIndex).Append('/').Append(hero.PostCount)
    .Append(" walking ").Append(hero.IsWalking).Append(" flying ").Append(hero.IsFlying).Append(" airborne ").Append(hero.IsAirborne)
    .Append(" touchdown ").Append(hero.FlightSecondsToTouchdown.ToString("0.00"));
if (art != null)
{
    var body = (Game.Core.ArtModel)typeof(Game.Hero.HeroArtView).GetField("_body", flags).GetValue(art);
    var player = body.Player;
    string Name(int i) => i >= 0 ? body.Art.Clips[i].Clip.name : "-";
    var jets = (Game.Hero.JetThrust)typeof(Game.Hero.HeroArtView).GetField("_jets", flags).GetValue(art);
    report.Append(" | view flight ").Append(typeof(Game.Hero.HeroArtView).GetField("_flight", flags).GetValue(art))
        .Append(" switch ").Append(Name((int)typeof(Game.Hero.HeroArtView).GetField("_switchClip", flags).GetValue(art)))
        .Append(" drawn ").Append(typeof(Game.Hero.HeroArtView).GetField("_drawn", flags).GetValue(art))
        .Append(" once ").Append(Name(player.Once)).Append(" upper ").Append(Name(player.Upper))
        .Append(" jets ").Append(jets == null ? "none" : jets.IsLit.ToString());
}

return report.ToString();
