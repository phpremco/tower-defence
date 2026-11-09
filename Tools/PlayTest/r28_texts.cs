// Round 28: the display-font fit check after the switch to Chakra Petch. For every active UI Text of 28 px and up (the
// display font): does it leave its box or the screen now (as r23c_texts.cs), and how much room it has for a translation.
// Dutch runs about 25 % longer than English; a text that only fits as one line (overflow, no wrap) needs that headroom
// as plain width, a wrapping best-fit text (UiKit.WrapInBox) shrinks instead, so for those the check is the size it
// would draw at with 25 % more copy. Writes the report to Tools/PlayTest/r28_texts.txt and returns its summary.
const float Dutch = 1.25f;
var sb = new System.Text.StringBuilder();
int checkedCount = 0, risky = 0, outCount = 0;
foreach (var text in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsSortMode.None))
{
    if (!text.isActiveAndEnabled || text.fontSize < 28 || string.IsNullOrEmpty(text.text)) { continue; }
    checkedCount++;
    var rect = text.rectTransform;
    float boxWidth = rect.rect.width;
    float boxHeight = rect.rect.height;
    var settings = text.GetGenerationSettings(new UnityEngine.Vector2(boxWidth, boxHeight));
    var generator = new UnityEngine.TextGenerator();
    string label = text.text.Replace("\n", "/");
    label = label.Length > 50 ? label.Substring(0, 50) : label;
    string font = text.font != null ? text.font.name : "none";
    bool wraps = text.horizontalOverflow == UnityEngine.HorizontalWrapMode.Wrap;
    var corners = new UnityEngine.Vector3[4];
    rect.GetWorldCorners(corners);
    UnityEngine.Camera cam = text.canvas != null && text.canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? text.canvas.worldCamera : null;
    var left = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
    var right = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
    bool outScreen = left.x < -1f || right.x > UnityEngine.Screen.width + 1f;
    if (!wraps)
    {
        // One line: its natural width against the box (a box of 0 is a layout-sized label; then the screen is the limit).
        float width = generator.GetPreferredWidth(text.text, settings) / settings.scaleFactor;
        float room = boxWidth > 1f ? boxWidth / UnityEngine.Mathf.Max(1f, width) : 99f;
        bool outBox = boxWidth > 1f && room < 1f;
        bool tight = room < Dutch;
        if (outBox || outScreen) outCount++;
        if (tight) risky++;
        sb.Append(outBox ? "OUT-BOX " : outScreen ? "OUT-SCREEN " : tight ? "TIGHT " : "ok ").Append('<').Append(label).Append("> ")
          .Append(font).Append(' ').Append(text.fontSize).Append(" px, line ").Append(width.ToString("0")).Append(" box ").Append(boxWidth.ToString("0"))
          .Append(" room x").Append(room.ToString("0.00")).AppendLine();
        continue;
    }

    // Wrapping best-fit: the size it draws at now and with a Dutch-length copy (the text repeated to 125 %).
    generator.Populate(text.text, settings);
    int now = text.resizeTextForBestFit ? generator.fontSizeUsedForBestFit : text.fontSize;
    string longer = text.text + " " + text.text.Substring(0, UnityEngine.Mathf.Max(1, (int)(text.text.Length * (Dutch - 1f))));
    generator.Populate(longer, settings);
    int dutch = text.resizeTextForBestFit ? generator.fontSizeUsedForBestFit : text.fontSize;
    bool clipped = generator.characterCountVisible < longer.Replace("\n", "").Length - 1;
    if (outScreen) outCount++;
    if (clipped) risky++;
    sb.Append(outScreen ? "OUT-SCREEN " : clipped ? "CLIPS-AT-125% " : "ok ").Append('<').Append(label).Append("> ")
      .Append(font).Append(' ').Append(text.fontSize).Append(" px wraps, draws ").Append(now).Append(", at 125% ").Append(dutch).AppendLine();
}

string summary = "checked " + checkedCount + ", out " + outCount + ", tight/clipping at 125% " + risky;
System.IO.File.AppendAllText("Tools/PlayTest/r28_texts.txt", "== " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " " + summary + "\n" + sb + "\n");
return summary;
