using System;
using System.Collections.Generic;
using System.IO;
using Game.Platform;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace Game.Tests
{
    /// <summary>
    /// Round 70: every action has a drawn glyph in every family (Xbox, PlayStation, Nintendo, keyboard), with letters the
    /// line font can draw, and every key a player can rebind to gets a readable key cap.
    /// </summary>
    public sealed class GlyphCoverageTests
    {
        private const string AssetPath = "Assets/_Project/Settings/Input/GameInput.inputactions";
        private const string KeyTablePath = "Assets/_Project/Settings/Input/Glyphs_Keyboard.asset";

        private static readonly GlyphFamily[] PadFamilies = { GlyphFamily.Xbox, GlyphFamily.PlayStation, GlyphFamily.Nintendo };

        [Test]
        public void EveryAction_HasAGlyphInEveryFamily()
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(AssetPath));
            var provider = new InputSystemProvider(asset, null, null);
            try
            {
                foreach (GameAction action in (GameAction[])Enum.GetValues(typeof(GameAction)))
                {
                    string key = provider.ControlPath(action, InputDeviceKind.KeyboardMouse);
                    GlyphSpec keySpec = GlyphCatalog.Describe(GlyphFamily.Keyboard, key, provider.PromptFor(action, InputDeviceKind.KeyboardMouse));
                    AssertDrawable(keySpec, $"{action} on the keyboard ({key})");

                    string pad = provider.ControlPath(action, InputDeviceKind.Gamepad);
                    if (pad.Length == 0)
                    {
                        continue;
                    }

                    foreach (GlyphFamily family in PadFamilies)
                    {
                        GlyphSpec spec = GlyphCatalog.Describe(family, pad, provider.PromptFor(action, InputDeviceKind.Gamepad));
                        AssertDrawable(spec, $"{action} on {family} ({pad})");
                        Assert.AreNotEqual(GlyphShape.KeyCap, spec.Shape, $"{action} on {family} drew a key cap");
                    }
                }
            }
            finally
            {
                provider.Dispose();
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void TheFaceButtons_ReadDifferentlyPerFamily()
        {
            GlyphSpec xbox = GlyphCatalog.Describe(GlyphFamily.Xbox, "<Gamepad>/buttonSouth", "A");
            GlyphSpec playStation = GlyphCatalog.Describe(GlyphFamily.PlayStation, "<Gamepad>/buttonSouth", "Cross");
            GlyphSpec nintendo = GlyphCatalog.Describe(GlyphFamily.Nintendo, "<Gamepad>/buttonSouth", "B");
            Assert.AreEqual("A", xbox.Text);
            Assert.AreEqual(GlyphSymbol.Cross, playStation.Symbol);
            Assert.AreEqual("B", nintendo.Text);
            Assert.AreEqual(GlyphSymbol.Triangle, GlyphCatalog.Describe(GlyphFamily.PlayStation, "<Gamepad>/buttonNorth", string.Empty).Symbol);
            Assert.AreEqual("L2", GlyphCatalog.Describe(GlyphFamily.PlayStation, "<Gamepad>/leftTrigger", string.Empty).Text);
            Assert.AreEqual("ZR", GlyphCatalog.Describe(GlyphFamily.Nintendo, "<Gamepad>/rightTrigger", string.Empty).Text);
            Assert.AreEqual(GlyphShape.Menu, GlyphCatalog.Describe(GlyphFamily.Xbox, "<Gamepad>/start", string.Empty).Shape);
            Assert.AreEqual(GlyphSymbol.MouseLeft, GlyphCatalog.Describe(GlyphFamily.Keyboard, "<Mouse>/leftButton", "Click").Symbol);
            Assert.AreNotEqual(xbox.Key, playStation.Key);
        }

        [Test]
        public void EveryKeyboardKey_GetsAKeyCapTheFontCanDraw()
        {
            var table = AssetDatabase.LoadAssetAtPath<ButtonGlyphTable>(KeyTablePath);
            Assert.IsNotNull(table);
            InputControlLayout keyboard = InputSystem.LoadLayout("Keyboard");
            var missing = new List<string>();
            foreach (InputControlLayout.ControlItem control in keyboard.controls)
            {
                if (control.layout != "Key")
                {
                    continue;
                }

                string label = table.TryGet(control.name, out string shortName) ? shortName : control.displayName;
                if (string.IsNullOrEmpty(label))
                {
                    label = control.name;
                }

                if (!StrokeFont.CoversAll(label.ToUpperInvariant()))
                {
                    missing.Add($"{control.name} '{label}'");
                }
            }

            CollectionAssert.IsEmpty(missing, "keys whose names the glyph font cannot draw");
        }

        [Test]
        public void Painter_DrawsEveryShape_InsideItsTexture()
        {
            var specs = new[]
            {
                GlyphCatalog.Describe(GlyphFamily.Xbox, "<Gamepad>/buttonSouth", "A"),
                GlyphCatalog.Describe(GlyphFamily.PlayStation, "<Gamepad>/buttonEast", string.Empty),
                GlyphCatalog.Describe(GlyphFamily.PlayStation, "<Gamepad>/buttonWest", string.Empty),
                GlyphCatalog.Describe(GlyphFamily.Xbox, "<Gamepad>/leftShoulder", string.Empty),
                GlyphCatalog.Describe(GlyphFamily.Xbox, "<Gamepad>/rightTrigger", string.Empty),
                GlyphCatalog.Describe(GlyphFamily.Xbox, "<Gamepad>/select", string.Empty),
                GlyphCatalog.Describe(GlyphFamily.Xbox, "<Gamepad>/leftStick", string.Empty),
                GlyphCatalog.Describe(GlyphFamily.Xbox, "<Gamepad>/dpad/up", string.Empty),
                GlyphCatalog.Describe(GlyphFamily.Keyboard, "<Mouse>/rightButton", string.Empty),
                GlyphCatalog.KeyCap("Num Enter"),
            };
            foreach (GlyphSpec spec in specs)
            {
                Color32[] pixels = GlyphPainter.Paint(spec, GlyphPainter.DefaultHeight, out int width);
                Assert.AreEqual(width * GlyphPainter.DefaultHeight, pixels.Length);
                Assert.Greater(Opaque(pixels), pixels.Length / 8, $"{spec.Shape} {spec.Text} drew almost nothing");
                Assert.AreEqual(0, pixels[0].a, $"{spec.Shape} fills its corner");
            }

            int keyWidth = GlyphPainter.WidthFor(GlyphCatalog.KeyCap("Space"), GlyphPainter.DefaultHeight);
            Assert.Greater(keyWidth, GlyphPainter.WidthFor(GlyphCatalog.KeyCap("F"), GlyphPainter.DefaultHeight), "a long key name gets a wider cap");
        }

        private static void AssertDrawable(GlyphSpec spec, string what)
        {
            Assert.IsTrue(StrokeFont.CoversAll(spec.Text), $"{what}: the font cannot draw '{spec.Text}'");
            bool hasMark = spec.Text.Length > 0 || spec.Symbol != GlyphSymbol.None || spec.Shape == GlyphShape.Menu || spec.Shape == GlyphShape.View;
            Assert.IsTrue(hasMark, $"{what}: an empty glyph");
        }

        private static int Opaque(Color32[] pixels)
        {
            int count = 0;
            foreach (Color32 pixel in pixels)
            {
                if (pixel.a > 128)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
