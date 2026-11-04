using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The Blender environment exports per continent (ArtSource README rounds 8, 14 and 17): where the export folders are,
    /// the tiling sets per ground role, which export is which kit piece, which meshes replace which procedural decoration
    /// kind, scatter, silhouettes and spawn openings, plus the look per material (glow, shadows, sway). A continent is one
    /// row; continents 2-6 share the kit names under their own prefix, so adding one is a new row, not new code.
    /// </summary>
    internal static class BlenderEnvironmentCatalog
    {
        /// <summary>One decoration kind's stand-ins.</summary>
        public readonly struct DecorRow
        {
            public DecorRow(PropKind kind, Color glow, params string[] assets)
            {
                Kind = kind;
                Glow = glow;
                Assets = assets;
            }

            public PropKind Kind { get; }
            public Color Glow { get; }

            /// <summary>Export paths under <c>ArtSource/Exports</c> (a kit decor under <c>Env/</c>, the round-2 rocks at the root).</summary>
            public string[] Assets { get; }
        }

        /// <summary>One continent's environment.</summary>
        public sealed class ContinentRow
        {
            public string Key;
            public string SetName;
            public string Folder;
            public string Prefix;
            public string Ground;
            public string GroundAlt;
            public string Road;
            public string Plaza;
            public string Cliff;
            public string Element;
            public TerrainKind ElementTerrain;
            public Dictionary<EnvironmentPieceKind, string> Pieces;
            public DecorRow[] Decor;
            public string[] Scatter;
            public string[] NestScatter;
            public string[] Silhouettes;
            public (TerrainKind Terrain, string Asset, bool Tint, float Scale)[] Origins;

            public string PieceAsset(string piece) => $"{Folder}/{Prefix}{piece}";
            public string TilingAsset(string set) => $"{Folder}/{BlenderSync.TilingFolder}/{Prefix}{set}";
        }

        public const string DetailNormal = "Detail_Rock_Normal.png";

        // The kit glows at about 2.2 (README round 8), the rift at 2.4. The crystals burn lower than the README so their
        // facets show (curator, MODEL_TODO section 7); rocks, flora and pads lower too, tuned on play-zoom screenshots
        // (import stage 4a): at 2.2 the pink veins of a whole wilderness of rocks drew the eye away from the units.
        public const float KitGlow = 2.2f;
        private static readonly Dictionary<string, float> GlowByAsset = new Dictionary<string, float>
        {
            { "Rift_1x1", 2.4f }, { "Rift_2x1", 2.4f }, { "Rift_3x1", 2.4f }, { "Rift_Cap", 2.4f },
            { "Crystal_A", 1.2f }, { "Crystal_B", 1.2f }, { "Crystal_C", 1.2f }, { "Scatter_CrystalSplinters", 1.4f },
            { "Rock_A", 1.3f }, { "Rock_B", 1.3f }, { "Rock_C", 1.3f }, { "Decor_Boulder", 1.4f }, { "Decor_Column", 1.4f },
            { "Decor_Spike", 1.4f }, { "Decor_Spire", 1.4f }, { "Scree_Patch", 1.4f },
            { "Flora_Frond", 1.6f }, { "Flora_Tendril", 1.6f }, { "Flora_GlowTree", 1.6f }, { "Flora_Mushroom", 1.6f },
            { "BuildPadTile", 1.5f }, { "Silhouette_CrystalTower", 1.2f }, { "Silhouette_Ridge", 1.2f }, { "Silhouette_Mesa", 1.2f },
        };

        // Flat or tiny pieces that would only add shadow passes.
        private static readonly HashSet<string> NoShadow = new HashSet<string>
        {
            "BuildPadTile", "Scatter_Pebbles", "Scatter_CrystalSplinters", "Scatter_ShellFragments", "CrystalVein",
            "Rift_1x1", "Rift_2x1", "Rift_3x1", "Rift_Cap", "Silhouette_CrystalTower", "Silhouette_Ridge", "Silhouette_Mesa",
        };

        private static readonly Color CrystalPink = new Color(1f, 0.5f, 0.9f);
        private static readonly Color FloraLime = new Color(0.75f, 1f, 0.35f);
        private static readonly Color EggAmber = new Color(1f, 0.55f, 0.25f);

        public static readonly ContinentRow[] Continents =
        {
            new ContinentRow
            {
                Key = "C1",
                SetName = "CrystalPlains",
                Folder = BlenderSync.EnvironmentFolder,
                Prefix = string.Empty,
                Ground = "Ground",
                GroundAlt = "GroundFlora",
                Road = "Road",
                Plaza = "Plaza",
                Cliff = "Cliff",
                Element = "RiftRock",
                ElementTerrain = TerrainKind.Rift,
                Pieces = new Dictionary<EnvironmentPieceKind, string>
                {
                    { EnvironmentPieceKind.RoadEdgeStraight, "RoadEdge_Straight" },
                    { EnvironmentPieceKind.RoadEdgeOuterCorner, "RoadEdge_OuterCorner" },
                    { EnvironmentPieceKind.RoadEdgeInnerCorner, "RoadEdge_InnerCorner" },
                    { EnvironmentPieceKind.RoadEdgeEndCap, "RoadEdge_EndCap" },
                    { EnvironmentPieceKind.RoadEdgePlazaJoin, "RoadEdge_PlazaJoin" },
                    { EnvironmentPieceKind.PlazaEdgeStraight, "PlazaEdge_Straight" },
                    { EnvironmentPieceKind.PlazaEdgeCorner, "PlazaEdge_Corner" },
                    { EnvironmentPieceKind.BuildPad, "BuildPadTile" },
                    { EnvironmentPieceKind.GatePlinth, "GatePlinth" },
                    { EnvironmentPieceKind.CliffStraight, "Cliff_Straight" },
                    { EnvironmentPieceKind.CliffOuterCorner, "Cliff_OuterCorner" },
                    { EnvironmentPieceKind.CliffInnerCorner, "Cliff_InnerCorner" },
                    { EnvironmentPieceKind.BankStraight, "Bank_Straight" },
                    { EnvironmentPieceKind.BankOuterCorner, "Bank_OuterCorner" },
                    { EnvironmentPieceKind.BankInnerCorner, "Bank_InnerCorner" },
                    { EnvironmentPieceKind.TerraceStep, "TerraceStep" },
                    { EnvironmentPieceKind.TerraceStepOuter, "TerraceStep_Outer" },
                    { EnvironmentPieceKind.RiftShort, "Rift_1x1" },
                    { EnvironmentPieceKind.RiftMedium, "Rift_2x1" },
                    { EnvironmentPieceKind.RiftLong, "Rift_3x1" },
                    { EnvironmentPieceKind.RiftCap, "Rift_Cap" },
                    { EnvironmentPieceKind.CrystalVein, "CrystalVein" },
                },
                Decor = new[]
                {
                    new DecorRow(PropKind.Rock, CrystalPink, "Rock_A", "Rock_B", "Rock_C"),
                    new DecorRow(PropKind.Boulder, CrystalPink, "Env/Decor_Boulder"),
                    new DecorRow(PropKind.Crystal, CrystalPink, "Crystal_A", "Crystal_B", "Crystal_C"),
                    new DecorRow(PropKind.Column, CrystalPink, "Env/Decor_Column"),
                    new DecorRow(PropKind.Spike, CrystalPink, "Env/Decor_Spike"),
                    new DecorRow(PropKind.Spire, CrystalPink, "Env/Decor_Spire"),
                    new DecorRow(PropKind.Frond, FloraLime, "Env/Flora_Frond"),
                    new DecorRow(PropKind.Tendril, FloraLime, "Env/Flora_Tendril"),
                    new DecorRow(PropKind.GlowTree, CrystalPink, "Env/Flora_GlowTree"),
                    new DecorRow(PropKind.Mushroom, FloraLime, "Env/Flora_Mushroom"),
                    new DecorRow(PropKind.Egg, EggAmber, "Env/Egg_Cluster"),
                    new DecorRow(PropKind.Scree, CrystalPink, "Env/Scree_Patch"),
                },
                Scatter = new[] { "Env/Scatter_Pebbles", "Env/Scatter_CrystalSplinters", "Env/Scatter_Pebbles" },
                NestScatter = new[] { "Env/Scatter_ShellFragments" },
                Silhouettes = new[] { "Env/Silhouette_CrystalTower", "Env/Silhouette_Ridge", "Env/Silhouette_Mesa" },
                Origins = new[]
                {
                    // The burrow is 2.8 across; at 0.75 it fills a rift's mouth without swallowing the cells around it.
                    (TerrainKind.Ground, "Burrow_A", false, 0.75f), (TerrainKind.Raised, "Burrow_A", false, 0.75f), (TerrainKind.Rift, "Burrow_A", false, 0.75f),
                    (TerrainKind.Water, "Env/Origin_Water", true, 1f), (TerrainKind.Lava, "Env/Origin_Lava", true, 1f),
                    (TerrainKind.Quicksand, "Env/Origin_Quicksand", true, 1f), (TerrainKind.Ice, "Env/Origin_Ice", true, 1f),
                    (TerrainKind.Bog, "Env/Origin_Bog", true, 1f),
                },
            },
        };

        /// <summary>Every export path the rows use (meshes and tiling sets), for the sync.</summary>
        public static IEnumerable<string> SyncedAssets()
        {
            var assets = new HashSet<string>();
            foreach (ContinentRow row in Continents)
            {
                foreach (string set in new[] { row.Ground, row.GroundAlt, row.Road, row.Plaza, row.Cliff, row.Element })
                {
                    assets.Add(row.TilingAsset(set));
                }

                foreach (string piece in row.Pieces.Values)
                {
                    assets.Add(row.PieceAsset(piece));
                }

                foreach (DecorRow decor in row.Decor)
                {
                    assets.UnionWith(decor.Assets);
                }

                assets.UnionWith(row.Scatter);
                assets.UnionWith(row.NestScatter);
                assets.UnionWith(row.Silhouettes);
                foreach ((TerrainKind _, string asset, bool _, float _) in row.Origins)
                {
                    assets.Add(asset);
                }
            }

            return assets;
        }

        public static float GlowOf(string asset) => GlowByAsset.TryGetValue(Name(asset), out float glow) ? glow : KitGlow;

        public static bool CastsShadow(string asset) => !NoShadow.Contains(Name(asset));

        public static bool Sways(string asset) => Name(asset).StartsWith("Flora_", System.StringComparison.Ordinal);

        /// <summary>The file name part of an export path (<c>Env/Flora_Frond</c> is <c>Flora_Frond</c>).</summary>
        public static string Name(string asset)
        {
            int slash = asset.LastIndexOf('/');
            return slash >= 0 ? asset.Substring(slash + 1) : asset;
        }
    }
}
