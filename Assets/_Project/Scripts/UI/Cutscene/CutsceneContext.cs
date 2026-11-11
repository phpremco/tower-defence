using System;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// Everything a cutscene set needs to build and play: the data, the camera, the quality profile, a way to make a
    /// post-processing volume for its theme (supplied by the Boot composition root, so the interface code never
    /// touches the render pipeline) and the cutscene's sound.
    /// </summary>
    public sealed class CutsceneContext
    {
        public CutsceneContext(CutsceneDefinition cutscene, LevelDefinition fieldLevel, CampaignCatalog catalog, Camera camera, Func<VisualTheme, Volume> createVolume, float particleScale, bool fogEnabled, CutsceneAudio audio)
        {
            Cutscene = cutscene;
            Cast = cutscene.Cast;
            FieldLevel = fieldLevel;
            Catalog = catalog;
            Camera = camera;
            CreateVolume = createVolume;
            ParticleScale = particleScale;
            FogEnabled = fogEnabled;
            Audio = audio;
        }

        public CutsceneDefinition Cutscene { get; }
        public CutsceneCast Cast { get; }

        /// <summary>The level whose terrain the field shots play on.</summary>
        public LevelDefinition FieldLevel { get; }

        public CampaignCatalog Catalog { get; }
        public Camera Camera { get; }

        /// <summary>Creates a global post-processing volume for a theme, at zero weight; the player fades it in.</summary>
        public Func<VisualTheme, Volume> CreateVolume { get; }

        /// <summary>The quality profile's share of particles and crowds, one for the full amount.</summary>
        public float ParticleScale { get; }

        public bool FogEnabled { get; }
        public CutsceneAudio Audio { get; }

        /// <summary>
        /// How hard the camera rumbles this frame, 0..1 (a burn, a re-entry): the set writes it in each pose, the player resets it
        /// before the pose and hands it to the camera, so it follows the shot clock alone and a sought frame shows the same rumble.
        /// </summary>
        public float Rumble { get; set; }

        /// <summary>
        /// Round 44: how hard the ground and the props tremble this frame, 0..1 (pebbles jumping, crystals shaking, burrows
        /// cracking); written like <see cref="Rumble"/>. The quake rumble follows it, so nothing shakes in silence.
        /// </summary>
        public float Tremble { get; set; }

        /// <summary>A slow handheld float of the camera in degrees for this frame (zero holds it on its mount); written like <see cref="Rumble"/>.</summary>
        public float Float { get; set; }

        /// <summary>A count from the cast scaled by the quality profile, never below one.</summary>
        public int Scaled(int count) => Mathf.Max(1, Mathf.RoundToInt(count * ParticleScale));
    }
}
