// Round 78: sets the HUD's safe margin (percent of the screen on every side, Tools/PlayTest/r78_margin_arg.txt) for this
// play session, as the HUD margin option or a console profile would; the next level's HUD is laid out inside it.
float percent = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r78_margin_arg.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
Game.UI.UiKit.SafeMarginPercent = percent;
return "safe margin " + percent;
