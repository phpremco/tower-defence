// Round 73: reads and drives the call-next-wave state of the running level. Tools/PlayTest/r73_call_arg.txt holds
// "probe" (wave, countdown, why a call is refused, money, the HUD button and its badge, the bonus text), "fast" (the
// developer fast-forward: the rest of the running wave out at once, the next wave starts) or "call" (the HUD button's
// request, as a click). "key <name>" presses and releases a key of the keyboard ("n").
string arg = System.IO.File.ReadAllText("Tools/PlayTest/r73_call_arg.txt").Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no level";
}

if (arg == "fast")
{
    context.FastForwardWaveRequested.Raise();
}
else if (arg == "flush")
{
    // The running wave (started first when none runs) comes out at once without starting the next one, so the countdown runs.
    if (context.WaveNumber == 0)
    {
        context.FastForwardWaveRequested.Raise();
    }

    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    var spawn = typeof(Game.Levels.LevelController).GetMethod("SpawnDueEnemies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var scheduler = typeof(Game.Levels.LevelController).GetField("_scheduler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    if (scheduler.GetValue(controller) != null)
    {
        spawn.Invoke(controller, new object[] { float.MaxValue, true });
    }
}
else if (arg == "call")
{
    context.CallWaveRequested.Raise();
}
else if (arg.StartsWith("tap "))
{
    // "tap <control> <ms>": presses a control of the first gamepad and releases it after that many milliseconds.
    var tapParts = arg.Split(' ');
    var pad = UnityEngine.InputSystem.Gamepad.current;
    var control = (UnityEngine.InputSystem.Controls.ButtonControl)pad[tapParts[1]];
    using (UnityEngine.InputSystem.LowLevel.StateEvent.From(pad, out var down))
    {
        UnityEngine.InputSystem.InputControlExtensions.WriteValueIntoEvent(control, 1f, down);
        UnityEngine.InputSystem.InputSystem.QueueEvent(down);
    }

    UnityEngine.InputSystem.InputSystem.Update();
    System.Threading.Thread.Sleep(int.Parse(tapParts[2]));
    using (UnityEngine.InputSystem.LowLevel.StateEvent.From(pad, out var up))
    {
        UnityEngine.InputSystem.InputControlExtensions.WriteValueIntoEvent(control, 0f, up);
        UnityEngine.InputSystem.InputSystem.QueueEvent(up);
    }

    UnityEngine.InputSystem.InputSystem.Update();
}
else if (arg.StartsWith("key "))
{
    var keyboard = UnityEngine.InputSystem.Keyboard.current;
    var key = (UnityEngine.InputSystem.Controls.ButtonControl)keyboard[arg.Substring(4)];
    using (UnityEngine.InputSystem.LowLevel.StateEvent.From(keyboard, out var down))
    {
        UnityEngine.InputSystem.InputControlExtensions.WriteValueIntoEvent(key, 1f, down);
        UnityEngine.InputSystem.InputSystem.QueueEvent(down);
    }

    UnityEngine.InputSystem.InputSystem.Update();
    using (UnityEngine.InputSystem.LowLevel.StateEvent.From(keyboard, out var up))
    {
        UnityEngine.InputSystem.InputControlExtensions.WriteValueIntoEvent(key, 0f, up);
        UnityEngine.InputSystem.InputSystem.QueueEvent(up);
    }

    UnityEngine.InputSystem.InputSystem.Update();
}

var hud = UnityEngine.Object.FindAnyObjectByType<Game.UI.HudView>();
var button = hud != null ? hud.WaveCallButton : null;
var badge = button != null ? button.Button.transform.Find("Badge").GetComponent<UnityEngine.UI.Image>() : null;
var input = Game.Core.Services.Get<Game.Platform.IInputProvider>();
return string.Format(System.Globalization.CultureInfo.InvariantCulture,
    "{0}: wave {1}/{2}, countdown {3:0.0}, block {4}, money {5}, alive {6}, button {7}, badge {8}, device {9} '{10}', bonus '{11}', outcome {12}",
    arg, context.WaveNumber, context.WaveCount, context.NextWaveCountdown, context.WaveCallBlock, context.Wallet.Money, context.Targets.Count,
    button != null && button.Button.interactable ? "lit" : "dim", badge != null && badge.enabled ? badge.sprite.name : "none",
    input.ActiveDevice, input.PromptFor(Game.Platform.GameAction.CallWave), button != null ? button.BonusText : "", context.Outcome);
