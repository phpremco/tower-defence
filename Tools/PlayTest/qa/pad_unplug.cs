// QA round 33: unplugs the virtual gamepad "QAPad" (as a controller dropping out); "plug" in qa/unplug_arg.txt adds it back.
string mode = System.IO.File.Exists("Tools/PlayTest/qa/unplug_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/qa/unplug_arg.txt").Trim() : "unplug";
var pad = UnityEngine.InputSystem.InputSystem.GetDevice("QAPad");
if (mode == "plug")
{
    if (pad == null) pad = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>("QAPad");
    return "plugged " + pad.deviceId;
}
if (pad == null) return "no pad";
int id = pad.deviceId;
UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);
return "unplugged " + id;
