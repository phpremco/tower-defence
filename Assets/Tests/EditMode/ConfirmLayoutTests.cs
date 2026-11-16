using System.IO;
using Game.Core;
using Game.Platform;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Game.Tests
{
    /// <summary>
    /// Round 71: on a Nintendo layout A (east) confirms and B (south) goes back, in the game's actions and in menu
    /// navigation; Xbox and PlayStation keep south/east. The swap belongs to the layout: it is never saved, and the
    /// player's own pad rebinding of confirm or cancel always wins.
    /// </summary>
    public sealed class ConfirmLayoutTests
    {
        private const string AssetPath = "Assets/_Project/Settings/Input/GameInput.inputactions";

        private InputActionAsset _asset;
        private InputSystemProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _asset = LoadCopy();
            _provider = new InputSystemProvider(_asset, null, null);
            _provider.UsePadLayout(PadLayout.Xbox);
        }

        [TearDown]
        public void TearDown()
        {
            _provider.Dispose();
            Object.DestroyImmediate(_asset);
        }

        [TestCase(PadLayout.Xbox, "<Gamepad>/buttonSouth", "<Gamepad>/buttonEast")]
        [TestCase(PadLayout.PlayStation, "<Gamepad>/buttonSouth", "<Gamepad>/buttonEast")]
        [TestCase(PadLayout.Nintendo, "<Gamepad>/buttonEast", "<Gamepad>/buttonSouth")]
        public void ConfirmAndCancel_FollowTheLayoutConvention(PadLayout layout, string confirm, string cancel)
        {
            _provider.UsePadLayout(layout);
            Assert.AreEqual(confirm, _provider.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad));
            Assert.AreEqual(cancel, _provider.ControlPath(GameAction.Cancel, InputDeviceKind.Gamepad));
            Assert.AreEqual(confirm, PadLayoutRules.ConfirmPath(layout));
            Assert.AreEqual(cancel, PadLayoutRules.CancelPath(layout));
            Assert.AreEqual("<Keyboard>/enter", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.KeyboardMouse), "keys never swap");
            Assert.AreEqual("<Keyboard>/escape", _provider.ControlPath(GameAction.Cancel, InputDeviceKind.KeyboardMouse));
        }

        [Test]
        public void NintendoSwap_IsNeverSaved_AndLeavesWithThePad()
        {
            _provider.UsePadLayout(PadLayout.Nintendo);
            Assert.IsTrue(_provider.ConfirmSwapped);
            Assert.AreEqual(string.Empty, _provider.SaveOverrides(), "the layout's pair is no player override");
            Assert.AreEqual("<Gamepad>/buttonEast", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad), "saving keeps the swap on");

            _provider.UsePadLayout(PadLayout.Xbox);
            Assert.IsFalse(_provider.ConfirmSwapped);
            Assert.AreEqual("<Gamepad>/buttonSouth", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad));
        }

        [Test]
        public void NintendoGlyphs_ShowAOnConfirm()
        {
            _provider.UsePadLayout(PadLayout.Nintendo);
            GlyphSpec confirm = GlyphCatalog.Describe(GlyphFamily.Nintendo, _provider.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad), string.Empty);
            GlyphSpec cancel = GlyphCatalog.Describe(GlyphFamily.Nintendo, _provider.ControlPath(GameAction.Cancel, InputDeviceKind.Gamepad), string.Empty);
            Assert.AreEqual("A", confirm.Text);
            Assert.AreEqual("B", cancel.Text);
        }

        [Test]
        public void UnrelatedRebind_KeepsTheSwapTheLayouts()
        {
            _provider.UsePadLayout(PadLayout.Nintendo);
            Assert.IsTrue(_provider.Assign(GameAction.HeroPost, InputDeviceKind.Gamepad, "<Gamepad>/leftShoulder"));
            Assert.IsTrue(_provider.ConfirmSwapped);
            string json = _provider.SaveOverrides();
            StringAssert.Contains("leftShoulder", json);
            StringAssert.DoesNotContain("buttonEast", json, "confirm's layout override stays out of the file");
        }

        [Test]
        public void RebindingConfirmOnTheNintendoPad_MakesThePairThePlayers()
        {
            _provider.UsePadLayout(PadLayout.Nintendo);
            Assert.IsTrue(_provider.Assign(GameAction.Confirm, InputDeviceKind.Gamepad, "<Gamepad>/buttonWest"));
            Assert.IsFalse(_provider.ConfirmSwapped);
            Assert.AreEqual("<Gamepad>/buttonWest", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad));
            Assert.AreEqual("<Gamepad>/buttonSouth", _provider.ControlPath(GameAction.Cancel, InputDeviceKind.Gamepad), "cancel stays where the player saw it");
            Assert.AreEqual("<Gamepad>/buttonEast", _provider.ControlPath(GameAction.HeroPost, InputDeviceKind.Gamepad), "hero post took confirm's old A");

            string json = _provider.SaveOverrides();
            InputActionAsset fresh = LoadCopy();
            var other = new InputSystemProvider(fresh, null, null);
            try
            {
                other.UsePadLayout(PadLayout.Nintendo);
                Assert.IsTrue(other.LoadOverrides(json));
                Assert.IsFalse(other.ConfirmSwapped, "a player's pad binding of confirm turns the swap off");
                Assert.AreEqual("<Gamepad>/buttonWest", other.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad));
                Assert.AreEqual("<Gamepad>/buttonSouth", other.ControlPath(GameAction.Cancel, InputDeviceKind.Gamepad));
            }
            finally
            {
                other.Dispose();
                Object.DestroyImmediate(fresh);
            }
        }

        [Test]
        public void Reset_BringsTheLayoutSwapBack()
        {
            _provider.UsePadLayout(PadLayout.Nintendo);
            _provider.Assign(GameAction.Confirm, InputDeviceKind.Gamepad, "<Gamepad>/buttonWest");
            _provider.ResetBindings();
            Assert.IsTrue(_provider.ConfirmSwapped);
            Assert.AreEqual("<Gamepad>/buttonEast", _provider.ControlPath(GameAction.Confirm, InputDeviceKind.Gamepad));
            Assert.AreEqual(string.Empty, _provider.SaveOverrides());
        }

        [Test]
        public void MenuNavigation_SubmitsAndCancelsByTheLayout()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            Services.Register<IInputProvider>(_provider);
            Services.Register<IInputBindings>(_provider);
            try
            {
                var module = go.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
                UiConfirmLayout layout = UiConfirmLayout.Attach(module);
                Assert.AreEqual("<Gamepad>/buttonSouth", layout.SubmitPadPath);
                Assert.AreEqual("<Gamepad>/buttonEast", layout.CancelPadPath);

                _provider.UsePadLayout(PadLayout.Nintendo);
                Assert.AreEqual("<Gamepad>/buttonEast", layout.SubmitPadPath, "A submits on a Nintendo pad");
                Assert.AreEqual("<Gamepad>/buttonSouth", layout.CancelPadPath, "B goes back");
                Assert.IsTrue(HasBinding(module.submit.action, "<Keyboard>/enter"), "Enter still submits");
                Assert.IsTrue(HasBinding(module.cancel.action, "<Keyboard>/escape"), "Esc still goes back");

                _provider.UsePadLayout(PadLayout.PlayStation);
                Assert.AreEqual("<Gamepad>/buttonSouth", layout.SubmitPadPath);
                Assert.AreEqual("<Gamepad>/buttonEast", layout.CancelPadPath);
            }
            finally
            {
                Services.Unregister<IInputProvider>();
                Services.Unregister<IInputBindings>();
                Object.DestroyImmediate(go);
            }
        }

        private static InputActionAsset LoadCopy() => InputActionAsset.FromJson(File.ReadAllText(AssetPath));

        private static bool HasBinding(InputAction action, string path)
        {
            foreach (InputBinding binding in action.bindings)
            {
                if (binding.effectivePath == path)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
