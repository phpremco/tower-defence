// Round 37 (Steam screenshots): frames the play camera and renders a still off-screen (the Game view is left alone).
// Tools/PlayTest/r37_shot_arg.txt: "<focus> <zoom> <hud|clean> <png path> [dx dz]". Focus: "crowd" (the densest knot of
// living creatures), "air" (the densest knot of flyers), "boss" (the continent boss), "hero" (Spencer), "drones" (the
// working drones), "keep" (leave the camera where it is), or "x,z" (a world point). Zoom multiplies the level's play
// zoom (1 = play view, larger = closer). "clean" hides the screen-space interface for the picture only (world-space
// health bars stay); "hud" draws it through the camera like render_still.cs. Renders 3840x2160; the caller downsamples.
var inv = System.Globalization.CultureInfo.InvariantCulture;
var a = System.IO.File.ReadAllText("Tools/PlayTest/r37_shot_arg.txt").Trim().Split(' ');
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig != null ? rig.GetComponent<Game.Levels.CameraPanZoom>() : null;
var camera = UnityEngine.Camera.main;
var enemies = UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None);
var alive = new System.Collections.Generic.List<UnityEngine.Vector3>();
var flyers = new System.Collections.Generic.List<UnityEngine.Vector3>();
foreach (var e in enemies)
{
    if (!e.isActiveAndEnabled || e.Health == null || e.Health.Fraction <= 0f) continue;
    alive.Add(e.transform.position);
    if (e.Definition != null && (e.Definition.Layer & Game.Core.TargetLayers.Air) != 0) flyers.Add(e.transform.position);
}
System.Func<System.Collections.Generic.List<UnityEngine.Vector3>, UnityEngine.Vector3?> densest = list =>
{
    int best = -1, bestCount = 0;
    for (int i = 0; i < list.Count; i++) { int c = 0; foreach (var q in list) if ((q - list[i]).sqrMagnitude < 49f) c++; if (c > bestCount) { bestCount = c; best = i; } }
    if (best < 0) return null;
    var sum = UnityEngine.Vector3.zero; int n = 0;
    foreach (var q in list) if ((q - list[best]).sqrMagnitude < 49f) { sum += q; n++; }
    return sum / n;
};
UnityEngine.Vector3? focus = null;
string mode = a[0];
if (mode == "crowd") focus = densest(alive);
else if (mode == "air") focus = densest(flyers);
else if (mode == "boss") { var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>(); if (brain != null) focus = brain.transform.position; }
else if (mode == "hero") { Game.Core.Services.TryGet(out Game.Levels.LevelContext lc); if (lc != null && lc.Hero != null) focus = lc.Hero.transform.position; }
else if (mode == "drones") { var dl = new System.Collections.Generic.List<UnityEngine.Vector3>(); foreach (var d in UnityEngine.Object.FindObjectsByType<Game.Drones.Drone>(UnityEngine.FindObjectsSortMode.None)) dl.Add(d.transform.position); focus = densest(dl); }
else if (mode.Contains(",")) { var xz = mode.Split(','); focus = new UnityEngine.Vector3(float.Parse(xz[0], inv), 0f, float.Parse(xz[1], inv)); }
if (focus.HasValue && rig != null)
{
    var f = focus.Value;
    if (a.Length > 5) f += new UnityEngine.Vector3(float.Parse(a[4], inv), 0f, float.Parse(a[5], inv));
    if (pan != null) pan.enabled = false;
    float play = pan != null ? pan.PlayZoom : 10f;
    rig.SetView(f, play * float.Parse(a[1], inv));
}
bool hud = a[2] == "hud";
var hidden = new System.Collections.Generic.List<UnityEngine.Canvas>();
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))
{
    if (!canvas.isRootCanvas) continue;
    if (hud && canvas.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay) { canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + 0.5f; }
    if (!hud && canvas.renderMode != UnityEngine.RenderMode.WorldSpace && canvas.enabled) { canvas.enabled = false; hidden.Add(canvas); }
}
UnityEngine.Canvas.ForceUpdateCanvases();
const int W = 3840, H = 2160;
var texture = new UnityEngine.RenderTexture(W, H, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = (float)W / H;
camera.Render();
camera.targetTexture = null;
camera.aspect = aspect;
foreach (var c in hidden) c.enabled = true;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(W, H, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, W, H), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(a[3], image.EncodeToPNG());
UnityEngine.Object.Destroy(image);
int towers = UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None).Length;
return "wrote " + System.IO.Path.GetFileName(a[3]) + " alive " + alive.Count + " air " + flyers.Count + " towers " + towers + (focus.HasValue ? " focus " + focus.Value.ToString("0.0") : " no focus");
