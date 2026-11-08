// Round 23c: lists every active UI Text of 28 px and up (the display font) whose drawn lines leave its own box or the
// screen, with its font size as drawn, its box and its screen extent; then a count of all display texts checked.
var sb = new System.Text.StringBuilder();
int checkedCount = 0;
UnityEditor.PlayModeWindow.GetRenderingResolution(out uint gw, out uint gh);
sb.Append("screen ").Append(UnityEngine.Screen.width).Append('x').Append(UnityEngine.Screen.height).Append(" gameview ").Append(gw).Append('x').Append(gh).Append(" | ");
foreach (var text in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsSortMode.None))
{
    if (!text.isActiveAndEnabled || text.fontSize < 28 || string.IsNullOrEmpty(text.text)) { continue; }
    checkedCount++;
    var rect = text.rectTransform;
    var gen = text.cachedTextGenerator;
    float scale = text.canvas != null ? text.canvas.scaleFactor : 1f;
    // The drawn extent of the glyphs, in the canvas's pixels.
    float minX = float.MaxValue, maxX = float.MinValue;
    var verts = gen.verts;
    for (int i = 0; i < verts.Count; i++) { minX = UnityEngine.Mathf.Min(minX, verts[i].position.x); maxX = UnityEngine.Mathf.Max(maxX, verts[i].position.x); }
    var corners = new UnityEngine.Vector3[4];
    rect.GetWorldCorners(corners);
    UnityEngine.Camera cam = text.canvas != null && text.canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? text.canvas.worldCamera : null;
    var left = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
    var right = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
    float boxWidth = rect.rect.width * scale;
    float drawn = verts.Count > 0 ? (maxX - minX) : 0f;
    bool outBox = drawn > boxWidth + 2f;
    bool outScreen = left.x < -1f || right.x > UnityEngine.Screen.width + 1f;
    int lines = gen.lineCount;
    bool truncated = gen.characterCountVisible < text.text.Replace("\n", "").Length - 1;
    if (outBox || outScreen || truncated || text.text.Length > 40)
    {
        string label = text.text.Replace("\n", "/");
        sb.Append(outBox ? "OUT-BOX " : "").Append(outScreen ? "OUT-SCREEN " : "").Append(truncated ? "TRUNC " : "")
          .Append('<').Append(label.Length > 60 ? label.Substring(0, 60) : label).Append("> size ").Append(text.fontSize)
          .Append(" drawn ").Append(gen.fontSizeUsedForBestFit).Append(" lines ").Append(lines)
          .Append(" box ").Append(boxWidth.ToString("0")).Append(" glyphs ").Append(drawn.ToString("0"))
          .Append(" screenX ").Append(left.x.ToString("0")).Append("..").Append(right.x.ToString("0")).Append(" | ");
    }
}
return sb.Append("checked ").Append(checkedCount).ToString();
