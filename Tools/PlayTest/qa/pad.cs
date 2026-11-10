// QA round 32: a virtual gamepad ("QAPad") driven in real frames (Play Mode must run unpaused; EditorApplication.Step
// runs no input update). Tools/PlayTest/qa/pad_arg.txt: "<buttons> [frames]" presses the buttons (space separated,
// joined with '+') for [frames] player updates (default 4), then releases them; "stick <left|right> x y [frames]"
// tilts a stick. Buttons: south east west north start select lb rb lt rt l3 r3 up down left right.
// The release is queued by an onBeforeUpdate hook in a later Dynamic update, so the caller only waits ~0.3 s.
var pad = UnityEngine.InputSystem.InputSystem.GetDevice("QAPad") as UnityEngine.InputSystem.Gamepad;
if (pad == null) pad = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>("QAPad");
pad.MakeCurrent();
var a = System.IO.File.ReadAllText("Tools/PlayTest/qa/pad_arg.txt").Trim().Split(' ');
var s = new UnityEngine.InputSystem.LowLevel.GamepadState();
int frames = 4;
if (a[0] == "stick")
{
    var v = new UnityEngine.Vector2(float.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture), float.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture));
    if (a[1] == "left") s.leftStick = v; else s.rightStick = v;
    if (a.Length > 4) frames = int.Parse(a[4]);
}
else
{
    foreach (var n in a[0].Split('+'))
    {
        UnityEngine.InputSystem.LowLevel.GamepadButton b;
        switch (n)
        {
            case "south": b = UnityEngine.InputSystem.LowLevel.GamepadButton.South; break;
            case "east": b = UnityEngine.InputSystem.LowLevel.GamepadButton.East; break;
            case "west": b = UnityEngine.InputSystem.LowLevel.GamepadButton.West; break;
            case "north": b = UnityEngine.InputSystem.LowLevel.GamepadButton.North; break;
            case "start": b = UnityEngine.InputSystem.LowLevel.GamepadButton.Start; break;
            case "select": b = UnityEngine.InputSystem.LowLevel.GamepadButton.Select; break;
            case "lb": b = UnityEngine.InputSystem.LowLevel.GamepadButton.LeftShoulder; break;
            case "rb": b = UnityEngine.InputSystem.LowLevel.GamepadButton.RightShoulder; break;
            case "l3": b = UnityEngine.InputSystem.LowLevel.GamepadButton.LeftStick; break;
            case "r3": b = UnityEngine.InputSystem.LowLevel.GamepadButton.RightStick; break;
            case "up": b = UnityEngine.InputSystem.LowLevel.GamepadButton.DpadUp; break;
            case "down": b = UnityEngine.InputSystem.LowLevel.GamepadButton.DpadDown; break;
            case "left": b = UnityEngine.InputSystem.LowLevel.GamepadButton.DpadLeft; break;
            case "right": b = UnityEngine.InputSystem.LowLevel.GamepadButton.DpadRight; break;
            case "lt": s.leftTrigger = 1f; continue;
            case "rt": s.rightTrigger = 1f; continue;
            default: return "unknown button " + n;
        }
        s = s.WithButton(b, true);
    }
    if (a.Length > 1) frames = int.Parse(a[1]);
}
UnityEditor.EditorApplication.isPaused = false;
UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad, s);
int left = frames;
System.Action hook = null;
hook = () =>
{
    if (UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType != UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic) return;
    if (--left > 0) return;
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad, new UnityEngine.InputSystem.LowLevel.GamepadState());
    UnityEngine.InputSystem.InputSystem.onBeforeUpdate -= hook;
};
UnityEngine.InputSystem.InputSystem.onBeforeUpdate += hook;
return "pressed " + string.Join(" ", a);
