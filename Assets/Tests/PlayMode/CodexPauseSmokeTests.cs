using System;
using System.Collections;
using Game.Core;
using Game.Levels;
using Game.Platform;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Tests
{
    /// <summary>
    /// Remco 2026-11-10: the Codex (creatures and tower combinations) pauses the fight wherever it is opened from, and closing
    /// it gives back the state it found. On a real level (C1_01): from the HUD at double speed it returns to double speed; from
    /// the pause menu its B press returns to the pause menu, still paused, with the Codex button focused; from a scan's
    /// Codex link it holds the clock too. Nothing may log an error.
    /// </summary>
    public sealed class CodexPauseSmokeTests
    {
        private sealed class PressInput : IInputProvider
        {
            public Vector2 CursorMove => Vector2.zero;
            public Vector2 CameraPan => Vector2.zero;
            public float CameraZoom => 0f;
            public Vector2 PointerPosition => Vector2.zero;
            public bool OverviewHeld => false;
            public bool FromPointer => false;
            public InputDeviceKind ActiveDevice => InputDeviceKind.Gamepad;

            public event Action Confirmed { add { } remove { } }
            public event Action Cancelled;
            public event Action PauseToggled;
            public event Action SpeedToggled;
            public event Action NextTower { add { } remove { } }
            public event Action PreviousTower { add { } remove { } }
            public event Action SkipWave { add { } remove { } }
            public event Action UpgradePressed { add { } remove { } }
            public event Action RepairPressed { add { } remove { } }
            public event Action SellPressed { add { } remove { } }
            public event Action HeroPostPressed { add { } remove { } }
            public event Action<int> HeroAbilityPressed { add { } remove { } }
            public event Action CodexPressed { add { } remove { } }
            public event Action ActiveDeviceChanged { add { } remove { } }
            public event Action DeviceLost { add { } remove { } }
            public event Action DeviceRegained { add { } remove { } }

            public string PromptFor(GameAction action) => string.Empty;

            public string PromptFor(GameAction action, InputDeviceKind device) => string.Empty;

            public void Back() => Cancelled?.Invoke();

            public void Pause() => PauseToggled?.Invoke();

            public void Speed() => SpeedToggled?.Invoke();

            public void Dispose()
            {
            }
        }

        [UnityTest]
        public IEnumerator TheCodex_PausesTheFight_AndGivesBackTheStateItFound()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;
            IInputProvider original = Services.Get<IInputProvider>();
            var input = new PressInput();
            Services.Register<IInputProvider>(input);
            try
            {
                var catalog = Services.Get<CampaignCatalog>();
                Services.Register(new LevelSession(catalog.Nodes[0].Level, catalog.Difficulties[1]));
                SceneManager.LoadScene(SceneNames.Level);
                LevelContext context = null;
                while (!Services.TryGet(out context))
                {
                    yield return null;
                }

                for (int i = 0; i < 10; i++)
                {
                    yield return null;
                }

                context.BriefingPending = false;
                CodexView codex = Object.FindAnyObjectByType<CodexView>();
                Assert.IsNotNull(codex, "the level has no codex");
                Assert.IsTrue(codex.Available, "the codex has nothing to show");

                yield return FromTheHud(context, codex, input);
                yield return FromThePauseMenu(context, codex, input);
                yield return FromAScan(context, codex, input);
            }
            finally
            {
                Services.Register(original);
                Time.timeScale = 1f;
            }

            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator FromTheHud(LevelContext context, CodexView codex, PressInput input)
        {
            input.Speed();
            yield return null;
            Assert.AreEqual(2f, Time.timeScale, "the speed button did not double the clock");

            codex.Open();
            yield return null;
            Assert.IsTrue(codex.IsOpen);
            Assert.AreEqual(0f, Time.timeScale, "the codex opened from the HUD did not stop the fight");

            input.Back();
            yield return null;
            Assert.IsFalse(codex.IsOpen, "B did not close the codex");
            Assert.IsFalse(context.Paused, "closing the codex opened the pause menu");
            Assert.AreEqual(2f, Time.timeScale, "closing the codex did not return to double speed");

            // Back to normal speed for the rest (three steps: 2 -> 3 -> 1).
            input.Speed();
            input.Speed();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
        }

        private static IEnumerator FromThePauseMenu(LevelContext context, CodexView codex, PressInput input)
        {
            input.Pause();
            yield return null;
            Assert.IsTrue(context.Paused);
            Button codexButton = FindButton("PauseDim", "Codex");
            Assert.IsNotNull(codexButton, "the pause menu has no Codex button");

            codexButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(codex.IsOpen, "the pause menu's Codex button did not open the book");
            Assert.IsTrue(context.Paused, "opening the codex from the pause menu unpaused the game");
            Assert.AreEqual(0f, Time.timeScale);

            input.Back();
            yield return null;
            yield return null;
            Assert.IsFalse(codex.IsOpen, "B did not close the codex");
            Assert.IsTrue(context.Paused, "closing the codex opened from the pause menu resumed the fight");
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreSame(codexButton.gameObject, EventSystem.current.currentSelectedGameObject, "the focus did not come back to the Codex button");

            input.Back();
            yield return null;
            Assert.IsFalse(context.Paused, "B in the pause menu did not resume");
            Assert.AreEqual(1f, Time.timeScale);
        }

        private static IEnumerator FromAScan(LevelContext context, CodexView codex, PressInput input)
        {
            var codexCatalog = Services.Get<CodexCatalog>();
            Assert.Greater(codexCatalog.Enemies.Count, 0);
            codex.OpenAt(codexCatalog.Enemies[0]);
            yield return null;
            Assert.IsTrue(codex.IsOpen, "the scan's Codex link did not open the book");
            Assert.AreEqual(0f, Time.timeScale, "the codex opened from a scan did not stop the fight");

            input.Back();
            yield return null;
            Assert.IsFalse(codex.IsOpen);
            Assert.IsFalse(context.Paused);
            Assert.AreEqual(1f, Time.timeScale, "the clock did not run again after the scan's codex closed");
        }

        private static Button FindButton(string rootName, string label)
        {
            foreach (Button button in Object.FindObjectsByType<Button>())
            {
                Text text = button.GetComponentInChildren<Text>();
                if (text != null && text.text == label && IsUnder(button.transform, rootName))
                {
                    return button;
                }
            }

            return null;
        }

        private static bool IsUnder(Transform transform, string name)
        {
            for (Transform t = transform; t != null; t = t.parent)
            {
                if (t.name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
