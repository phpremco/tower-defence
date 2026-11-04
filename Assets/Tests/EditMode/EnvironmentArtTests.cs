using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The imported environment (import stage 4a) as data and layout: every campaign level's theme wears its continent's
    /// set with the ground layers, the shader and every kit piece the layout asks for; the layout puts a kerb on every
    /// open road side (none on a gate's), a pad on every buildable cell and a plinth under every gate, turns each piece
    /// so its road or liquid lies on its frame's side, and keeps the border walls clear of the road ends and the base;
    /// the flora carries its sway colours and the decor replaces the procedural kinds the themes use.
    /// </summary>
    public sealed class EnvironmentArtTests
    {
        private const string LevelFolder = "Assets/_Project/Data/Levels";
        private const float Tolerance = 0.01f;

        private static readonly EnvironmentPieceKind[] Required =
        {
            EnvironmentPieceKind.RoadEdgeStraight, EnvironmentPieceKind.RoadEdgeOuterCorner, EnvironmentPieceKind.RoadEdgeInnerCorner,
            EnvironmentPieceKind.RoadEdgeEndCap, EnvironmentPieceKind.RoadEdgeEndCapMirrored, EnvironmentPieceKind.PlazaEdgeStraight,
            EnvironmentPieceKind.PlazaEdgeCorner, EnvironmentPieceKind.BuildPad, EnvironmentPieceKind.GatePlinth,
            EnvironmentPieceKind.CliffStraight, EnvironmentPieceKind.CliffInnerCorner, EnvironmentPieceKind.BankStraight,
            EnvironmentPieceKind.BankOuterCorner, EnvironmentPieceKind.BankInnerCorner, EnvironmentPieceKind.TerraceStep,
            EnvironmentPieceKind.TerraceStepOuter, EnvironmentPieceKind.RiftCap, EnvironmentPieceKind.CrystalVein,
        };

        [Test]
        public void EveryCampaignLevel_WearsAnEnvironmentSet_WithItsGroundAndKit()
        {
            foreach (LevelDefinition level in Levels())
            {
                EnvironmentSet set = level.Theme.Environment;
                Assert.IsNotNull(set, $"{level.name}: its theme has no environment set");
                Assert.IsTrue(set.HasGround, $"{set.name}: no ground shader or ground/road layer");
                Assert.AreEqual(EnvironmentShaders.Ground, set.GroundShader.name);
                foreach (EnvironmentLayer layer in new[] { set.Ground, set.GroundAlt, set.Road, set.Plaza, set.Cliff, set.Element })
                {
                    Assert.IsTrue(layer.IsValid, $"{set.name}: a ground layer lacks its maps");
                    Assert.AreEqual(TextureWrapMode.Repeat, layer.Albedo(false).wrapMode, $"{set.name}: a tiling set does not repeat");
                }

                foreach (EnvironmentPieceKind kind in Required)
                {
                    EnvironmentMesh piece = set.Piece(kind);
                    Assert.IsNotNull(piece, $"{set.name}: no {kind}");
                    Assert.AreEqual(EnvironmentShaders.Lit, piece.MaterialFor(false).shader.name, $"{set.name}: {kind} has the wrong shader");
                    Assert.IsTrue(piece.MaterialFor(false).enableInstancing && piece.MaterialFor(true).enableInstancing, $"{set.name}: {kind} is not instanced");
                }

                Assert.IsNotNull(set.Origin(TerrainKind.Rift), $"{set.name}: no burrow for the rift nests");
            }
        }

        [Test]
        public void Layout_PutsAKerbOnEveryOpenRoadSide_APadOnEveryBuildCell_AndAPlinthUnderEveryGate()
        {
            foreach (LevelDefinition level in Levels())
            {
                EnvironmentLayout layout = new EnvironmentLayout(level, null).Build(1.5f, 0.75f);
                GridMap grid = level.Grid;
                var gates = new HashSet<Vector2Int>();
                foreach (BlockadeSpot spot in level.Blockades)
                {
                    gates.UnionWith(level.Road.Rung(spot.Cell));
                }

                int openSides = 0;
                foreach (Vector2Int cell in level.Road.Cells)
                {
                    foreach (Vector2Int side in new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down })
                    {
                        Vector2Int beyond = cell + side;
                        openSides += !gates.Contains(cell) && grid.Contains(beyond) && !level.Road.IsRoad(beyond) ? 1 : 0;
                    }
                }

                int edges = 0;
                foreach (EnvironmentPieceKind kind in new[]
                         {
                             EnvironmentPieceKind.RoadEdgeStraight, EnvironmentPieceKind.RoadEdgeEndCap, EnvironmentPieceKind.RoadEdgeEndCapMirrored,
                             EnvironmentPieceKind.RoadEdgePlazaJoin, EnvironmentPieceKind.RoadEdgePlazaJoinMirrored, EnvironmentPieceKind.PlazaEdgeStraight,
                         })
                {
                    edges += layout.Count(kind);
                }

                Assert.AreEqual(openSides, edges, $"{level.name}: kerbs and open road sides differ");
                Assert.AreEqual(level.Blockades.Length, layout.Count(EnvironmentPieceKind.GatePlinth), $"{level.name}: a gate without plinth");
                Assert.GreaterOrEqual(layout.Count(EnvironmentPieceKind.RoadEdgeEndCap) + layout.Count(EnvironmentPieceKind.RoadEdgeEndCapMirrored), 2 * level.Blockades.Length, $"{level.name}: a gate without end caps");

                var build = new BuildGrid(level);
                int buildable = 0;
                for (int x = 0; x < grid.Size.x; x++)
                {
                    for (int y = 0; y < grid.Size.y; y++)
                    {
                        buildable += build.CanBuild(new Vector2Int(x, y)) ? 1 : 0;
                    }
                }

                Assert.AreEqual(buildable, layout.Count(EnvironmentPieceKind.BuildPad), $"{level.name}: pads and build cells differ");
            }
        }

        [Test]
        public void Layout_TurnsEachPiece_SoItsRoadOrLiquidLiesOnItsFramesSide()
        {
            foreach (LevelDefinition level in Levels())
            {
                EnvironmentLayout layout = new EnvironmentLayout(level, null).Build(1.5f, 0.75f);
                GridMap grid = level.Grid;
                float half = level.CellSize * 0.5f;
                foreach (EnvironmentPlacement placement in layout.Placements)
                {
                    Quaternion turn = Quaternion.Euler(0f, placement.Yaw, 0f);
                    switch (placement.Kind)
                    {
                        case EnvironmentPieceKind.RoadEdgeStraight:
                        case EnvironmentPieceKind.PlazaEdgeStraight:
                            // The road on the piece's -Z, open ground on its +Z.
                            Assert.IsTrue(level.Road.IsRoad(grid.WorldToCell(placement.Position + turn * Vector3.back * half)), $"{level.name}: kerb at {placement.Position} has no road behind it");
                            Assert.IsFalse(level.Road.IsRoad(grid.WorldToCell(placement.Position + turn * Vector3.forward * half)), $"{level.name}: kerb at {placement.Position} faces the road");
                            break;
                        case EnvironmentPieceKind.RoadEdgeOuterCorner:
                            Assert.IsTrue(level.Road.IsRoad(grid.WorldToCell(placement.Position + turn * new Vector3(-half, 0f, -half))), $"{level.name}: outer corner at {placement.Position} has its road quadrant off the road");
                            break;
                        case EnvironmentPieceKind.RoadEdgeInnerCorner:
                            Assert.IsFalse(level.Road.IsRoad(grid.WorldToCell(placement.Position + turn * new Vector3(half, 0f, half))), $"{level.name}: inner corner at {placement.Position} has its ground quadrant on the road");
                            break;
                        case EnvironmentPieceKind.BankStraight:
                            Assert.IsTrue(level.Terrain.IsLiquid(grid.WorldToCell(placement.Position + turn * Vector3.back * half)), $"{level.name}: bank at {placement.Position} has no liquid behind it");
                            break;
                        case EnvironmentPieceKind.TerraceStep:
                            Assert.AreEqual(TerrainKind.Raised, level.Terrain.KindAt(grid.WorldToCell(placement.Position + turn * Vector3.forward * half)), $"{level.name}: step at {placement.Position} has no plateau in front");
                            break;
                    }

                    if (placement.Kind == EnvironmentPieceKind.RoadEdgeStraight || placement.Kind == EnvironmentPieceKind.BankStraight)
                    {
                        // An edge piece sits on a cell side: one coordinate on a half-cell line.
                        Vector3 local = placement.Position - grid.CellToWorld(grid.WorldToCell(placement.Position + turn * Vector3.back * half));
                        Assert.AreEqual(half, Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z)), Tolerance, $"{level.name}: edge piece at {placement.Position} is off the cell side");
                    }
                }
            }
        }

        [Test]
        public void BorderWalls_KeepClearOfTheRoadEnds_AndTheBase()
        {
            foreach (LevelDefinition level in Levels())
            {
                EnvironmentLayout layout = new EnvironmentLayout(level, null).Build(1.5f, 0.75f);
                Vector3 basePosition = LevelVisualBuilder.BasePosition(level);
                Assert.Greater(layout.Count(EnvironmentPieceKind.CliffStraight), 0, $"{level.name}: no border wall");
                foreach (EnvironmentPlacement placement in layout.Placements)
                {
                    if (placement.Kind != EnvironmentPieceKind.CliffStraight)
                    {
                        continue;
                    }

                    Assert.Greater(Flat(placement.Position - basePosition), 2f, $"{level.name}: a wall stands in the base");
                    foreach (EnemyPath path in level.Paths)
                    {
                        Assert.Greater(Flat(placement.Position - level.Grid.CellToWorld(path.Waypoints[0])), 2f, $"{level.name}: a wall blocks where {path.Name} enters");
                    }
                }
            }
        }

        [Test]
        public void Flora_CarriesSwayColours_AndTheDecorReplacesTheThemesKinds()
        {
            foreach (LevelDefinition level in Levels())
            {
                EnvironmentSet set = level.Theme.Environment;
                foreach (PropKind kind in level.Theme.PropKinds)
                {
                    if (kind == PropKind.Reed)
                    {
                        // Continent 1 has no reed in its kit (Mirewood's comes with continent 4).
                        continue;
                    }

                    EnvironmentDecor decor = set.Decor(kind);
                    Assert.IsNotNull(decor, $"{level.name}: {kind} stays procedural");
                    Assert.Greater(decor.Scale, 0.2f, $"{set.name}: {kind} is scaled away");
                    Assert.Less(decor.Scale, 5f, $"{set.name}: {kind} is blown up");
                }
            }

            EnvironmentSet first = Levels()[0].Theme.Environment;
            foreach (PropKind plant in new[] { PropKind.Frond, PropKind.Tendril, PropKind.GlowTree, PropKind.Mushroom })
            {
                foreach (EnvironmentMesh variant in first.Decor(plant).Variants)
                {
                    Assert.IsTrue(variant.MaterialFor(false).IsKeywordEnabled(EnvironmentShaders.SwayKeyword), $"{variant.Name}: does not sway");
                    Color[] colours = variant.Mesh.colors;
                    Assert.AreEqual(variant.Mesh.vertexCount, colours.Length, $"{variant.Name}: no sway colours");
                    float top = 0f;
                    foreach (Color colour in colours)
                    {
                        top = Mathf.Max(top, colour.r);
                    }

                    Assert.Greater(top, 0.5f, $"{variant.Name}: its tips do not bend");
                }
            }
        }

        // Round 17 (import stage 4b): continents 2-6 have no terrace, rift or silhouette pieces; the layout's asks for
        // them are left out on purpose (raised ground and rifts keep the painted terrain). The Core alone has a vein.
        private static readonly EnvironmentPieceKind[] LeftOutOnLaterContinents =
        {
            EnvironmentPieceKind.TerraceStep, EnvironmentPieceKind.TerraceStepOuter, EnvironmentPieceKind.RiftShort,
            EnvironmentPieceKind.RiftMedium, EnvironmentPieceKind.RiftLong, EnvironmentPieceKind.RiftCap, EnvironmentPieceKind.Burrow,
            EnvironmentPieceKind.CrystalVein,
        };

        private static readonly (string Set, string Prefix)[] LaterContinents =
        {
            ("AshenReach", "Ash_"), ("NorthernCrown", "North_"), ("Mirewood", "Mire_"), ("SouthernCrown", "South_"), ("TheCore", "Core_"),
        };

        [Test]
        public void LaterContinentSets_ResolveEveryLayerPieceDecorAndOpening_TheLayoutCanAskFor()
        {
            foreach ((string name, string prefix) in LaterContinents)
            {
                EnvironmentSet set = Set(name);
                Assert.AreEqual(prefix, set.Prefix, $"{name}: wrong prefix");
                Assert.IsTrue(set.HasGround, $"{name}: no ground shader or ground/road layer");
                foreach (EnvironmentLayer layer in new[] { set.Ground, set.GroundAlt, set.Road, set.Plaza, set.Cliff, set.Element })
                {
                    Assert.IsTrue(layer.IsValid, $"{name}: a ground layer lacks its maps");
                    Assert.IsTrue(layer.Albedo(false).name.StartsWith(prefix), $"{name}: a ground layer is not its own ({layer.Albedo(false).name})");
                    Assert.AreNotSame(layer.Albedo(false), layer.Albedo(true), $"{name}: a ground layer has no Low maps");
                }

                foreach (EnvironmentPieceKind kind in (EnvironmentPieceKind[])System.Enum.GetValues(typeof(EnvironmentPieceKind)))
                {
                    EnvironmentMesh piece = set.Piece(kind);
                    bool leftOut = System.Array.IndexOf(LeftOutOnLaterContinents, kind) >= 0 && !(name == "TheCore" && kind == EnvironmentPieceKind.CrystalVein);
                    if (leftOut)
                    {
                        Assert.IsNull(piece, $"{name}: {kind} was meant to be left out");
                        continue;
                    }

                    Assert.IsNotNull(piece, $"{name}: no {kind}");
                    Assert.AreEqual(EnvironmentShaders.Lit, piece.MaterialFor(false).shader.name, $"{name}: {kind} has the wrong shader");
                    Assert.AreNotSame(piece.MaterialFor(false), piece.MaterialFor(true), $"{name}: {kind} has no Low material");
                    Assert.IsTrue(piece.MaterialFor(false).enableInstancing, $"{name}: {kind} is not instanced");
                    bool shared = kind == EnvironmentPieceKind.BuildPad || kind == EnvironmentPieceKind.GatePlinth;
                    Assert.AreEqual(!shared, piece.Name.StartsWith(prefix), $"{name}: {kind} wears {piece.Name}");
                }

                foreach (PropKind kind in (PropKind[])System.Enum.GetValues(typeof(PropKind)))
                {
                    EnvironmentDecor decor = set.Decor(kind);
                    Assert.IsNotNull(decor, $"{name}: {kind} would fall back to the procedural shape");
                    Assert.Greater(decor.Scale, 0.1f, $"{name}: {kind} is scaled away");
                    Assert.Less(decor.Scale, 8f, $"{name}: {kind} is blown up");
                }

                foreach (TerrainKind terrain in (TerrainKind[])System.Enum.GetValues(typeof(TerrainKind)))
                {
                    Assert.IsNotNull(set.Origin(terrain), $"{name}: no spawn opening on {terrain}");
                }

                Assert.IsNotEmpty(set.Scatter, $"{name}: no scatter");
            }
        }

        [Test]
        public void LaterContinentSets_ArePutOnNoTheme_AndCarryTheirLandmarks()
        {
            foreach (LevelDefinition level in Levels())
            {
                Assert.AreEqual("CrystalPlains", level.Theme.Environment.name, $"{level.name}: a continent-1 level wears another continent");
            }

            foreach (string seal in new[] { "Core_Seal_Intact", "Core_Seal_Cracked", "Core_Seal_Broken", "Core_SealVein_Branch" })
            {
                Assert.IsNotNull(Set("TheCore").Landmark(seal), $"The Core has no {seal}");
            }

            Assert.IsNotNull(Set("Mirewood").Landmark("Pioneer1_Wreck"), "Mirewood has no Pioneer-1 wreck");
        }

        [Test]
        public void LaterContinentFlora_SwaysFromItsOwnColours()
        {
            foreach ((string name, string _) in LaterContinents)
            {
                EnvironmentSet set = Set(name);
                foreach (PropKind kind in (PropKind[])System.Enum.GetValues(typeof(PropKind)))
                {
                    foreach (EnvironmentMesh variant in set.Decor(kind).Variants)
                    {
                        if (!variant.Name.Contains("Flora_"))
                        {
                            continue;
                        }

                        Assert.IsTrue(variant.MaterialFor(false).IsKeywordEnabled(EnvironmentShaders.SwayKeyword), $"{variant.Name}: does not sway");
                        Assert.AreEqual(variant.Mesh.vertexCount, variant.Mesh.colors.Length, $"{variant.Name}: no sway colours");
                    }
                }
            }
        }

        private static EnvironmentSet Set(string name)
        {
            var set = AssetDatabase.LoadAssetAtPath<EnvironmentSet>($"Assets/_Project/Data/Art/Environment/{name}.asset");
            Assert.IsNotNull(set, $"no environment set {name}");
            return set;
        }

        private static float Flat(Vector3 offset) => new Vector2(offset.x, offset.z).magnitude;

        private static List<LevelDefinition> Levels()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>($"{LevelFolder}/CampaignCatalog.asset");
            var levels = new List<LevelDefinition>();
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null)
                {
                    levels.Add(node.Level);
                }
            }

            Assert.IsNotEmpty(levels, "no campaign level");
            return levels;
        }
    }
}
