// QA round 33: sets the HUD margin for this session only (the save is not written): Tools/PlayTest/qa/margin_arg.txt holds the percent.
float percent = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/qa/margin_arg.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
Game.UI.UiKit.SafeMarginPercent = percent;
return "margin " + percent;
