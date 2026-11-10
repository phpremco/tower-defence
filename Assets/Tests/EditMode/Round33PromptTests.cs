using Game.Platform;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Round 33: the button-name tables the prompts print, and which device the prompts follow.</summary>
    public sealed class Round33PromptTests
    {
        private const string SkinPath = "Assets/_Project/Settings/UiSkin.asset";

        private static readonly ButtonGlyph[] Xbox =
        {
            new ButtonGlyph("buttonSouth", "A"), new ButtonGlyph("buttonEast", "B"), new ButtonGlyph("leftTrigger", "LT"),
        };

        [Test]
        public void ControlName_DropsTheDevicePart()
        {
            Assert.AreEqual("buttonSouth", ButtonGlyphTable.ControlName("<Gamepad>/buttonSouth"));
            Assert.AreEqual("leftStick/up", ButtonGlyphTable.ControlName("<Gamepad>/leftStick/up"));
            Assert.AreEqual("escape", ButtonGlyphTable.ControlName("escape"));
            Assert.AreEqual(string.Empty, ButtonGlyphTable.ControlName("<Gamepad>"));
            Assert.AreEqual(string.Empty, ButtonGlyphTable.ControlName(null));
        }

        [Test]
        public void Lookup_FindsTheLabelByControl_IgnoringCaseAndDevice()
        {
            Assert.IsTrue(ButtonGlyphTable.TryFind(Xbox, "<Gamepad>/buttonSouth", out string south));
            Assert.AreEqual("A", south);
            Assert.IsTrue(ButtonGlyphTable.TryFind(Xbox, "<XInputController>/LEFTTRIGGER", out string trigger));
            Assert.AreEqual("LT", trigger);
        }

        [Test]
        public void Lookup_MissesAControlTheTableDoesNotName_SoTheInputSystemNameIsUsed()
        {
            Assert.IsFalse(ButtonGlyphTable.TryFind(Xbox, "<Gamepad>/buttonNorth", out string label));
            Assert.AreEqual(string.Empty, label);
            Assert.IsFalse(ButtonGlyphTable.TryFind(null, "<Gamepad>/buttonSouth", out _));
        }

        [Test]
        public void GeneratedTables_NameTheFaceButtonsPerLayout()
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            Assert.IsNotNull(skin, "UI skin missing");
            ButtonGlyphTable xbox = skin.PadGlyphs(PadLayout.Xbox);
            ButtonGlyphTable nintendo = skin.PadGlyphs(PadLayout.Nintendo);
            Assert.IsNotNull(xbox, "no Xbox glyph table on the skin");
            Assert.IsNotNull(nintendo, "no Nintendo glyph table on the skin");
            Assert.AreEqual(PadLayout.Xbox, xbox.Layout);
            Assert.AreEqual(PadLayout.Nintendo, nintendo.Layout);

            // The same physical button carries a different letter on the two layouts.
            AssertLabel(xbox, "<Gamepad>/buttonSouth", "A");
            AssertLabel(nintendo, "<Gamepad>/buttonSouth", "B");
            AssertLabel(xbox, "<Gamepad>/buttonNorth", "Y");
            AssertLabel(nintendo, "<Gamepad>/buttonNorth", "X");
            AssertLabel(xbox, "<Gamepad>/leftTrigger", "LT");
            AssertLabel(nintendo, "<Gamepad>/leftTrigger", "ZL");
            Assert.IsNotNull(skin.KeyGlyphs, "no key table on the skin");
            AssertLabel(skin.KeyGlyphs, "<Keyboard>/escape", "Esc");
            Assert.GreaterOrEqual(skin.MinReadableFontSize, 27, "small text must reach about 18 px at 720p (27 canvas units)");
        }

        [Test]
        public void Tracker_FollowsTheLastDeviceUsed()
        {
            var tracker = new ActiveDeviceTracker();
            Assert.AreEqual(InputDeviceKind.KeyboardMouse, tracker.Kind);
            Assert.IsFalse(tracker.Used(InputDeviceKind.KeyboardMouse, 1), "same kind is no change");
            Assert.IsTrue(tracker.Used(InputDeviceKind.Gamepad, 7));
            Assert.AreEqual(InputDeviceKind.Gamepad, tracker.Kind);
            Assert.AreEqual(7, tracker.GamepadId);
            Assert.IsTrue(tracker.Used(InputDeviceKind.KeyboardMouse, 1));
            Assert.AreEqual(7, tracker.GamepadId, "the pad is remembered while the keyboard is in use");
        }

        [Test]
        public void Tracker_LosingThePadInUse_PausesOnce_OtherDevicesDoNot()
        {
            var tracker = new ActiveDeviceTracker();
            tracker.Used(InputDeviceKind.Gamepad, 7);
            Assert.IsFalse(tracker.Lost(8), "another pad dropping out is not the one in use");
            Assert.IsTrue(tracker.Lost(7));
            Assert.IsFalse(tracker.Lost(7), "one loss, one pause");
            Assert.IsTrue(tracker.Regained(9), "a reconnected pad may come back under a new id");
            Assert.AreEqual(9, tracker.GamepadId);
            Assert.IsFalse(tracker.Regained(9));
        }

        [Test]
        public void Tracker_PadLostWhileTheKeyboardIsInUse_DoesNotPause()
        {
            var tracker = new ActiveDeviceTracker();
            tracker.Used(InputDeviceKind.Gamepad, 7);
            tracker.Used(InputDeviceKind.KeyboardMouse, 1);
            Assert.IsFalse(tracker.Lost(7));
        }

        [Test]
        public void KeyedLabel_PutsTheKeyFirst_AndShowsTheTextAloneWithoutOne()
        {
            StringAssert.Contains("LT", UiKit.Keyed("LT", "Shockwave"));
            StringAssert.EndsWith("Shockwave", UiKit.Keyed("LT", "Shockwave"));
            Assert.AreEqual("Shockwave", UiKit.Keyed(string.Empty, "Shockwave"));
        }

        private static void AssertLabel(ButtonGlyphTable table, string path, string expected)
        {
            Assert.IsTrue(table.TryGet(path, out string label), $"{table.name} has no label for {path}");
            Assert.AreEqual(expected, label, $"{table.name}: {path}");
        }
    }
}
