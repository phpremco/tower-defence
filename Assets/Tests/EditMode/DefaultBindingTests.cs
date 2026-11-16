using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Platform;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Game.Tests
{
    /// <summary>
    /// Round 71: no two always-active actions share a default control within a control scheme. Two kinds of action are
    /// outside that rule on purpose: the Codex button only listens while Voss shows a scan, when the clock is held and the
    /// speed button does nothing, so it shares the speed button on the pad; skip wave is a developer action that only
    /// fires in developer mode (it shares the View button with the held overview, told apart by tap and hold).
    /// </summary>
    public sealed class DefaultBindingTests
    {
        private const string AssetPath = "Assets/_Project/Settings/Input/GameInput.inputactions";
        private const string PadScheme = "Gamepad";
        private const string KeyScheme = "Keyboard&Mouse";

        // Context-only (Codex) and developer-only (SkipWave) actions; see the summary.
        private static readonly HashSet<string> NotAlwaysActive = new HashSet<string> { "Codex", "SkipWave" };

        [TestCase(PadScheme)]
        [TestCase(KeyScheme)]
        public void AlwaysActiveActions_ShareNoDefaultControl(string scheme)
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(AssetPath));
            try
            {
                var holders = new Dictionary<string, string>();
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
                        if (holders.TryGetValue(path, out string other) && other != action.name)
                        {
                            clashes.AppendLine($"{binding.path}: {other} and {action.name}");
                        }

                        holders[path] = action.name;
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
        public void SkipWave_IsADeveloperActionOnly()
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(AssetPath));
            var provider = new InputSystemProvider(asset, null, null);
            try
            {
                Assert.IsFalse(provider.DeveloperActions, "off until the bootstrapper finds developer mode");
            }
            finally
            {
                provider.Dispose();
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

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
