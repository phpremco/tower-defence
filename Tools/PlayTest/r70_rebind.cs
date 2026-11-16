// Round 70: on the open controls page, "start" presses the first pad cell (Confirm on the gamepad) so the page waits for a
// button; "cancel" ends the wait; "probe" lists the cells. Argument in Tools/PlayTest/r70_rebind_arg.txt.
string arg = System.IO.File.ReadAllText("Tools/PlayTest/r70_rebind_arg.txt").Trim();
var page = UnityEngine.Object.FindAnyObjectByType<Game.UI.ControlsPage>();
if (page == null)
{
    return "no controls page";
}

var bindings = Game.Core.Services.Get<Game.Platform.IInputBindings>();
if (arg == "cancel")
{
    bindings.CancelRebind();
    return "cancelled, rebinding " + bindings.IsRebinding;
}

var buttons = page.GetComponentsInChildren<UnityEngine.UI.Button>();
if (arg == "start")
{
    buttons[0].onClick.Invoke();
    return "pressed the first cell; busy " + page.Busy;
}

var report = new System.Text.StringBuilder();
foreach (var button in buttons)
{
    var image = button.transform.Find("Glyph") != null ? button.transform.Find("Glyph").GetComponent<UnityEngine.UI.Image>() : null;
    var text = button.GetComponentInChildren<UnityEngine.UI.Text>();
    report.Append(image != null && image.enabled && image.sprite != null ? image.sprite.name : "-").Append('/').Append(text != null ? text.text : "").Append(" | ");
}

return "rebinding " + bindings.IsRebinding + ": " + report;
