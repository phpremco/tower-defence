using System.Collections;
using System.IO;
using Game.Core;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>Writes a picture of the last level of the world to Logs/, for a visual check without opening the editor.</summary>
    public sealed class ScreenshotTests
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private const int LevelIndex = 4;

        [UnityTest]
        public IEnumerator LastLevel_WritesScreenshot()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;

            var catalog = Services.Get<CampaignCatalog>();
            Services.Register(new LevelSession(catalog.Nodes[LevelIndex].Level, catalog.Difficulties[1]));
            SceneManager.LoadScene(SceneNames.Level);
            yield return null;
            yield return null;

            Assert.IsTrue(Services.TryGet(out LevelContext context));
            PlaceSomeTowers(context);

            // Let a few seconds of the first wave happen, then draw everything including the HUD.
            for (int i = 0; i < 90; i++)
            {
                yield return null;
            }

            Capture(Path.Combine(Application.dataPath, "..", "Logs", "screenshot_level5.png"));

            // Second picture with the hero panel open, as after clicking the hero.
            context.HeroMenuPost = context.Hero.PostIndex;
            context.Changed.Raise();
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.IsNotNull(GameObject.Find("HeroPanel"), "The hero panel should be open after selecting the hero spot.");
            Capture(Path.Combine(Application.dataPath, "..", "Logs", "screenshot_level5_hero.png"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EveryLevel_WritesLandscapeScreenshot()
        {
            for (int index = 0; index < 5; index++)
            {
                SceneManager.LoadScene(SceneNames.Boot);
                yield return null;
                yield return null;

                var catalog = Services.Get<CampaignCatalog>();
                Services.Register(new LevelSession(catalog.Nodes[index].Level, catalog.Difficulties[1]));
                SceneManager.LoadScene(SceneNames.Level);
                yield return null;
                yield return null;

                Assert.IsTrue(Services.TryGet(out LevelContext context));
                PlaceSomeTowers(context);
                for (int i = 0; i < 30; i++)
                {
                    yield return null;
                }

                Capture(Path.Combine(Application.dataPath, "..", "Logs", $"terrain_level{index + 1}.png"));
            }

            LogAssert.NoUnexpectedReceived();
        }

        private static void PlaceSomeTowers(LevelContext context)
        {
            int index = 0;
            foreach (TowerDefinition definition in context.AllTowers)
            {
                var go = new GameObject($"ShotTower_{definition.DisplayName}");
                go.transform.position = context.Level.Grid.CellToWorld(new Vector2Int(3 + index, 7 - (index % 2)));
                var tower = go.AddComponent<Tower>();
                go.AddComponent<TowerView>();
                tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
                tower.Register(context.Structures);
                index++;
            }

            context.Hero.MoveTo(1);
        }

        // Overlay canvases are not drawn by a camera render, so they are switched to camera space for the picture.
        private static void Capture(string path)
        {
            Camera camera = Camera.main;
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = camera.nearClipPlane + 0.5f;
                }
            }

            var texture = new RenderTexture(Width, Height, 24);
            camera.targetTexture = texture;
            camera.aspect = Width / (float)Height;
            camera.Render();
            camera.targetTexture = null;

            RenderTexture.active = texture;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
    }
}
