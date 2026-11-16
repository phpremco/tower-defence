// Clicks the first active UI button whose label contains the text in Tools/PlayTest/button_arg.txt (case-insensitive),
// and lists every button label it saw, so menus can be walked without a mouse. An exact label wins over a partial one
// (round 69: "Back" also matched "Mute in background").
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/button_arg.txt").Trim().ToLowerInvariant();
var seen = new System.Text.StringBuilder();
UnityEngine.UI.Button hit = null;
UnityEngine.UI.Button exact = null;
foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsSortMode.None))
{
    var text = button.GetComponentInChildren<UnityEngine.UI.Text>();
    string label = text != null ? text.text.Replace("\n", " ") : button.name;
    seen.Append(label).Append(" | ");
    if (exact == null && label.Trim().ToLowerInvariant() == wanted && button.interactable)
    {
        exact = button;
    }

    if (hit == null && wanted.Length > 0 && label.ToLowerInvariant().Contains(wanted) && button.interactable)
    {
        hit = button;
    }
}

hit = exact ?? hit;
if (hit != null)
{
    hit.onClick.Invoke();
    return "clicked '" + wanted + "'  seen: " + seen;
}

return "no button for '" + wanted + "'  seen: " + seen;
