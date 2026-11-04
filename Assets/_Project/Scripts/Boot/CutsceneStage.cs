using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.Platform;
using Game.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Boot
{
    /// <summary>
    /// Composition root of the Intro scene: the perspective camera with its listener, the post-processing volumes
    /// per set (made here, so the interface code never touches the render pipeline) and the cutscene player. The
    /// tilt-shift band blur belongs to the level's fixed camera, so it is off while a cutscene plays and goes back to
    /// the profile's setting afterwards. Without services (the scene played on its own) it sends the player to Boot.
    /// </summary>
    public sealed class CutsceneStage : MonoBehaviour
    {
        [Tooltip("The tilt-shift pass on the PC renderer, switched off for the cutscene's perspective shots.")]
        [SerializeField] private FullScreenPassRendererFeature _tiltShift;

        private IPlatformSettings _platform;

        // Awake, so the listener exists from the first frame: the menu music still fades out while the scene loads.
        private void Awake()
        {
            if (!Services.TryGet(out ProgressService progress) || !Services.TryGet(out _platform) || !Services.TryGet(out CampaignCatalog catalog))
            {
                SceneManager.LoadScene(SceneNames.Boot);
                return;
            }

            CutsceneRequest request = Request(catalog);
            if (request == null)
            {
                // Nothing to play: the first launch still counts as seen and goes on to the menu.
                progress.MarkIntroSeen();
                SceneManager.LoadScene(SceneNames.MainMenu);
                return;
            }

            Time.timeScale = 1f;
            UiKit.EnsureEventSystem();
            if (_tiltShift != null)
            {
                _tiltShift.SetActive(false);
            }

            var cameraGo = new GameObject("CutsceneCamera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            IPlatformSettings platform = _platform;
            Services.TryGet(out AudioMix mix);
            Services.TryGet(out SfxLibrary library);
            var player = new GameObject("CutscenePlayer").AddComponent<CutscenePlayer>();
            player.Initialize(request, catalog, camera, theme => PostProcessingBuilder.Build(camera, theme, platform, 0f, transform), PostProcessingBuilder.BuildExposure(transform), platform, Services.Get<IInputProvider>(), progress, mix, library);
        }

        private void OnDestroy()
        {
            if (_tiltShift != null && _platform != null)
            {
                _tiltShift.SetActive(_platform.DepthOfFieldEnabled);
            }
        }

        // Boot registers a request on the first launch, a replay registers its own; playing the scene from Boot
        // without one (a test, a tool) plays the opening cutscene as its data says. The request is consumed here,
        // so a later visit of the scene never plays with a stale return scene or test speed.
        private static CutsceneRequest Request(CampaignCatalog catalog)
        {
            if (Services.TryGet(out CutsceneRequest request))
            {
                Services.Unregister<CutsceneRequest>();
                return request;
            }

            CutsceneDefinition opening = catalog.Story != null ? catalog.Story.OpeningCutscene : null;
            return opening != null ? CutsceneRequest.FirstLaunch(opening) : null;
        }
    }
}
