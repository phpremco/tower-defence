// Round 65: a still of the play camera, rendered off-screen (the Game view is left alone).
// Tools/PlayTest/r65_shot_arg.txt: first line "<focus> <zoom> <hud|clean> [width height]", second line the png path (it may
// hold spaces). Focus: "cell:<x>:<y>" (a grid cell, fractions allowed) or "keep" (the camera as it is). Zoom multiplies the
// play zoom (1 = as played, larger = closer).
var inv = System.Globalization.CultureInfo.InvariantCulture;
var argLines = System.IO.File.ReadAllLines("Tools/PlayTest/r65_shot_arg.txt");
var a = argLines[0].Trim().Split(' ');
string png = argLines[1].Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext lc)) return "no context";
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig != null ? rig.GetComponent<Game.Levels.CameraPanZoom>() : null;
var camera = UnityEngine.Camera.main;
string mode = a[0];
if (mode.StartsWith("cell:") && rig != null)
{
    var p = mode.Split(':');
    float cx = float.Parse(p[1], inv), cy = float.Parse(p[2], inv);
    UnityEngine.Vector3 a0 = lc.Level.Grid.CellToWorld(new UnityEngine.Vector2Int(0, 0));
    var f = new UnityEngine.Vector3(a0.x + cx * lc.Level.CellSize, 0f, a0.z + cy * lc.Level.CellSize);
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
int W = a.Length > 4 ? int.Parse(a[3]) : 1920, H = a.Length > 4 ? int.Parse(a[4]) : 1080;
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
System.IO.File.WriteAllBytes(png, image.EncodeToPNG());
UnityEngine.Object.Destroy(image);
return "wrote " + System.IO.Path.GetFileName(png);
