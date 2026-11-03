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

        /// <summary>A count from the cast scaled by the quality profile, never below one.</summary>
        public int Scaled(int count) => Mathf.Max(1, Mathf.RoundToInt(count * ParticleScale));
    }
}
