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

            /// <summary>The kerb ends are asymmetric pieces of their own, mirrored for the other side; false when an end
            /// stands in with the symmetric straight (round 17's continuous kerb ends flush), which needs no mirror.</summary>
            public bool MirrorEnds = true;

            /// <summary>Story set pieces (a seal in its states, a wreck), export paths; imported, placed by later code only.</summary>
            public string[] Landmarks = new string[0];

            /// <summary>The bendable bank segments swept along the pools' smoothed shores (README round 24b), kit names.</summary>
            public string[] BankSweeps = { "BankSweep_A", "BankSweep_B", "BankSweep_C" };

            /// <summary>The loose shore rocks laid on the swept bank's joints, kit name.</summary>
            public string BankAccent = "BankAccent";

            /// <summary>Colour of the glints in the ground (mask A).</summary>
            public Color Sparkle = new Color(1f, 0.7f, 0.95f);

            // A name with a folder in it is a full export path: a piece or layer this continent shares with another.
            public string PieceAsset(string piece) => piece.Contains("/") ? piece : $"{Folder}/{Prefix}{piece}";
            public string TilingAsset(string set) => set.Contains("/") ? set : $"{Folder}/{BlenderSync.TilingFolder}/{Prefix}{set}";
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
            { "BuildPadTile", 1.5f }, { "RoadEdge_Straight", 0.9f }, { "RoadEdge_OuterCorner", 0.9f }, { "RoadEdge_InnerCorner", 0.9f },
            { "RoadEdge_EndCap", 0.9f }, { "RoadEdge_PlazaJoin", 0.9f }, { "Silhouette_CrystalTower", 1.2f }, { "Silhouette_Ridge", 1.2f }, { "Silhouette_Mesa", 1.2f },
            // Round 17, tuned on the stage 4b screenshots (env_*): the hottest decor drew the eye from the units, as the
            // crystals did on continent 1.
            { "Ash_Flora_EmberBloom", 1.6f }, { "Ash_Decor_LavaRock", 1.6f }, { "North_Decor_FrozenCrystal", 1.4f },
            { "South_Decor_UnstableCrystal", 1.5f }, { "Core_Decor_CorruptedCrystal", 1f }, { "Core_Decor_CrystalPillar", 1.6f },
        };

        // Flat or tiny pieces that would only add shadow passes.
        private static readonly HashSet<string> NoShadow = new HashSet<string>
        {
            "BuildPadTile", "Scatter_Pebbles", "Scatter_CrystalSplinters", "Scatter_ShellFragments", "CrystalVein",
            "Rift_1x1", "Rift_2x1", "Rift_3x1", "Rift_Cap", "Silhouette_CrystalTower", "Silhouette_Ridge", "Silhouette_Mesa",
            // Round 17: the flat veins and the scatter of continents 2-6.
            "Decor_CrackedVein", "SealVein_Straight", "SealVein_Branch", "Scatter_Cinders", "Scatter_IceChips", "Scatter_Twigs",
            "Scatter_Shards", "Scatter_CrystalGrit",
        };

        private static readonly Color CrystalPink = new Color(1f, 0.5f, 0.9f);
        private static readonly Color FloraLime = new Color(0.75f, 1f, 0.35f);
        private static readonly Color EggAmber = new Color(1f, 0.55f, 0.25f);

        // The light pools of continents 2-6, in the glow colour of each decor family (README round 17).
        private static readonly Color AshEmber = new Color(1f, 0.45f, 0.15f);
        private static readonly Color AshBloom = new Color(1f, 0.62f, 0.25f);
        private static readonly Color NorthIce = new Color(0.55f, 0.85f, 1f);
        private static readonly Color MireTeal = new Color(0.3f, 1f, 0.8f);
        private static readonly Color MireFungus = new Color(0.7f, 1f, 0.45f);
        private static readonly Color SouthCrystal = new Color(0.65f, 0.72f, 1f);
        private static readonly Color CoreViolet = new Color(0.6f, 0.42f, 0.9f);
        private static readonly Color SealGold = new Color(1f, 0.78f, 0.35f);

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

            // Round 17: continents 2-6, the same kit names under their prefix. No level uses them yet, so no theme points
            // at them (import stage 4b). Their element layer lies under the terrain each continent is about.
            Round17("C2", "AshenReach", "AshenReach", "Ash_", "Sand", "Road", TerrainKind.Quicksand, new Color(1f, 0.6f, 0.3f), new[]
            {
                Decor(PropKind.Rock, AshEmber, "Decor_LavaRock"), Decor(PropKind.Boulder, AshEmber, "Decor_LavaRock"),
                Decor(PropKind.Crystal, AshEmber, "Decor_Fulgurite"), Decor(PropKind.Column, AshEmber, "Decor_Fulgurite"),
                Decor(PropKind.Spike, AshEmber, "Decor_Fulgurite"), Decor(PropKind.Spire, AshEmber, "Decor_Fulgurite"),
                Decor(PropKind.IceShard, AshEmber, "Decor_Fulgurite"), Decor(PropKind.GlowTree, AshEmber, "Flora_CharredTree"),
                Decor(PropKind.DeadTree, AshEmber, "Flora_CharredTree"), Decor(PropKind.Tendril, AshEmber, "Flora_CharredTree"),
                Decor(PropKind.Frond, AshBloom, "Flora_EmberBloom"), Decor(PropKind.Mushroom, AshBloom, "Flora_EmberBloom"),
                Decor(PropKind.Reed, AshBloom, "Flora_EmberBloom"), Decor(PropKind.Scree, AshEmber, "Decor_AshDune"),
                Decor(PropKind.Snowdrift, AshEmber, "Decor_AshDune"),
            }, "Scatter_Cinders"),
            Round17("C3", "NorthernCrown", "NorthernCrown", "North_", "Snow", "Road", TerrainKind.Ice, new Color(0.7f, 0.9f, 1f), new[]
            {
                Decor(PropKind.Rock, NorthIce, "Decor_RimeBoulder"), Decor(PropKind.Boulder, NorthIce, "Decor_RimeBoulder"),
                Decor(PropKind.Crystal, NorthIce, "Decor_FrozenCrystal"), Decor(PropKind.GlowTree, NorthIce, "Decor_FrozenCrystal"),
                Decor(PropKind.Column, NorthIce, "Decor_IceShard"), Decor(PropKind.Spike, NorthIce, "Decor_IceShard"),
                Decor(PropKind.Spire, NorthIce, "Decor_IceShard"), Decor(PropKind.IceShard, NorthIce, "Decor_IceShard"),
                Decor(PropKind.DeadTree, NorthIce, "Decor_IceShard"), Decor(PropKind.Frond, NorthIce, "Flora_FrostFern"),
                Decor(PropKind.Tendril, NorthIce, "Flora_FrostFern"), Decor(PropKind.Mushroom, NorthIce, "Flora_FrostFern"),
                Decor(PropKind.Reed, NorthIce, "Flora_FrostFern"), Decor(PropKind.Scree, NorthIce, "Decor_Snowdrift"),
                Decor(PropKind.Snowdrift, NorthIce, "Decor_Snowdrift"),
            }, "Scatter_IceChips"),
            Round17("C4", "Mirewood", "Mirewood", "Mire_", "Mud", "Road", TerrainKind.Bog, new Color(0.5f, 1f, 0.85f), new[]
            {
                Decor(PropKind.Rock, MireTeal, "Decor_MossStone"), Decor(PropKind.Boulder, MireTeal, "Decor_MossStone"),
                Decor(PropKind.Column, MireTeal, "Decor_MossStone"), Decor(PropKind.IceShard, MireTeal, "Decor_MossStone"),
                Decor(PropKind.Snowdrift, MireTeal, "Decor_MossStone"), Decor(PropKind.Crystal, MireFungus, "Decor_Fungus"),
                Decor(PropKind.Mushroom, MireFungus, "Decor_Fungus"), Decor(PropKind.GlowTree, MireTeal, "Flora_DeadTree"),
                Decor(PropKind.DeadTree, MireTeal, "Flora_DeadTree"), Decor(PropKind.Spire, MireTeal, "Flora_DeadTree"),
                Decor(PropKind.Spike, MireTeal, "Flora_DeadTree"), Decor(PropKind.Tendril, MireTeal, "Flora_Reed"),
                Decor(PropKind.Frond, MireTeal, "Flora_Reed"), Decor(PropKind.Reed, MireTeal, "Flora_Reed"),
                Decor(PropKind.Scree, MireTeal, "Decor_FogVent"),
            }, "Scatter_Twigs", storyLandmarks: new[] { BlenderStoryCatalog.Wreck }),
            Round17("C5", "SouthernCrown", "SouthernCrown", "South_", "CrackedIce", "Road", TerrainKind.Ice, new Color(0.75f, 0.8f, 1f), new[]
            {
                Decor(PropKind.Rock, SouthCrystal, "Decor_MixedBoulder"), Decor(PropKind.Boulder, SouthCrystal, "Decor_MixedBoulder"),
                Decor(PropKind.Mushroom, SouthCrystal, "Decor_MixedBoulder"), Decor(PropKind.Crystal, SouthCrystal, "Decor_UnstableCrystal"),
                Decor(PropKind.Frond, SouthCrystal, "Decor_UnstableCrystal"), Decor(PropKind.Tendril, SouthCrystal, "Decor_UnstableCrystal"),
                Decor(PropKind.IceShard, SouthCrystal, "Decor_UnstableCrystal"), Decor(PropKind.Reed, SouthCrystal, "Decor_UnstableCrystal"),
                Decor(PropKind.GlowTree, SouthCrystal, "Decor_ShatteredPillar"), Decor(PropKind.Column, SouthCrystal, "Decor_ShatteredPillar"),
                Decor(PropKind.Spike, SouthCrystal, "Decor_ShatteredPillar"), Decor(PropKind.Spire, SouthCrystal, "Decor_ShatteredPillar"),
                Decor(PropKind.DeadTree, SouthCrystal, "Decor_ShatteredPillar"), Decor(PropKind.Snowdrift, SouthCrystal, "Decor_PressureRidge"),
                Decor(PropKind.Scree, SouthCrystal, "Decor_CrackedVein"),
            }, "Scatter_Shards"),
            // The Core: the plateau set is the plazas and the base area, the corrupted ground lies round its pools; the
            // seal veins stand in for the crystal veins, and the seal's three states are its landmarks.
            Round17("C6", "TheCore", "TheCore", "Core_", "GroundAlt", "Plateau", TerrainKind.Bog, new Color(0.85f, 0.55f, 1f), new[]
            {
                Decor(PropKind.Rock, CoreViolet, "Decor_HexOutcrop"), Decor(PropKind.Boulder, CoreViolet, "Decor_HexOutcrop"),
                Decor(PropKind.Snowdrift, CoreViolet, "Decor_HexOutcrop"), Decor(PropKind.Crystal, CoreViolet, "Decor_CorruptedCrystal"),
                Decor(PropKind.Mushroom, CoreViolet, "Decor_CorruptedCrystal"), Decor(PropKind.Frond, CoreViolet, "Decor_CorruptedCrystal"),
                Decor(PropKind.GlowTree, CoreViolet, "Decor_CrystalPillar"), Decor(PropKind.Column, CoreViolet, "Decor_CrystalPillar"),
                Decor(PropKind.Spire, CoreViolet, "Decor_CrystalPillar"), Decor(PropKind.DeadTree, CoreViolet, "Decor_CrystalPillar"),
                Decor(PropKind.Tendril, SealGold, "Decor_SealShard"), Decor(PropKind.Spike, SealGold, "Decor_SealShard"),
                Decor(PropKind.IceShard, SealGold, "Decor_SealShard"), Decor(PropKind.Reed, SealGold, "Decor_SealShard"),
                Decor(PropKind.Scree, SealGold, "SealVein_Branch"),
            }, "Scatter_CrystalGrit", vein: "SealVein_Straight", landmarks: new[] { "Seal_Intact", "Seal_Cracked", "Seal_Broken", "SealVein_Branch" }),
        };

        // A round-17 continent: its own tiling sets, kerb, banks, cliffs, decor and scatter under its prefix; the build pad,
        // gate plinth, nest shells, eggs and spawn openings shared with Crystal Plains (README: "ours" on every continent,
        // and the openings are tinted by the nest's element). Pieces it has no export for stand in deliberately: every kerb
        // end and plaza edge is the continuous straight (it ends flush), a plaza corner the inner corner stone. It has no
        // terrace, rift or silhouette pieces, so those are left out (raised ground and rifts keep the painted terrain).
        private static ContinentRow Round17(string key, string setName, string folder, string prefix, string element, string plaza,
            TerrainKind elementTerrain, Color sparkle, DecorRow[] decor, string scatter, string vein = null, string[] landmarks = null, string[] storyLandmarks = null)
        {
            string root = $"{BlenderSync.EnvironmentFolder}/{folder}";
            var pieces = new Dictionary<EnvironmentPieceKind, string>
            {
                { EnvironmentPieceKind.RoadEdgeStraight, "RoadEdge_Straight" },
                { EnvironmentPieceKind.RoadEdgeOuterCorner, "RoadEdge_OuterCorner" },
                { EnvironmentPieceKind.RoadEdgeInnerCorner, "RoadEdge_InnerCorner" },
                { EnvironmentPieceKind.RoadEdgeEndCap, "RoadEdge_Straight" },
                { EnvironmentPieceKind.RoadEdgePlazaJoin, "RoadEdge_Straight" },
                { EnvironmentPieceKind.PlazaEdgeStraight, "RoadEdge_Straight" },
                { EnvironmentPieceKind.PlazaEdgeCorner, "RoadEdge_InnerCorner" },
                { EnvironmentPieceKind.BuildPad, "Env/BuildPadTile" },
                { EnvironmentPieceKind.GatePlinth, "Env/GatePlinth" },
                { EnvironmentPieceKind.CliffStraight, "Cliff_Straight" },
                { EnvironmentPieceKind.CliffOuterCorner, "Cliff_OuterCorner" },
                { EnvironmentPieceKind.CliffInnerCorner, "Cliff_InnerCorner" },
                { EnvironmentPieceKind.BankStraight, "Bank_Straight" },
                { EnvironmentPieceKind.BankOuterCorner, "Bank_OuterCorner" },
                { EnvironmentPieceKind.BankInnerCorner, "Bank_InnerCorner" },
            };
            if (vein != null)
            {
                pieces[EnvironmentPieceKind.CrystalVein] = vein;
            }

            var row = new ContinentRow
            {
                Key = key,
                SetName = setName,
                Folder = root,
                Prefix = prefix,
                Ground = "Ground",
                GroundAlt = "GroundAlt",
                Road = "Road",
                Plaza = plaza,
                Cliff = "Cliff",
                Element = element,
                ElementTerrain = elementTerrain,
                Pieces = pieces,
                Decor = decor,
                Scatter = new[] { $"{root}/{prefix}{scatter}" },
                NestScatter = new[] { "Env/Scatter_ShellFragments" },
                Silhouettes = new string[0],
                Origins = SharedOrigins(),
                MirrorEnds = false,
                Sparkle = sparkle,
            };

            // Decor and landmarks are named without the prefix; the egg clusters are the aliens' own on every continent.
            var withFolder = new List<DecorRow>(decor.Length + 1);
            foreach (DecorRow entry in decor)
            {
                withFolder.Add(new DecorRow(entry.Kind, entry.Glow, row.PieceAsset(entry.Assets[0])));
            }

            withFolder.Add(new DecorRow(PropKind.Egg, EggAmber, "Env/Egg_Cluster"));
            row.Decor = withFolder.ToArray();
            var marks = new List<string>();
            foreach (string landmark in landmarks ?? new string[0])
            {
                marks.Add(row.PieceAsset(landmark));
            }

            // A story model of another round (the Pioneer-1 wreck) keeps its own export folder.
            marks.AddRange(storyLandmarks ?? new string[0]);

            row.Landmarks = marks.ToArray();
            return row;
        }

        private static DecorRow Decor(PropKind kind, Color glow, string asset) => new DecorRow(kind, glow, asset);

        private static (TerrainKind, string, bool, float)[] SharedOrigins() => new[]
        {
            (TerrainKind.Ground, "Burrow_A", false, 0.75f), (TerrainKind.Raised, "Burrow_A", false, 0.75f), (TerrainKind.Rift, "Burrow_A", false, 0.75f),
            (TerrainKind.Water, "Env/Origin_Water", true, 1f), (TerrainKind.Lava, "Env/Origin_Lava", true, 1f),
            (TerrainKind.Quicksand, "Env/Origin_Quicksand", true, 1f), (TerrainKind.Ice, "Env/Origin_Ice", true, 1f),
            (TerrainKind.Bog, "Env/Origin_Bog", true, 1f),
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
                assets.UnionWith(row.Landmarks);
                foreach (string sweep in row.BankSweeps)
                {
                    assets.Add(row.PieceAsset(sweep));
                }

                assets.Add(row.PieceAsset(row.BankAccent));
                foreach ((TerrainKind _, string asset, bool _, float _) in row.Origins)
                {
                    assets.Add(asset);
                }
            }

            return assets;
        }

        // Pieces whose material darkens its albedo: the kerb's light stones and glowing tips read as a row of teeth at play
        // zoom (MODEL_TODO, curator round 8), so it sits closer to the road's value and glows less.
        private static readonly Dictionary<string, float> AlbedoByAsset = new Dictionary<string, float>
        {
            { "RoadEdge_Straight", 0.8f }, { "RoadEdge_OuterCorner", 0.8f }, { "RoadEdge_InnerCorner", 0.8f },
            { "RoadEdge_EndCap", 0.8f }, { "RoadEdge_PlazaJoin", 0.8f },
        };

        public static float AlbedoOf(string asset) => AlbedoByAsset.TryGetValue(KitName(asset), out float albedo) ? albedo : 1f;

        /// <summary>The build pad: calm by default, lit only while a tower is being placed (BuildPadHighlight).</summary>
        public static bool IsPad(string asset) => KitName(asset) == "BuildPadTile";

        // A continent may tune one of its own pieces by its full name; otherwise the kit name's look holds on every continent.
        public static float GlowOf(string asset) =>
            GlowByAsset.TryGetValue(Name(asset), out float glow) || GlowByAsset.TryGetValue(KitName(asset), out glow) ? glow : KitGlow;

        public static bool CastsShadow(string asset) => !NoShadow.Contains(KitName(asset));

        public static bool Sways(string asset) => KitName(asset).StartsWith("Flora_", System.StringComparison.Ordinal);

        /// <summary>The kit name of an export without its continent's prefix (<c>Env/AshenReach/Ash_Flora_CharredTree</c> is <c>Flora_CharredTree</c>).</summary>
        public static string KitName(string asset)
        {
            string name = Name(asset);
            foreach (ContinentRow row in Continents)
            {
                if (row.Prefix.Length > 0 && name.StartsWith(row.Prefix, System.StringComparison.Ordinal))
                {
                    return name.Substring(row.Prefix.Length);
                }
            }

            return name;
        }

        /// <summary>The file name part of an export path (<c>Env/Flora_Frond</c> is <c>Flora_Frond</c>).</summary>
        public static string Name(string asset)
        {
            int slash = asset.LastIndexOf('/');
            return slash >= 0 ? asset.Substring(slash + 1) : asset;
        }
    }
}
