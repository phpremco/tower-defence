using Game.Platform;
using Game.UI;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>Round 70: the pad in the player's hands picks the button names (Xbox, PlayStation, Nintendo).</summary>
    public sealed class PadLayoutTests
    {
        private const string SkinPath = "Assets/_Project/Settings/UiSkin.asset";

        [TestCase("DualSenseGamepadHID", PadLayout.PlayStation)]
        [TestCase("DualShock4GamepadHID", PadLayout.PlayStation)]
        [TestCase("DualShock3GamepadHID", PadLayout.PlayStation)]
        [TestCase("SwitchProControllerHID", PadLayout.Nintendo)]
        [TestCase("XInputControllerWindows", PadLayout.Xbox)]
        [TestCase("XInputController", PadLayout.Xbox)]
        public void RegisteredPadLayouts_MapToTheirFamily(string layout, PadLayout expected)
        {
            Assert.AreEqual(expected, PadLayoutResolver.ForDeviceLayout(layout, PadLayout.Nintendo));
        }

        [Test]
        public void AnUnknownPad_KeepsThePlatformDefault()
        {
            Assert.AreEqual(PadLayout.Xbox, PadLayoutResolver.ForDeviceLayout("Gamepad", PadLayout.Xbox));
            Assert.AreEqual(PadLayout.Nintendo, PadLayoutResolver.ForDeviceLayout("Gamepad", PadLayout.Nintendo));
            Assert.AreEqual(PadLayout.Xbox, PadLayoutResolver.ForDeviceLayout(null, PadLayout.Xbox));
        }

        [Test]
        public void TheChain_IsReadMostSpecificFirst()
        {
            // A third-party HID pad that the Input System files under the DualShock base reads as PlayStation.
            Assert.AreEqual(PadLayout.PlayStation, PadLayoutResolver.ForLayoutChain(new[] { "SomeVendorPadHID", "DualShockGamepad", "Gamepad" }, PadLayout.Xbox));
            Assert.AreEqual(PadLayout.Xbox, PadLayoutResolver.ForLayoutChain(new[] { "Gamepad" }, PadLayout.Xbox));
            Assert.AreEqual(PadLayout.Xbox, PadLayoutResolver.ForLayoutChain(null, PadLayout.Xbox));
            CollectionAssert.Contains(PadLayoutResolver.Chain("DualSenseGamepadHID"), "Gamepad", "the chain walks up to the base pad layout");
        }

        [Test]
        public void TheSkin_HasANamesTableForEveryLayout()
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            Assert.IsNotNull(skin);
            foreach (PadLayout layout in new[] { PadLayout.Xbox, PadLayout.Nintendo, PadLayout.PlayStation })
            {
                ButtonGlyphTable table = skin.PadGlyphs(layout);
                Assert.IsNotNull(table, layout.ToString());
                Assert.AreEqual(layout, table.Layout, $"the {layout} pad falls back to another table");
            }

            Assert.IsTrue(skin.PadGlyphs(PadLayout.PlayStation).TryGet("<Gamepad>/buttonSouth", out string cross));
            Assert.AreEqual("Cross", cross);
            Assert.IsTrue(skin.PadGlyphs(PadLayout.PlayStation).TryGet("<Gamepad>/leftTrigger", out string l2));
            Assert.AreEqual("L2", l2);
        }

        [Test]
        public void GlyphFamilies_FollowDeviceAndLayout()
        {
            Assert.AreEqual(GlyphFamily.Keyboard, GlyphFamilies.For(InputDeviceKind.KeyboardMouse, PadLayout.PlayStation));
            Assert.AreEqual(GlyphFamily.PlayStation, GlyphFamilies.For(InputDeviceKind.Gamepad, PadLayout.PlayStation));
            Assert.AreEqual(GlyphFamily.Nintendo, GlyphFamilies.For(InputDeviceKind.Gamepad, PadLayout.Nintendo));
            Assert.AreEqual(GlyphFamily.Xbox, GlyphFamilies.For(InputDeviceKind.Gamepad, PadLayout.Xbox));
        }
    }
}
