using Game.Core;
using Game.Platform;
using Game.UI;
using UnityEngine;

namespace Game.Boot
{
    /// <summary>
    /// Composition root of the main menu scene: the camera with the game's post-processing, and the living diorama
    /// behind the buttons. Runs in Awake so the menu screen finds the camera when it builds its canvas.
    /// </summary>
    public sealed class MenuStage : MonoBehaviour
    {
        [SerializeField] private MenuStageDefinition _stage;

        private void Awake()
        {
            // Without services the screen sends the player back to Boot; without a complete stage there is nothing to show.
            if (_stage == null || !_stage.IsComplete || !Services.TryGet(out IPlatformSettings platform) || !Services.TryGet(out AudioMix mix))
            {
                return;
            }

            var cameraGo = new GameObject("MenuCamera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            PostProcessingBuilder.Build(camera, _stage.NestTheme, platform);
            MenuDiorama.Build(_stage, camera, mix, platform.ParticleScale);
        }
    }
}
