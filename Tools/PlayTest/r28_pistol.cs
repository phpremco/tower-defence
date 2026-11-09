// Round 28: Spencer's pistol light strip. Tools/PlayTest/r28_pistol.txt holds "report", "on" or "off" (off hides the strip
// and resets the pistol to its art scale, for an A/B still; on restores both). Reports the strip's state, its length on
// screen in pixels at the current camera, and the pistol's scale.
string arg = System.IO.File.ReadAllText("Tools/PlayTest/r28_pistol.txt").Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var view = context.Hero.GetComponent<Game.Hero.HeroArtView>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var glow = (Game.Hero.PistolGlow)typeof(Game.Hero.HeroArtView).GetField("_pistolGlow", flags).GetValue(view);
var pistol = (Game.Core.ArtModel)typeof(Game.Hero.HeroArtView).GetField("_pistol", flags).GetValue(view);
var art = (Game.Hero.HeroArtDefinition)typeof(Game.Hero.HeroArtView).GetField("_art", flags).GetValue(view);
if (glow == null || pistol == null) return "no pistol glow (glow " + (glow != null) + ", pistol " + (pistol != null) + ")";
var strip = (UnityEngine.MeshRenderer)typeof(Game.Hero.PistolGlow).GetField("_strip", flags).GetValue(glow);
float baseScale = art.Pistol.Scale;
if (arg == "off") { strip.enabled = false; pistol.Root.localScale = UnityEngine.Vector3.one * baseScale; }
if (arg == "on") { strip.enabled = true; pistol.Root.localScale = UnityEngine.Vector3.one * baseScale * art.PistolScale; }
var cam = UnityEngine.Camera.main;
var t = strip.transform;
var a = cam.WorldToScreenPoint(t.position - t.right * t.lossyScale.x * 0.5f);
var b = cam.WorldToScreenPoint(t.position + t.right * t.lossyScale.x * 0.5f);
return "strip active " + strip.gameObject.activeInHierarchy + " enabled " + strip.enabled + " at " + t.position.ToString("0.00") + " length " + t.lossyScale.x.ToString("0.000")
    + " m, on screen " + UnityEngine.Vector2.Distance(a, b).ToString("0") + " px of " + cam.pixelWidth + ", pistol scale " + pistol.Root.localScale.x.ToString("0.00")
    + ", ortho " + cam.orthographicSize.ToString("0.0");
