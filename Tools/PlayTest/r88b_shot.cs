// Round 88b: a still WITH the HUD laid out for the still's own size. r88_shot.cs draws the HUD at 1920x1080 while the
// world labels (markers, badges) were placed for the batch editor's own 640x480 screen: they land on panels they never
// touch in the game. Here the camera gets its target first, two frames are stepped so every view lays itself out for
// that size, and only then the picture is taken. Also prints the safe area, and every showing world label that touches
// a keep-out zone ("on panel": must be none).
// Tools/PlayTest/r88b_shot_arg.txt: "<width> <height> <png path relative to the project>".
string[] a = System.IO.File.ReadAllText("Tools/PlayTest/r88b_shot_arg.txt").Trim().Split(' ');
int W = int.Parse(a[0]), H = int.Parse(a[1]);
var camera = UnityEngine.Camera.main;
var view = UnityEngine.Object.FindAnyObjectByType<Game.UI.SurprisePingView>();
if (camera == null || view == null) { return "no level"; }
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))
{
    if (canvas.isRootCanvas && canvas.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay) { canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + 0.5f; }
}
var texture = new UnityEngine.RenderTexture(W, H, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = (float)W / H;
UnityEngine.Canvas.ForceUpdateCanvases();
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 2; i++) { UnityEditor.EditorApplication.Step(); }
camera.Render();

var inv = System.Globalization.CultureInfo.InvariantCulture;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var area = (UnityEngine.RectTransform)typeof(Game.UI.SurprisePingView).GetField("_area", flags).GetValue(view);
var keepOut = (Game.UI.HudKeepOut)typeof(Game.UI.SurprisePingView).GetField("_keepOut", flags).GetValue(view);
var sb = new System.Text.StringBuilder();
sb.Append("camera ").Append(camera.pixelWidth).Append('x').Append(camera.pixelHeight).Append(" area ").Append(area.rect.ToString("0")).Append(" pings ").Append(view.Showing).Append(" zones ").Append(keepOut.Zones.Count).Append(" | labels:");
var corners = new UnityEngine.Vector3[4];
var shown = new System.Collections.Generic.List<UnityEngine.Rect>();
int onPanel = 0, outside = 0;
foreach (UnityEngine.Transform child in area)
{
    if (!child.gameObject.activeSelf) { continue; }
    ((UnityEngine.RectTransform)child).GetWorldCorners(corners);
    UnityEngine.Vector2 min = area.InverseTransformPoint(corners[0]), max = min;
    for (int i = 1; i < 4; i++) { UnityEngine.Vector2 c = area.InverseTransformPoint(corners[i]); min = UnityEngine.Vector2.Min(min, c); max = UnityEngine.Vector2.Max(max, c); }
    var rect = UnityEngine.Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    // A label's box is wider than its word: the word is in the middle, so only the middle half counts for a text.
    var text = child.GetComponent<UnityEngine.UI.Text>();
    if (text != null) { float w = UnityEngine.Mathf.Min(rect.width, text.preferredWidth); rect = new UnityEngine.Rect(rect.center.x - w * 0.5f, rect.y, w, rect.height); }
    // A ring marks a place on the field and ripples: it is drawn under the panels and is no label.
    bool ring = child.name.EndsWith("Ring");
    bool hit = false;
    if (!ring) { foreach (UnityEngine.Rect zone in keepOut.Zones) { hit |= rect.Overlaps(zone); } }
    bool off = !ring && (rect.xMin < area.rect.xMin - 0.5f || rect.xMax > area.rect.xMax + 0.5f || rect.yMin < area.rect.yMin - 0.5f || rect.yMax > area.rect.yMax + 0.5f);
    onPanel += hit ? 1 : 0;
    outside += off ? 1 : 0;
    shown.Add(rect);
    sb.Append(' ').Append(child.name).Append(text != null ? "'" + text.text + "'" : "").Append('[').Append(rect.xMin.ToString("0", inv)).Append(',').Append(rect.yMin.ToString("0", inv)).Append(" .. ").Append(rect.xMax.ToString("0", inv)).Append(',').Append(rect.yMax.ToString("0", inv)).Append(']').Append(hit ? "ON PANEL" : "").Append(off ? "OUTSIDE" : "");
}
sb.Append(" | on panel ").Append(onPanel).Append(", outside the safe area ").Append(outside);

camera.targetTexture = null;
camera.aspect = aspect;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(W, H, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, W, H), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(a[2], image.EncodeToPNG());
UnityEngine.Object.Destroy(image);
return "wrote " + a[2] + " " + sb;
