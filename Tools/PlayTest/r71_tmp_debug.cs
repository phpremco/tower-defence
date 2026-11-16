// Round 71: what the dialogue body holds (text, sprite asset, sprite count, parsed characters), to check inline glyphs.
var view = UnityEngine.Object.FindAnyObjectByType<Game.UI.DialogueView>();
if (view == null) { return "no view"; }
var sb = new System.Text.StringBuilder();
foreach (var t in view.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
{
    if (t.name != "Body") { continue; }
    t.ForceMeshUpdate();
    sb.Append("text=[" + t.text + "] asset=" + (t.spriteAsset != null ? t.spriteAsset.name + " chars " + t.spriteAsset.spriteCharacterTable.Count + " mat " + (t.spriteAsset.material != null ? t.spriteAsset.material.shader.name : "null") + " sheet " + (t.spriteAsset.spriteSheet != null) : "null") + " rich=" + t.richText + " count=" + t.textInfo.characterCount);
    for (int i = 0; i < t.textInfo.characterCount; i++)
    {
        if (t.textInfo.characterInfo[i].elementType == TMPro.TMP_TextElementType.Sprite) { sb.Append(" sprite@" + i); }
    }
}
return sb.ToString();
