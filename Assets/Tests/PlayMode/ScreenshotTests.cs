using System.Collections;
using System.IO;
using Game.Core;
using Game.Levels;
using Game.Towers;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Writes pictures to Logs/ for a visual check without opening the editor: the last level, every campaign
    /// level's landscape and the middle of every cutscene shot. The cutscene pictures must not be black, so a set that
    /// fails to build or to light itself shows up as a failure. The real save is restored afterwards.
    /// </summary>
    public sealed class ScreenshotTests
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private const float ShotMiddle = 0.5f;
        private const float FrozenSpeed = 0.02f;
        private const int BrightnessStep = 16;
        // The darkest shot is the cave before the eyes open: crystals in black fog. Pure black would be 0.
        private const float MinBrightness = 0.006f;

        private string _savePath;
        private byte[] _saveBytes;

        [SetUp]
        public void SetUp()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "Saves", "progress.json");
            _saveBytes = File.Exists(_savePath) ? File.ReadAllBytes(_savePath) : null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_saveBytes != null)
            {
                File.WriteAllBytes(_savePath, _saveBytes);
            }
        }

        [UnityTest]
        public IEnumerator LastLevel_WritesScreenshot()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;

            var catalog = Services.Get<CampaignCatalog>();
            Services.Register(new LevelSession(catalog.Nodes[LastLevelNode(catalog)].Level, catalog.Difficulties[1]));
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

            Capture(Path.Combine(Application.dataPath, "..", "Logs", "screenshot_last_level.png"));

            // Second picture with the hero panel open, as after clicking the hero.
            context.HeroMenuPost = context.Hero.PostIndex;
            context.Changed.Raise();
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.IsNotNull(GameObject.Find("HeroPanel"), "The hero panel should be open after selecting the hero spot.");
            Capture(Path.Combine(Application.dataPath, "..", "Logs", "screenshot_last_level_hero.png"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EveryLevel_WritesLandscapeScreenshot()
        {
            // Boot once to read the catalog, then every site that has a level.
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;
            var catalog = Services.Get<CampaignCatalog>();
            for (int index = 0; index < catalog.Nodes.Count; index++)
            {
                LevelDefinition level = catalog.Nodes[index].Level;
                if (level == null)
                {
                    continue;
                }

                SceneManager.LoadScene(SceneNames.Boot);
                yield return null;
                yield return null;
                Services.Register(new LevelSession(level, catalog.Difficulties[1]));
                SceneManager.LoadScene(SceneNames.Level);
                yield return null;
                yield return null;

                Assert.IsTrue(Services.TryGet(out LevelContext context));
                PlaceSomeTowers(context);
                for (int i = 0; i < 30; i++)
                {
                    yield return null;
                }

                Capture(Path.Combine(Application.dataPath, "..", "Logs", $"terrain_{level.name}.png"));
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Cutscene_WritesShots()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;

            var catalog = Services.Get<CampaignCatalog>();
            CutsceneDefinition cutscene = catalog.Story.OpeningCutscene;
            Assert.IsNotNull(cutscene, "run Tools/Generate/Scenes and Starter Data");
            Services.Register(new CutsceneRequest(cutscene, CutsceneHandoff.ReturnScene, SceneNames.MainMenu) { Speed = FrozenSpeed });
            SceneManager.LoadScene(SceneNames.Intro);
            yield return null;
            yield return null;

            var player = Object.FindAnyObjectByType<CutscenePlayer>();
            Assert.IsNotNull(player, "the Intro scene plays the cutscene");
            for (int shot = 0; shot < cutscene.Shots.Count; shot++)
            {
                player.Seek(shot, ShotMiddle);
                yield return null;
                yield return null;
                Texture2D picture = Capture(Path.Combine(Application.dataPath, "..", "Logs", $"cutscene_shot{shot:00}.png"));
                float brightness = Brightness(picture);
                Object.Destroy(picture);
                CutsceneShot current = cutscene.Shots[shot];
                TestContext.WriteLine($"Cutscene shot {shot} {current.Set}/{current.Action}: brightness {brightness:0.000}");
                Assert.Greater(brightness, MinBrightness, $"shot {shot} ({current.Set}/{current.Action}) renders more than black");
            }

            LogAssert.NoUnexpectedReceived();
        }

        private static int LastLevelNode(CampaignCatalog catalog)
        {
            int last = 0;
            for (int i = 0; i < catalog.Nodes.Count; i++)
            {
                if (catalog.Nodes[i].Level != null)
                {
                    last = i;
                }
            }

            return last;
        }

        private static float Brightness(Texture2D picture)
        {
            Color32[] pixels = picture.GetPixels32();
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < pixels.Length; i += BrightnessStep)
            {
                sum += (pixels[i].r + pixels[i].g + pixels[i].b) / (3f * 255f);
                count++;
            }

            return sum / count;
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
        private static Texture2D Capture(string path)
        {
            Camera camera = Camera.main;
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>())
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
            texture.Release();
            return image;
        }
    }
}
