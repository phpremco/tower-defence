using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Platform;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Game.Tests
{
    /// <summary>
    /// Round 71: no two always-active actions share a default control within a control scheme. The Codex button is outside
    /// that rule on purpose: it only listens while Voss shows a scan, when the clock is held and the speed button does
    /// nothing, so it shares the speed button on the pad. Two actions may also share a control when one takes a tap and the
    /// other a hold of it: calling the next wave (round 73: every player's action) taps the View button, the overview holds it.
    /// </summary>
    public sealed class DefaultBindingTests
    {
        private const string AssetPath = "Assets/_Project/Settings/Input/GameInput.inputactions";
        private const string PadScheme = "Gamepad";
        private const string KeyScheme = "Keyboard&Mouse";

        // The context-only action (Codex); see the summary.
        private static readonly HashSet<string> NotAlwaysActive = new HashSet<string> { "Codex" };

        [TestCase(PadScheme)]
        [TestCase(KeyScheme)]
        public void AlwaysActiveActions_ShareNoDefaultControl(string scheme)
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(AssetPath));
            try
            {
                var holders = new Dictionary<string, (string Action, string Press)>();
                var clashes = new StringBuilder();
                foreach (InputAction action in asset.FindActionMap("Gameplay", true).actions)
                {
                    if (NotAlwaysActive.Contains(action.name))
                    {
                        continue;
                    }

                    foreach (InputBinding binding in action.bindings)
                    {
                        if (binding.isComposite || !InGroup(binding, scheme))
                        {
                            continue;
                        }

                        string path = binding.path.ToLowerInvariant();
                        string press = PressOf(binding);
                        if (holders.TryGetValue(path, out (string Action, string Press) other) && other.Action != action.name && !TapAndHold(other.Press, press))
                        {
                            clashes.AppendLine($"{binding.path}: {other.Action} and {action.name}");
                        }

                        holders[path] = (action.name, press);
                    }
                }

                Assert.IsEmpty(clashes.ToString(), $"Shared default controls in {scheme}:\n{clashes}");
                Assert.Greater(holders.Count, 10, "the scheme's bindings were found");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Codex_SharesOnlyTheSpeedButton_OnThePad()
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(AssetPath));
            var provider = new InputSystemProvider(asset, null, null);
            try
            {
                string codex = provider.ControlPath(GameAction.Codex, InputDeviceKind.Gamepad);
                Assert.AreEqual(provider.ControlPath(GameAction.Speed, InputDeviceKind.Gamepad), codex, "the documented sharing");
                foreach (GameAction action in System.Enum.GetValues(typeof(GameAction)))
                {
                    if (action != GameAction.Codex && action != GameAction.Speed)
                    {
                        Assert.AreNotEqual(codex, provider.ControlPath(action, InputDeviceKind.Gamepad), action.ToString());
                    }
                }

                Assert.AreNotEqual(provider.ControlPath(GameAction.Speed, InputDeviceKind.KeyboardMouse), provider.ControlPath(GameAction.Codex, InputDeviceKind.KeyboardMouse), "the keyboard has room for both");
            }
            finally
            {
                provider.Dispose();
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CallWave_IsBoundForEveryPlayer_OnBothDevices_AndRebindable()
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(AssetPath));
            var provider = new InputSystemProvider(asset, null, null);
            try
            {
                Assert.AreEqual("<Keyboard>/n", provider.ControlPath(GameAction.CallWave, InputDeviceKind.KeyboardMouse));
                Assert.AreEqual("<Gamepad>/select", provider.ControlPath(GameAction.CallWave, InputDeviceKind.Gamepad), "a tap of View; the overview holds it");
                Assert.IsTrue(provider.CanRebind(GameAction.CallWave, InputDeviceKind.KeyboardMouse));
                Assert.IsTrue(provider.CanRebind(GameAction.CallWave, InputDeviceKind.Gamepad));
            }
            finally
            {
                provider.Dispose();
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        // "tap", "hold" or "" (a plain press) from a binding's interactions.
        private static string PressOf(InputBinding binding)
        {
            string interactions = binding.interactions ?? string.Empty;
            return interactions.StartsWith("tap") ? "tap" : interactions.StartsWith("hold") ? "hold" : string.Empty;
        }

        private static bool TapAndHold(string a, string b) => (a == "tap" && b == "hold") || (a == "hold" && b == "tap");

        private static bool InGroup(InputBinding binding, string scheme)
        {
            if (string.IsNullOrEmpty(binding.groups))
            {
                return false;
            }

            foreach (string group in binding.groups.Split(InputBinding.Separator))
            {
                if (group == scheme)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
