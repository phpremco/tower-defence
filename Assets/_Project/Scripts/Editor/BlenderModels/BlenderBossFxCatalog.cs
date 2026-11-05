using System;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The round-29 boss art (ArtSource README round 29): which file is which ground mark or flipbook, the look numbers of
    /// each (glow colour, threshold mode, world size), the props and the drained gate maps, and the import settings of the
    /// <c>Boss_FX</c> folder (albedo sRGB with transparent alpha, packs linear; strips repeat along U).
    /// </summary>
    public static class BlenderBossFxCatalog
    {
        public const string Folder = "Boss_FX";
        public const string Report = "Boss_FX_report.json";
        public const string RuinGate = "RuinGate";
        public const string SealCrystal = "SealCrystal";
        public const string IceBlock = "Gate_IceBlock";
        public const string DrillWreck = "Cutscene_Drill_Wreck";
        public const string CrackSlabs = "Gravemaw_CrackSlabs";
        public const string Fissure = BlenderModelImporter.FissureAsset;
        public const string UiFolder = "UI";
        public const string Dampened = "UI_Dampened";

        /// <summary>How a mark's threshold moves (the <c>_Mode</c> of <c>Game/BossDecal</c>).</summary>
        public enum Threshold
        {
            Open = 0,
            Decay = 1,
            Heat = 2,
            Grow = 3,
            Always = 4,
        }

        public sealed class MarkSpec
        {
            public MarkSpec(BossFxArt.Mark mark, string file, float width, float tile, Threshold mode, Color glow, float depth = 0f, float lip = 0f, float pulse = 0f, float edgeGlow = 1f)
            {
                Mark = mark;
                File = file;
                Width = width;
                Tile = tile;
                Mode = mode;
                Glow = glow;
                Depth = depth;
                Lip = lip;
                Pulse = pulse;
                EdgeGlow = edgeGlow;
            }

            public BossFxArt.Mark Mark { get; }
            public string File { get; }
            public float Width { get; }
            public float Tile { get; }
            public Threshold Mode { get; }
            public Color Glow { get; }
            public float Depth { get; }
            public float Lip { get; }
            public float Pulse { get; }
            public float EdgeGlow { get; }
            public bool Atlas => Mark == BossFxArt.Mark.FrostCreep;
        }

        public sealed class SheetSpec
        {
            public SheetSpec(BossFxArt.Sheet sheet, string file, float fps, Color core, Color glow, Color third, float opacity, bool soft)
            {
                Sheet = sheet;
                File = file;
                Fps = fps;
                Core = core;
                Glow = glow;
                Third = third;
                Opacity = opacity;
                Soft = soft;
            }

            public BossFxArt.Sheet Sheet { get; }
            public string File { get; }
            public float Fps { get; }
            public Color Core { get; }
            public Color Glow { get; }
            public Color Third { get; }
            public float Opacity { get; }
            public bool Soft { get; }
        }

        private static Color Hdr(float r, float g, float b, float intensity) => new Color(r * intensity, g * intensity, b * intensity, 1f);

        // Glow colours and strengths from the report; depth, lips and pulse from game-camera screenshots (round 5 boss look).
        public static readonly MarkSpec[] Marks =
        {
            new MarkSpec(BossFxArt.Mark.GroundCrack, "BossFX_GroundCrack", 2f, 8f, Threshold.Open, Hdr(1f, 0.42f, 0.1f, 3f), 1f, 0.22f, 0.18f, 0.2f),
            new MarkSpec(BossFxArt.Mark.LavaTrail, "BossFX_LavaTrail", 1.4f, 5.6f, Threshold.Heat, Hdr(1f, 0.35f, 0.08f, 3f), 0f, 0f, 0.12f),
            new MarkSpec(BossFxArt.Mark.Furrow, "BossFX_Furrow", 3f, 12f, Threshold.Always, Hdr(0.6f, 0.3f, 1f, 1.5f), 0.35f, 0.14f),
            new MarkSpec(BossFxArt.Mark.RotTrail, "BossFX_RotTrail", 1.6f, 6.4f, Threshold.Decay, Hdr(0.55f, 1f, 0.2f, 1.5f), 0f, 0f, 0.15f),
            new MarkSpec(BossFxArt.Mark.RoadSink, "BossFX_RoadSink", 3.5f, 14f, Threshold.Open, Hdr(0.55f, 0.25f, 1f, 1.8f), 0.45f),
            new MarkSpec(BossFxArt.Mark.FrostCreep, "BossFX_FrostCreep", 2.5f, 2.5f, Threshold.Grow, Hdr(0.5f, 0.85f, 1f, 1.4f)),
        };

        // CG pack (R core, G glow, B extra, A coverage) is additive; the wakes are the LIT pack drawn covering.
        public static readonly SheetSpec[] Sheets =
        {
            new SheetSpec(BossFxArt.Sheet.SporeCloud, "BossFX_SporeCloud", 12f, Hdr(0.9f, 1f, 0.6f, 1.6f), Hdr(0.5f, 1f, 0.25f, 1.2f), Color.clear, 0.35f, true),
            new SheetSpec(BossFxArt.Sheet.VineBurn, "BossFX_VineBurn", 16f, Hdr(1f, 0.85f, 0.5f, 3f), Hdr(1f, 0.45f, 0.1f, 2.4f), Color.clear, 0f, true),
            new SheetSpec(BossFxArt.Sheet.ShardFlash, "BossFX_ShardFlash", 24f, Hdr(1f, 1f, 1f, 3f), Hdr(0.95f, 0.45f, 1f, 2.4f), Hdr(0.6f, 0.9f, 1f, 2f), 0f, false),
            new SheetSpec(BossFxArt.Sheet.WakeWater, "BossFX_DiveWake_Water", 12f, Hdr(0.3f, 0.45f, 0.55f, 1f), Hdr(0.6f, 0.65f, 0.65f, 1f), Hdr(1f, 1f, 1f, 0.6f), 1f, false),
            new SheetSpec(BossFxArt.Sheet.WakeLava, "BossFX_DiveWake_Lava", 12f, Hdr(0.12f, 0.06f, 0.04f, 1f), Hdr(1f, 0.4f, 0.08f, 4f), Hdr(1f, 0.7f, 0.4f, 0.6f), 1f, false),
            new SheetSpec(BossFxArt.Sheet.WakeBog, "BossFX_DiveWake_Bog", 12f, Hdr(0.16f, 0.2f, 0.08f, 1f), Hdr(0.5f, 0.9f, 0.3f, 0.5f), Hdr(1f, 1f, 0.8f, 0.4f), 1f, false),
            new SheetSpec(BossFxArt.Sheet.WakeQuicksand, "BossFX_DiveWake_Quicksand", 12f, Hdr(0.45f, 0.36f, 0.26f, 1f), Hdr(0.35f, 0.3f, 0.22f, 1f), Hdr(1f, 0.95f, 0.8f, 0.4f), 1f, false),
            new SheetSpec(BossFxArt.Sheet.WakeIce, "BossFX_DiveWake_Ice", 12f, Hdr(0.4f, 0.55f, 0.7f, 1f), Hdr(0.5f, 0.85f, 1f, 0.9f), Hdr(1f, 1f, 1f, 0.6f), 1f, false),
        };

        /// <summary>The lit and drained emission maps of the gate parts (folder, lit file, drained file).</summary>
        public static readonly (string Folder, string Lit, string Drained)[] DrainedMaps =
        {
            ("Gate", "Gate_Emission", "Gate_Drained_Emission"),
            ("Tower_GateGun", "Tower_GateGun_Emission", "Tower_GateGun_Drained_Emission"),
        };

        public static bool IsBossFx(string path) => path.StartsWith(BlenderSync.TargetRoot + "/" + Folder + "/", StringComparison.Ordinal);

        private static bool IsStrip(string file)
        {
            foreach (MarkSpec mark in Marks)
            {
                if (!mark.Atlas && file.StartsWith(mark.File, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>README round 29 import: albedo sRGB with transparent alpha, packs and sheets linear; strips repeat along U.</summary>
        public static void Preprocess(TextureImporter importer, string file, bool low)
        {
            bool albedo = file.EndsWith("_Albedo", StringComparison.Ordinal);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = albedo;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = albedo;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = IsStrip(file) ? 4 : 1;
            importer.maxTextureSize = low ? 512 : 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (IsStrip(file))
            {
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Clamp;
            }
            else
            {
                importer.wrapMode = TextureWrapMode.Clamp;
            }
        }
    }
}
