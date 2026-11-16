// Round 70: presses and releases one control of the virtual pad added by r70_pad.cs; the control name ("buttonNorth") is in
// Tools/PlayTest/r70_press_arg.txt. Reports where Confirm and Speed sit on the pad afterwards (a rebind check).
string control = System.IO.File.ReadAllText("Tools/PlayTest/r70_press_arg.txt").Trim();
UnityEngine.InputSystem.Gamepad pad = null;
foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
{
    if (device.name.StartsWith("R70Pad") && device is UnityEngine.InputSystem.Gamepad found)
    {
        pad = found;
    }
}

if (pad == null)
{
    return "no virtual pad";
}

var button = (UnityEngine.InputSystem.Controls.ButtonControl)pad[control];
using (UnityEngine.InputSystem.LowLevel.StateEvent.From(pad, out var eventPtr))
{
    UnityEngine.InputSystem.InputControlExtensions.WriteValueIntoEvent(button, 1f, eventPtr);
    UnityEngine.InputSystem.InputSystem.QueueEvent(eventPtr);
}

UnityEngine.InputSystem.InputSystem.Update();
System.Threading.Thread.Sleep(200);
UnityEngine.InputSystem.InputSystem.Update();
using (UnityEngine.InputSystem.LowLevel.StateEvent.From(pad, out var eventPtr))
{
    UnityEngine.InputSystem.InputControlExtensions.WriteValueIntoEvent(button, 0f, eventPtr);
    UnityEngine.InputSystem.InputSystem.QueueEvent(eventPtr);
}

UnityEngine.InputSystem.InputSystem.Update();
var bindings = Game.Core.Services.Get<Game.Platform.IInputBindings>();
return "pressed " + control + "; rebinding " + bindings.IsRebinding
    + ", confirm " + bindings.ControlPath(Game.Platform.GameAction.Confirm, Game.Platform.InputDeviceKind.Gamepad)
    + ", speed " + bindings.ControlPath(Game.Platform.GameAction.Speed, Game.Platform.InputDeviceKind.Gamepad)
    + ", saved overrides: " + bindings.SaveOverrides().Length + " chars";
