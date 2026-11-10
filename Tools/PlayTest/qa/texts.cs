// QA round 32: every active Text of at least the size in Tools/PlayTest/qa/texts_arg.txt, with its size and screen x/y.
int min = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/qa/texts_arg.txt").Trim());
var sb = new System.Text.StringBuilder();
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsSortMode.None))
{
    if (!t.isActiveAndEnabled || t.fontSize < min || string.IsNullOrEmpty(t.text)) continue;
    var p = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, t.rectTransform.position);
    string s = t.text.Replace("\n", "/"); if (s.Length > 70) s = s.Substring(0, 70);
    sb.Append(t.fontSize).Append(" @").Append(p.x.ToString("0")).Append(",").Append(p.y.ToString("0")).Append(" a=").Append(t.color.a.ToString("0.0")).Append(" '").Append(s).Append("'\n");
}
return sb.ToString();
