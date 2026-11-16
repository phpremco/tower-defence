// Round 70: a virtual pad of the layout in Tools/PlayTest/r70_pad_arg.txt ("DualSenseGamepadHID", "XInputControllerWindows",
// "SwitchProControllerHID") is added and its right stick clicked once, so the prompts switch to that pad family; "remove"
// takes the virtual pads away again. The click is a control no menu listens to. The unfocused editor may drop queued
// events from a new device (the Input System disables devices in the background), so when the press does not arrive the
// provider is told about the pad directly, the same call a press makes. Reports which way it went.
string arg = System.IO.File.ReadAllText("Tools/PlayTest/r70_pad_arg.txt").Trim();
if (arg == "remove")
{
    int removed = 0;
    foreach (var device in UnityEngine.InputSystem.InputSystem.devices.ToArray())
    {
        if (device.name.StartsWith("R70Pad"))
        {
            UnityEngine.InputSystem.InputSystem.RemoveDevice(device);
            removed++;
        }
    }

    return "removed " + removed;
}

var pad = (UnityEngine.InputSystem.Gamepad)UnityEngine.InputSystem.InputSystem.AddDevice(arg, "R70Pad" + arg);
var bindings = Game.Core.Services.Get<Game.Platform.IInputBindings>();
var input = Game.Core.Services.Get<Game.Platform.IInputProvider>();
using (UnityEngine.InputSystem.LowLevel.StateEvent.From(pad, out var eventPtr))
{
    UnityEngine.InputSystem.InputControlExtensions.WriteValueIntoEvent(pad.rightStickButton, 1f, eventPtr);
    UnityEngine.InputSystem.InputSystem.QueueEvent(eventPtr);
}

UnityEngine.InputSystem.InputSystem.Update();
using (UnityEngine.InputSystem.LowLevel.StateEvent.From(pad, out var eventPtr))
{
    UnityEngine.InputSystem.InputControlExtensions.WriteValueIntoEvent(pad.rightStickButton, 0f, eventPtr);
    UnityEngine.InputSystem.InputSystem.QueueEvent(eventPtr);
}

UnityEngine.InputSystem.InputSystem.Update();
string way = "via press";
if (input.ActiveDevice != Game.Platform.InputDeviceKind.Gamepad)
{
    var track = typeof(Game.Platform.InputSystemProvider).GetMethod("Track", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    track.Invoke(input, new object[] { pad });
    way = "via Track (press not delivered to the unfocused editor)";
}

return "pad " + pad.layout + " " + way + " -> layout " + bindings.ActivePadLayout + ", device " + input.ActiveDevice + ", confirm prompt " + input.PromptFor(Game.Platform.GameAction.Confirm);
