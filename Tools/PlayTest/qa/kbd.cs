// QA round 32: a keyboard press in real frames (Play Mode unpaused): Tools/PlayTest/qa/kbd_arg.txt holds an Input System
// Key name (Enter, Escape, UpArrow, U, Q ...) and an optional frame count; released by an onBeforeUpdate hook.
var a = System.IO.File.ReadAllText("Tools/PlayTest/qa/kbd_arg.txt").Trim().Split(' ');
var key = (UnityEngine.InputSystem.Key)System.Enum.Parse(typeof(UnityEngine.InputSystem.Key), a[0]);
int left = a.Length > 1 ? int.Parse(a[1]) : 4;
var kb = UnityEngine.InputSystem.Keyboard.current;
UnityEditor.EditorApplication.isPaused = false;
bool pressed = false;
System.Action hook = null;
hook = () =>
{
    if (UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType != UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic) return;
    if (!pressed) { pressed = true; UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(key)); return; }
    if (--left > 0) return;
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    UnityEngine.InputSystem.InputSystem.onBeforeUpdate -= hook;
};
UnityEngine.InputSystem.InputSystem.onBeforeUpdate += hook;
return "key " + key;
