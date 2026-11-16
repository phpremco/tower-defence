// Round 72: the credits screen. Tools/PlayTest/r72_credits_arg.txt holds one line:
//   "probe"        open state, scroll position, content and viewport height, number of text blocks, selected object
//   "scroll <v>"   sets the scroll position (1 = top, 0 = bottom)
//   "stick <y>"    holds the left stick of the virtual pad (r70_pad.cs, name R70Pad*) at (0, y); "stick 0" lets go
string[] a = System.IO.File.ReadAllText("Tools/PlayTest/r72_credits_arg.txt").Trim().Split(' ');
var view = UnityEngine.Object.FindAnyObjectByType<Game.UI.CreditsView>();
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (a[0] == "stick")
{
    UnityEngine.InputSystem.Gamepad pad = null;
    foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
    {
        if (device.name.StartsWith("R70Pad") && device is UnityEngine.InputSystem.Gamepad found) pad = found;
    }
    if (pad == null) return "no virtual pad";
    float y = float.Parse(a[1], inv);
    using (UnityEngine.InputSystem.LowLevel.StateEvent.From(pad, out var eventPtr))
    {
        UnityEngine.InputSystem.InputControlExtensions.WriteValueIntoEvent(pad.leftStick, new UnityEngine.Vector2(0f, y), eventPtr);
        UnityEngine.InputSystem.InputSystem.QueueEvent(eventPtr);
    }
    UnityEngine.InputSystem.InputSystem.Update();
    return "stick y " + y + " read " + pad.leftStick.ReadValue();
}
if (view == null) return "no credits view";
if (a[0] == "scroll")
{
    view.ScrollTo(float.Parse(a[1], inv));
    return "scroll " + view.ScrollPosition.ToString("0.000", inv);
}
var scroll = view.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
var selected = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
return "open " + view.IsOpen + ", scroll " + view.ScrollPosition.ToString("0.000", inv)
    + ", content " + scroll.content.rect.height.ToString("0", inv) + ", viewport " + scroll.viewport.rect.height.ToString("0", inv)
    + ", blocks " + scroll.content.childCount + ", selected " + (selected != null ? selected.name + "/" + selected.GetComponentInChildren<UnityEngine.UI.Text>()?.text : "none");
