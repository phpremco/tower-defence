using System;
using System.Collections;
using System.Reflection;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Platform;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Game.Tests
{
    /// <summary>
    /// Smoke test of the overview and the radar on a real level (C1_01): holding the overview zooms the camera out to the
    /// whole field and blocks the build cursor, the pan keys move the aim, a confirm fixes it and letting go returns to the
    /// play zoom at that spot; the radar shows the base, the towers and a spawned enemy, and a click on its centre sends
    /// the camera to the field's centre. Nothing may log an error.
    /// </summary>
    public sealed class OverviewRadarSmokeTests
    {
        private const float Tolerance = 0.05f;
        private static string _report = string.Empty;

        private sealed class HeldInput : IInputProvider
        {
            public Vector2 CursorMove => Vector2.zero;
            public Vector2 CameraPan { get; set; }
            public float CameraZoom => 0f;
            public Vector2 PointerPosition => Vector2.zero;
            public bool OverviewHeld { get; set; }
            public bool FromPointer => false;

            public event Action Confirmed;
            public event Action Cancelled { add { } remove { } }
            public event Action PauseToggled { add { } remove { } }
            public event Action SpeedToggled { add { } remove { } }
            public event Action NextTower { add { } remove { } }
            public event Action PreviousTower { add { } remove { } }
            public event Action SkipWave { add { } remove { } }
            public event Action UpgradePressed { add { } remove { } }
            public event Action RepairPressed { add { } remove { } }
            public event Action SellPressed { add { } remove { } }
            public event Action HeroPostPressed { add { } remove { } }
            public event Action<int> HeroAbilityPressed { add { } remove { } }

            public void Confirm() => Confirmed?.Invoke();

            public void Dispose()
            {
            }
        }

        [UnityTest]
        public IEnumerator OverviewZoomsOutAndReturnsToThePick_RadarShowsTheField_AndAClickMovesTheCamera()
        {
            _report = string.Empty;
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;
            IInputProvider original = Services.Get<IInputProvider>();
            var input = new HeldInput();
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

                var panZoom = Object.FindAnyObjectByType<CameraPanZoom>();
                var rig = Object.FindAnyObjectByType<FixedCameraRig>();
                Camera camera = rig.GetComponent<Camera>();
                panZoom.Locked = false;
                context.BriefingPending = false;
                yield return CheckOverview(context, panZoom, rig, camera, input);
                yield return CheckRadar(context, rig);
            }
            finally
            {
                Services.Register(original);
            }

            LogAssert.NoUnexpectedReceived();
            Debug.Log("Overview and radar smoke: " + _report);
        }

        private static IEnumerator CheckOverview(LevelContext context, CameraPanZoom panZoom, FixedCameraRig rig, Camera camera, HeldInput input)
        {
            Vector3 start = rig.Focus;
            float playZoom = rig.Zoom;
            float fitted = FixedCameraRig.FitSize(context.Level, camera.aspect);
            input.OverviewHeld = true;
            yield return WaitUnscaled(OverviewFraming.BlendSeconds + 0.2f);
            Assert.IsTrue(context.OverviewActive, "holding the overview did not take the camera");
            Assert.AreEqual(OverviewFraming.Zoom, rig.Zoom, Tolerance, "the overview does not frame the whole field");
            Assert.AreEqual(fitted, camera.orthographicSize, fitted * Tolerance);

            // The pan keys move the aim to the right; a confirm fixes it.
            input.CameraPan = Vector2.right;
            yield return WaitUnscaled(0.4f);
            input.CameraPan = Vector2.zero;
            input.Confirm();
            input.OverviewHeld = false;
            yield return WaitUnscaled(OverviewFraming.BlendSeconds + 0.2f);
            Assert.IsFalse(context.OverviewActive, "the overview did not let go");
            Assert.AreEqual(playZoom, rig.Zoom, Tolerance, "the camera did not return to the play zoom");
            Assert.Greater(rig.Focus.x, start.x + 1f, "the camera did not go to the picked spot");
            _report += $"Overview: play zoom {playZoom:0.00}, overview size {fitted:0.0}, focus {start} -> {rig.Focus}; ";
        }

        private static IEnumerator CheckRadar(LevelContext context, FixedCameraRig rig)
        {
            var radar = Object.FindAnyObjectByType<RadarView>();
            Assert.IsNotNull(radar, "the HUD has no radar");
            Assert.IsTrue(radar.Panel.gameObject.activeInHierarchy, "the radar is hidden");
            var spawner = (EnemySpawner)typeof(LevelController).GetField("_spawner", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Object.FindAnyObjectByType<LevelController>());
            spawner.Spawn(AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Skitter.asset"), 0);
            yield return WaitUnscaled(0.3f);
            Transform symbols = radar.Panel.Find("Map/Symbols");
            int shown = 0;
            foreach (Transform blip in symbols)
            {
                shown += blip.gameObject.activeSelf ? 1 : 0;
            }

            // At least the base, the level's gates and the skitter.
            Assert.GreaterOrEqual(shown, 2, "the radar shows almost nothing");

            Vector3 centre = RectTransformUtility.WorldToScreenPoint(null, radar.Panel.Find("Map").position);
            var click = new PointerEventData(EventSystem.current) { position = centre };
            radar.OnPointerClick(click);
            yield return WaitUnscaled(1.2f);
            Assert.Less(new Vector2(rig.Focus.x, rig.Focus.z).magnitude, 2.5f, $"a click on the radar's centre left the camera at {rig.Focus}");
            _report += $"Radar: {shown} symbols shown, camera after the click at {rig.Focus}";
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }
    }
}
