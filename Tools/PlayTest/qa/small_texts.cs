// QA round 33: every visible UI text (legacy and TextMeshPro) and the size it draws at, as pixels on a 720p screen
// (the canvas scales the 1080p layout by two thirds). Lists the ones under 18 px there, and texts cut off by their box.
var sb = new System.Text.StringBuilder();
int small = 0, cut = 0, total = 0;
const float To720 = 720f / 1080f;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsSortMode.None))
{
    if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text) || t.color.a < 0.05f) continue;
    total++;
    int used = t.resizeTextForBestFit ? t.cachedTextGenerator.fontSizeUsedForBestFit : t.fontSize;
    if (used <= 0) used = t.fontSize;
    float px = used * To720;
    bool clipped = t.verticalOverflow == UnityEngine.VerticalWrapMode.Truncate && t.cachedTextGenerator.characterCountVisible < t.text.Length - CountTags(t.text) - 1;
    if (px < 17.5f) { small++; sb.Append("small ").Append(px.ToString("0.0")).Append("px <").Append(t.text.Replace("\n", "/")).Append("> in ").Append(t.transform.parent != null ? t.transform.parent.name : "-").Append('\n'); }
    if (clipped) { cut++; sb.Append("cut <").Append(t.text.Replace("\n", "/")).Append("> ").Append(t.cachedTextGenerator.characterCountVisible).Append('/').Append(t.text.Length).Append('\n'); }
}
foreach (var t in UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>(UnityEngine.FindObjectsSortMode.None))
{
    if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text) || t.alpha < 0.05f) continue;
    total++;
    float px = t.fontSize * To720;
    if (px < 17.5f) { small++; sb.Append("small(tmp) ").Append(px.ToString("0.0")).Append("px <").Append(t.text.Replace("\n", "/")).Append("> in ").Append(t.transform.parent != null ? t.transform.parent.name : "-").Append('\n'); }
}
return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ": " + total + " texts, " + small + " under 18 px at 720p, " + cut + " cut\n" + sb;
static int CountTags(string s) { int n = 0; bool inTag = false; foreach (char c in s) { if (c == '<') inTag = true; if (inTag) n++; if (c == '>') inTag = false; } return n; }
