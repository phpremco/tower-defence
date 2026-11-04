using System;
using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The Blender liquid and mist textures (ArtSource README round 24, <c>Liquids_report.json</c>): which files the game
    /// reads per kind, how each imports, and the look of each kind in <c>Game/Liquid</c> (tile sizes, scrolls and the
    /// Crystal Plains palette from the report; the water lifted about twice, as the report's near-black sea read as a
    /// hole on the dark levels). One row per kind, so a kind is added as data, not code.
    /// </summary>
    internal static class BlenderLiquidCatalog
    {
        /// <summary>The export folder (and the folder under the Blender target root).</summary>
        public const string Folder = "Liquids";

        public const string Report = "Liquids_report.json";
        public const string MistFolder = "Mist";
        public static readonly string[] MistLayers = { "Mist_Layer_A", "Mist_Layer_B", "Mist_Layer_C" };

        // README round 24: heights, densities, tiles (world units) and drifts of the three mist sheets.
        public static readonly (float Height, float Alpha, float Tile, Vector2 Scroll)[] MistLook =
        {
            (0.12f, 0.16f, 7f, new Vector2(0.012f, 0.003f)),
            (0.32f, 0.1f, 5f, new Vector2(0.025f, 0.004f)),
            (0.6f, 0.06f, 3.5f, new Vector2(0.04f, -0.01f)),
        };

        public static readonly Color MistColour = new Color(0.55f, 0.6f, 0.75f);

        /// <summary>A texture of a kind and the material slot it fills.</summary>
        public readonly struct Map
        {
            public Map(string slot, string file)
            {
                Slot = slot;
                File = file;
            }

            public string Slot { get; }
            public string File { get; }
        }

        /// <summary>One kind's surface: its keyword, maps and numbers.</summary>
        public sealed class Row
        {
            public string Name;
            public TerrainKind Kind;
            public string Keyword;
            public Map[] Maps;
            public Dictionary<string, Vector4> Vectors = new Dictionary<string, Vector4>();
            public Dictionary<string, Color> Colours = new Dictionary<string, Color>();
            public Dictionary<string, float> Floats = new Dictionary<string, float>();
            public bool Refracts;
            public Color BankLight = Color.black;
        }

        public static readonly Row[] Rows =
        {
            new Row
            {
                Name = "Water", Kind = TerrainKind.Water, Keyword = "_LIQUID_WATER", Refracts = true,
                Maps = new[]
                {
                    new Map("_NormalA", "Water_Normal_Swell"), new Map("_NormalB", "Water_Normal_Ripple"), new Map("_Flipbook", "Water_Caustics_Flip"),
                    new Map("_StaticCaustics", "Water_Caustics"), new Map("_MaskMap", "Water_Foam"), new Map("_ProfileMap", "Water_ShoreProfile"),
                },
                Vectors =
                {
                    { "_NormalTiles", new Vector4(4.8f, 1.65f, 0.7f, 40f) }, { "_NormalScroll", new Vector4(0.035f, 0.012f, -0.05f, 0.07f) },
                    { "_Tiles", new Vector4(1.8f, 1.8f, 1f, 6.3f) }, { "_Alpha", new Vector4(0.72f, 0.96f, 0f, 0f) },
                    { "_Smoothness", new Vector4(0.95f, 0.45f, 0f, 0f) }, { "_Waves", new Vector4(0.025f, 1.4f, 1.6f, 0f) },
                },
                Colours =
                {
                    { "_ShallowColor", new Color(0.07f, 0.24f, 0.32f) }, { "_DeepColor", new Color(0.02f, 0.08f, 0.15f) },
                    { "_FoamColor", new Color(0.8f, 0.88f, 0.95f) }, { "_CausticColor", new Color(0.55f, 0.85f, 1f) }, { "_GlowColor", new Color(0.25f, 0.55f, 1f) },
                },
                Floats = { { "_DepthRange", 0.8f }, { "_Refraction", 0.03f }, { "_EmissionStrength", 2f } },
            },
            new Row
            {
                Name = "Lava", Kind = TerrainKind.Lava, Keyword = "_LIQUID_LAVA", BankLight = new Color(1f, 0.45f, 0.12f),
                Maps = new[]
                {
                    new Map("_AlbedoA", "Lava_CrustA_Albedo"), new Map("_AlbedoB", "Lava_CrustB_Albedo"), new Map("_NormalA", "Lava_CrustA_Normal"),
                    new Map("_NormalB", "Lava_CrustB_Normal"), new Map("_HeatA", "Lava_CrustA_Heat"), new Map("_HeatB", "Lava_CrustB_Heat"), new Map("_FlowMap", "Lava_Flow"),
                },
                Vectors =
                {
                    { "_Tiles", new Vector4(5.5f, 4.4f, 8.25f, 1f) }, { "_Scroll", new Vector4(0.018f, 0.006f, -0.012f, 0.014f) },
                    { "_Smoothness", new Vector4(0.55f, 0.25f, 0f, 0f) }, { "_Pulse", new Vector4(0.15f, 1.1f, 0f, 0f) }, { "_Waves", new Vector4(0.02f, 2.6f, 0.45f, 0f) },
                },
                Colours =
                {
                    { "_ShallowColor", Color.white }, { "_Ramp1", new Color(0.4f, 0.03f, 0.005f) }, { "_Ramp2", new Color(1.6f, 0.35f, 0.05f) },
                    { "_Ramp3", new Color(3.2f, 1.3f, 0.25f) }, { "_Ramp4", new Color(4f, 2.8f, 1.4f) },
                },
            },
            new Row
            {
                Name = "Bog", Kind = TerrainKind.Bog, Keyword = "_LIQUID_BOG",
                Maps = new[] { new Map("_NormalA", "Bog_Normal"), new Map("_MaskMap", "Bog_Scum") },
                Vectors =
                {
                    { "_NormalTiles", new Vector4(4.5f, 2.45f, 1f, 75f) }, { "_NormalScroll", new Vector4(0.004f, 0.002f, -0.003f, 0.003f) },
                    { "_Tiles", new Vector4(3.5f, 3.5f, 1f, 1f) }, { "_Scroll", new Vector4(0.002f, -0.001f, 0f, 0f) },
                    { "_Alpha", new Vector4(0.85f, 0.97f, 0f, 0f) }, { "_Smoothness", new Vector4(0.85f, 0.6f, 0f, 0f) }, { "_Waves", new Vector4(0.008f, 2.4f, 0.2f, 0f) },
                },
                Colours =
                {
                    { "_ShallowColor", new Color(0.06f, 0.09f, 0.04f) }, { "_DeepColor", new Color(0.01f, 0.016f, 0.01f) },
                    { "_FoamColor", new Color(0.13f, 0.2f, 0.06f) }, { "_GlowColor", new Color(0.35f, 1.2f, 0.6f) },
                },
                Floats = { { "_DepthRange", 0.8f } },
            },
            new Row
            {
                Name = "Quicksand", Kind = TerrainKind.Quicksand, Keyword = "_LIQUID_QUICKSAND",
                Maps = new[] { new Map("_AlbedoA", "Quicksand_Albedo"), new Map("_NormalA", "Quicksand_Normal"), new Map("_FlowMap", "Quicksand_Flow") },
                Vectors = { { "_Tiles", new Vector4(4.5f, 4.5f, 1f, 1f) }, { "_Smoothness", new Vector4(0.5f, 0.08f, 0f, 0f) }, { "_Waves", new Vector4(0.006f, 2f, 0.3f, 0f) } },
                Colours = { { "_ShallowColor", new Color(0.5f, 0.4f, 0.3f) * 2f }, { "_FoamColor", new Color(0.6f, 0.6f, 0.65f) * 2f } },
            },
            new Row
            {
                Name = "Ice", Kind = TerrainKind.Ice, Keyword = "_LIQUID_ICE",
                Maps = new[] { new Map("_AlbedoA", "Ice_Albedo"), new Map("_NormalA", "Ice_Normal"), new Map("_MaskMap", "Ice_Cracks"), new Map("_FrostMap", "Ice_Frost") },
                Vectors = { { "_Tiles", new Vector4(4.5f, 4.5f, 1f, 1f) }, { "_Alpha", new Vector4(0.88f, 0.97f, 0f, 0f) }, { "_Smoothness", new Vector4(0.92f, 0.35f, 0f, 0f) }, { "_Waves", Vector4.zero } },
                Colours =
                {
                    { "_ShallowColor", new Color(0.8f, 0.9f, 1f) }, { "_DeepColor", new Color(0.02f, 0.07f, 0.14f) },
                    { "_FoamColor", new Color(0.9f, 0.95f, 1f) }, { "_GlowColor", new Color(0.4f, 0.8f, 1f) },
                },
            },
        };

        /// <summary>The Core's corrupted pools: no terrain kind uses them yet, the material is ready in the set.</summary>
        public static readonly Row Corruption = new Row
        {
            Name = "Corruption", Kind = TerrainKind.Bog, Keyword = "_LIQUID_CORRUPTION",
            Maps = new[] { new Map("_NormalA", "Corruption_Normal"), new Map("_MaskMap", "Corruption_Veins") },
            Vectors =
            {
                { "_NormalTiles", new Vector4(5f, 3f, 1f, 50f) }, { "_NormalScroll", new Vector4(0.003f, 0.002f, -0.002f, 0.003f) },
                { "_Tiles", new Vector4(4f, 4f, 1f, 1f) }, { "_Smoothness", new Vector4(0.82f, 0.82f, 0f, 0f) }, { "_Waves", new Vector4(0.01f, 2.5f, 0.2f, 0f) },
            },
            Colours = { { "_ShallowColor", new Color(0.035f, 0.008f, 0.055f) }, { "_DeepColor", new Color(0.012f, 0f, 0.025f) }, { "_GlowColor", new Color(1.4f, 0.25f, 2.4f) } },
        };

        /// <summary>Every texture file the rows read, by kind folder (for the sync).</summary>
        public static IEnumerable<(string Folder, string[] Files)> Files()
        {
            foreach (Row row in AllRows())
            {
                var files = new List<string>();
                foreach (Map map in row.Maps)
                {
                    files.Add(map.File + ".png");
                }

                yield return (row.Name, files.ToArray());
            }

            var mist = new List<string>();
            foreach (string layer in MistLayers)
            {
                mist.Add(layer + ".png");
            }

            yield return (MistFolder, mist.ToArray());
        }

        public static IEnumerable<Row> AllRows()
        {
            foreach (Row row in Rows)
            {
                yield return row;
            }

            yield return Corruption;
        }

        public static bool IsNormal(string file) => file.Contains("_Normal");
        public static bool IsColour(string file) => file.EndsWith("_Albedo", StringComparison.Ordinal);
        public static bool IsFlow(string file) => file.EndsWith("_Flow", StringComparison.Ordinal);
        public static bool IsProfile(string file) => file == "Water_ShoreProfile";
        public static bool Clamps(string file) => file.EndsWith("_Flip", StringComparison.Ordinal) || file == "Quicksand_Vortex";
    }
}
