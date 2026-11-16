using System;
using System.IO;
using Game.Platform;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Game.Tests
{
    /// <summary>
    /// Round 70: rebinding on a copy of the game's action asset. Overrides go out as JSON and come back on a fresh asset;
    /// a control taken from another action swaps; bad or unbinding text falls back to the defaults.
    /// </summary>
    public sealed class RebindingTests
    {
        private const string AssetPath = "Assets/_Project/Settings/Input/GameInput.inputactions";

        private InputActionAsset _asset;
        private InputSystemProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _asset = LoadCopy();
            _provider = new InputSystemProvider(_asset, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            _provider.Dispose();
            UnityEngine.Object.DestroyImmediate(_asset);
        }

        [Test]
        public void Defaults_HaveNoOverrides_AndBindConfirmToSouthAndEnter()
        {
            Assert.AreEqual(string.Empty, _provider.SaveOverrides());
            Assert.AreEqual("<Gamepad>/buttonSouth", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad));
            Assert.AreEqual("<Keyboard>/enter", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.KeyboardMouse));
        }

        [Test]
        public void Assign_MovesTheAction_AndSurvivesSaveAndLoadOnAFreshAsset()
        {
            int changes = 0;
            _provider.BindingsChanged += () => changes++;
            Assert.IsTrue(_provider.Assign(GameAction.HeroPost, InputDeviceKind.KeyboardMouse, "<Keyboard>/k"));
            Assert.AreEqual("<Keyboard>/k", _provider.ControlPath(GameAction.HeroPost, InputDeviceKind.KeyboardMouse));
            Assert.AreEqual("K", _provider.PromptFor(GameAction.HeroPost, InputDeviceKind.KeyboardMouse).ToUpperInvariant(), "the prompt cache follows the rebind");
            Assert.AreEqual(1, changes);
            string json = _provider.SaveOverrides();
            StringAssert.Contains("<Keyboard>/k", json);

            InputActionAsset fresh = LoadCopy();
            var other = new InputSystemProvider(fresh, null, null);
            try
            {
                Assert.AreEqual("<Keyboard>/h", other.ControlPath(GameAction.HeroPost, InputDeviceKind.KeyboardMouse));
                Assert.IsTrue(other.LoadOverrides(json));
                Assert.AreEqual("<Keyboard>/k", other.ControlPath(GameAction.HeroPost, InputDeviceKind.KeyboardMouse));
            }
            finally
            {
                other.Dispose();
                UnityEngine.Object.DestroyImmediate(fresh);
            }
        }

        [Test]
        public void TakingAnotherActionsControl_SwapsTheOldControlToIt()
        {
            // Hero post (pad X) onto Confirm's south button: Confirm takes X, nothing is left unbound.
            Assert.IsTrue(_provider.Assign(GameAction.HeroPost, InputDeviceKind.Gamepad, "<Gamepad>/buttonSouth"));
            Assert.AreEqual("<Gamepad>/buttonSouth", _provider.ControlPath(GameAction.HeroPost, InputDeviceKind.Gamepad));
            Assert.AreEqual("<Gamepad>/buttonWest", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad));
            Assert.AreEqual("<Keyboard>/enter", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.KeyboardMouse), "the keyboard side is untouched");
        }

        [Test]
        public void Assign_RefusesEmptyUnchangedAndMissingBindings()
        {
            Assert.IsFalse(_provider.Assign(GameAction.Confirm, InputDeviceKind.Gamepad, string.Empty));
            Assert.IsFalse(_provider.Assign(GameAction.Confirm, InputDeviceKind.Gamepad, "<Gamepad>/buttonSouth"));
            Assert.IsFalse(_provider.CanRebind(GameAction.Upgrade, InputDeviceKind.Gamepad), "upgrade has no pad binding to move");
            Assert.IsFalse(_provider.Assign(GameAction.Upgrade, InputDeviceKind.Gamepad, "<Gamepad>/buttonNorth"));
            Assert.AreEqual(string.Empty, _provider.SaveOverrides());
        }

        [Test]
        public void Reset_DropsEveryOverride()
        {
            _provider.Assign(GameAction.Pause, InputDeviceKind.KeyboardMouse, "<Keyboard>/o");
            _provider.ResetBindings();
            Assert.AreEqual("<Keyboard>/p", _provider.ControlPath(GameAction.Pause, InputDeviceKind.KeyboardMouse));
            Assert.AreEqual(string.Empty, _provider.SaveOverrides());
        }

        [Test]
        public void BrokenText_FallsBackToTheDefaults()
        {
            LogAssertIgnore();
            Assert.IsFalse(_provider.LoadOverrides("{ this is not json"));
            Assert.AreEqual("<Gamepad>/buttonSouth", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad));
            Assert.IsTrue(_provider.LoadOverrides(string.Empty));
        }

        [Test]
        public void TextThatUnbindsAnEssentialAction_FallsBackToTheDefaults()
        {
            InputAction confirm = _asset.FindAction("Gameplay/Confirm", true);
            int binding = IndexOf(confirm, "<Gamepad>/buttonSouth");
            confirm.ApplyBindingOverride(binding, string.Empty);
            string json = _asset.SaveBindingOverridesAsJson();
            _asset.RemoveAllBindingOverrides();

            LogAssertIgnore();
            Assert.IsFalse(_provider.LoadOverrides(json));
            Assert.AreEqual("<Gamepad>/buttonSouth", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad));
            Assert.IsTrue(InputSystemProvider.IsEssential(GameAction.Confirm));
            Assert.IsTrue(InputSystemProvider.IsEssential(GameAction.Cancel));
            Assert.IsTrue(InputSystemProvider.IsEssential(GameAction.Pause));
            Assert.IsFalse(InputSystemProvider.IsEssential(GameAction.Speed));
        }

        [Test]
        public void TheAsset_HasAGamepadAndAKeyboardScheme_AndEveryPlainBindingAGroup()
        {
            Assert.IsTrue(_asset.FindControlSchemeIndex("Gamepad") >= 0);
            Assert.IsTrue(_asset.FindControlSchemeIndex("Keyboard&Mouse") >= 0);
            foreach (InputBinding binding in _asset.bindings)
            {
                if (!binding.isComposite)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(binding.groups), $"binding {binding.path} has no scheme group");
                }
            }
        }

        [Test]
        public void EveryAction_HasAControlOnBothDevices_OrIsAKeyboardOnlyTowerKey()
        {
            foreach (GameAction action in (GameAction[])Enum.GetValues(typeof(GameAction)))
            {
                Assert.IsNotEmpty(_provider.ControlPath(action, InputDeviceKind.KeyboardMouse), $"{action} has no key");
                bool keyboardOnly = action == GameAction.Upgrade || action == GameAction.Repair || action == GameAction.Sell;
                Assert.AreEqual(!keyboardOnly, _provider.ControlPath(action, InputDeviceKind.Gamepad).Length > 0, $"{action} pad binding");
            }
        }

        private static InputActionAsset LoadCopy()
        {
            return InputActionAsset.FromJson(File.ReadAllText(AssetPath));
        }

        private static int IndexOf(InputAction action, string path)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].path == path)
                {
                    return i;
                }
            }

            return -1;
        }

        // The fallback warns on purpose; the test checks the outcome, not the log line.
        private static void LogAssertIgnore() => UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
    }
}
