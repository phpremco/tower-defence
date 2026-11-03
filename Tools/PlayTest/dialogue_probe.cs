// Reports the dialogue box's body text state: font, size, rect, characters laid out and visible.
var view = UnityEngine.Object.FindAnyObjectByType<Game.UI.DialogueView>();
if (view == null)
{
    return "no view";
}

var sb = new System.Text.StringBuilder();
sb.Append("shown ").Append(view.IsShown).Append(" text '").Append(view.ShownText).Append("' ");
foreach (var t in view.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
{
    var r = t.rectTransform.rect;
    sb.Append(" | ").Append(t.name).Append(" font=").Append(t.font == null ? "null" : t.font.name)
      .Append(" size=").Append(t.fontSize.ToString("0.0")).Append(" rect=").Append(r.width.ToString("0")).Append("x").Append(r.height.ToString("0"))
      .Append(" chars=").Append(t.textInfo == null ? -1 : t.textInfo.characterCount).Append(" max=").Append(t.maxVisibleCharacters)
      .Append(" active=").Append(t.isActiveAndEnabled).Append(" col=").Append(t.color.ToString()).Append(" mat=").Append(t.fontSharedMaterial == null ? "null" : t.fontSharedMaterial.shader.name);
}

sb.Append(" | default=").Append(TMPro.TMP_Settings.defaultFontAsset == null ? "null" : TMPro.TMP_Settings.defaultFontAsset.name);
return sb.ToString();
