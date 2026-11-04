using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A planet painted in Blender instead of by noise: equirectangular maps laid out as <see cref="PlanetCoordinates.ToUv"/>
    /// (albedo, normal, a packed mask, region ids, glow and cloud cover) in a High and a Low (Switch) size, plus how the
    /// planet shaders wear them. The builder writes only the maps, so tweaks to the look below survive a re-import.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story/Planet Texture Set", fileName = "PlanetTextureSet")]
    public sealed class PlanetTextureSet : ScriptableObject
    {
        /// <summary>One size of the map set.</summary>
        [Serializable]
        public sealed class Maps
        {
            public Texture2D Albedo;
            public Texture2D Normal;

            [Tooltip("R land, G gloss (sea 1, dry land 0.1), B glow strength, A region id.")]
            public Texture2D Mask;

            [Tooltip("Point filtered: R region id on land, G id with its sea territory, B soft membership.")]
            public Texture2D Regions;
            public Texture2D Emission;
            public Texture2D Clouds;

            public bool IsComplete => Albedo != null && Normal != null && Mask != null && Regions != null && Emission != null && Clouds != null;
        }

        [SerializeField] private Maps _high = new Maps();
        [SerializeField] private Maps _low = new Maps();

        [Header("Regions")]
        [Tooltip("The id the region map gives the first region of the campaign; region i has id FirstRegionId + i.")]
        [SerializeField] [Min(1)] private int _firstRegionId = 1;
        [Tooltip("The value step between two region ids in the region map (id = round(value * 255 / step)).")]
        [SerializeField] [Min(1f)] private float _regionValueStep = 40f;

        [Header("Look")]
        [Tooltip("The glow map is already coloured; this scales it.")]
        [ColorUsage(false, true)] [SerializeField] private Color _emission = new Color(1.6f, 1.6f, 1.6f);
        [Tooltip("Share of the night glow that also shows on the day side of the map globe.")]
        [SerializeField] [Range(0f, 1f)] private float _mapDayEmission = 0.3f;
        [SerializeField] [Range(0f, 4f)] private float _mapRelief = 0.8f;
        [SerializeField] [Range(0f, 4f)] private float _cutsceneRelief = 1f;
        [Tooltip("The painted sea is near black in linear light; this lifts it so the oceans read deep blue by day.")]
        [SerializeField] [Range(1f, 4f)] private float _seaLift = 1.8f;
        [Tooltip("Share of the cloud cover on the map globe, so the regions stay readable under the painted weather.")]
        [SerializeField] [Range(0f, 1f)] private float _mapCloudShare = 0.55f;
        [Tooltip("Share of the cloud cover in the cutscene shots.")]
        [SerializeField] [Range(0f, 1f)] private float _cutsceneCloudShare = 0.75f;
        [Tooltip("How far the painted cloud map is torn into wisps by noise; the painted map has its own detail.")]
        [SerializeField] [Range(0f, 1f)] private float _cloudWisps = 0.3f;
        [Tooltip("Fine noise over the land, so a close view never looks like a blown-up picture.")]
        [SerializeField] [Range(0f, 0.5f)] private float _grain = 0.12f;
        [Tooltip("The colony lights of every built level site, over the painted glow.")]
        [SerializeField] [Range(0f, 4f)] private float _colonyLights = 1f;

        [Header("Selected region on the map")]
        [Tooltip("How much brighter the chosen region's land is lit.")]
        [SerializeField] [Range(0f, 1f)] private float _regionLift = 0.22f;
        [Tooltip("How much the other regions darken while one is chosen.")]
        [SerializeField] [Range(0f, 1f)] private float _regionDim = 0.25f;
        [Tooltip("The glowing rim the chosen region gets in the sea along its coast.")]
        [SerializeField] [Range(0f, 4f)] private float _regionRim = 0.6f;
        [Tooltip("A region that cannot be entered yet glows this much of an open one.")]
        [SerializeField] [Range(0f, 1f)] private float _lockedShare = 0.45f;
        [Tooltip("How much thinner the clouds are over the chosen region, so its land reads before the view flies in.")]
        [SerializeField] [Range(0f, 1f)] private float _regionCloudClear = 0.65f;

        public Maps High => _high;
        public Maps Low => _low;
        public int FirstRegionId => _firstRegionId;
        public float RegionValueStep => _regionValueStep;
        public Color Emission => _emission;
        public float MapDayEmission => _mapDayEmission;
        public float MapRelief => _mapRelief;
        public float CutsceneRelief => _cutsceneRelief;
        public float CloudWisps => _cloudWisps;
        public float SeaLift => _seaLift;
        public float MapCloudShare => _mapCloudShare;
        public float CutsceneCloudShare => _cutsceneCloudShare;
        public float Grain => _grain;
        public float ColonyLights => _colonyLights;
        public float RegionLift => _regionLift;
        public float RegionDim => _regionDim;
        public float RegionRim => _regionRim;
        public float LockedShare => _lockedShare;
        public float RegionCloudClear => _regionCloudClear;

        /// <summary>True when the High maps are all there (the Low size falls back to them).</summary>
        public bool IsComplete => _high != null && _high.IsComplete;

        /// <summary>The maps for a quality profile: the Low size when asked for and complete, the High one otherwise.</summary>
        public Maps For(bool lowDetail) => lowDetail && _low != null && _low.IsComplete ? _low : _high;

        /// <summary>The id the region map gives a campaign region.</summary>
        public int RegionId(int regionIndex) => _firstRegionId + regionIndex;

        private void OnValidate()
        {
            // An empty set is a fresh asset the builder is about to fill; a half-filled one is a broken import.
            if (!IsComplete && _high != null && _high.Albedo != null)
            {
                Debug.LogWarning($"Planet texture set '{name}' misses High maps; the planet falls back to its painted globe.", this);
            }
        }
    }
}
