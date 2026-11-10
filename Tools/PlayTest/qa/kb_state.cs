var k = UnityEngine.InputSystem.Keyboard.current; var m = UnityEngine.InputSystem.Mouse.current;
return "keyboard enabled=" + (k != null && k.enabled) + " bg=" + (k != null && k.canRunInBackground) + " mouse enabled=" + (m != null && m.enabled) + " appFocus=" + UnityEngine.Application.isFocused + " bgBehavior=" + UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
