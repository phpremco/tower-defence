using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Import settings for everything under <see cref="BlenderSync.TargetRoot"/>, so a synced export needs no clicks:
    /// FBX at scale 1 with the axis change baked in, a Generic rig, imported normals with Mikktspace tangents (the
    /// mirrored UVs rely on the tangent sign), no materials (the generator makes them), the scale curves kept (organs
    /// swell by bone scale) and every take renamed from <c>Rig|Clip</c> to <c>Clip</c>, looping the cycles. Normal maps
    /// import as normal maps, the packed Mask and the detail normals as linear data, Albedo and Emission as sRGB, the
    /// detail normals tiling, everything with mipmaps.
    /// </summary>
    public sealed class BlenderModelImporter : AssetPostprocessor
    {
        private const int AnisoLevel = 2;
        private const int DetailAnisoLevel = 4;
        private const int TilingAnisoLevel = 8;
        private const int FullMapSize = 1024;
        private const int LowMapSize = 512;
        private const int DetailMapSize = 512;
        private const string LodSuffix = "_LOD1";
        private const string FarSuffix = "_Far";
        private const string FloraMarker = "Flora_";
        private const int PlanetAnisoLevel = 8;
        private const int PlanetMapSize = 4096;
        private const int PlanetLowMapSize = 2048;
        private const int VfxMapSize = 2048;
        private const int VfxLowMapSize = 1024;
        private const string GlossSuffix = "_Gloss";

        // Hero assets whose maps are painted larger than the usual 1024 (High, Low), by export folder.
        private static readonly Dictionary<string, (int High, int Low)> MapSizeOverrides = new Dictionary<string, (int High, int Low)>
        {
            { BlenderStoryCatalog.Mothership, (2048, 1024) },
            { VossAsset, (2048, 1024) },
            // The menu island: Top 2048, Cliff 4096 x 512 (Low half); the display backdrop 2048 x 1024.
            { MenuIslandAsset, (4096, 2048) },
            { UiDisplayAsset, (2048, 1024) },
        };

        // Exports whose shape keys (Voss's Smile, Frown, Blink, JawOpen) are imported as blend shapes.
        private static readonly HashSet<string> BlendShapeAssets = new HashSet<string> { VossAsset };

        private const string VossAsset = "Voss";
        public const string MenuIslandAsset = "Menu_Island";
        public const string MenuMouthAsset = "Menu_NestMouth";
        public const string UiDisplayAsset = "UI_Display";
        private const string HeightSuffix = "_Height";
        private const string DisplayRingTexture = "UI_DisplayRing";
        private const int MenuHeightSize = 512;

        // Shape-key channels come as extra takes "Key|Rig|Clip" beside the bone takes "Rig|Clip" of the same clip.
        private const string ShapeKeyTakePrefix = "Key|";

        // Bumped whenever these settings change, so Unity reimports the models with them.
        public override uint GetVersion() => 3;

        private static bool IsOurs(string path) => path.StartsWith(BlenderSync.TargetRoot + "/", StringComparison.Ordinal);

        // The environment kit (import stage 4a): static meshes without a rig, and tiling ground sets that repeat. The
        // resource node and the living spawn origins (round 23, rigged with takes) live in the same folder but import as
        // art models.
        private static bool IsEnvironment(string path) =>
            path.StartsWith(BlenderSync.TargetRoot + "/" + BlenderSync.EnvironmentFolder + "/", StringComparison.Ordinal) &&
            !path.StartsWith(BlenderSync.TargetRoot + "/" + BlenderColonyCatalog.ResourceNodeAsset + "/", StringComparison.Ordinal) &&
            !path.StartsWith(BlenderSync.TargetRoot + "/" + BlenderSync.EnvironmentFolder + "/" + BlenderOriginCatalog.LivingPrefix, StringComparison.Ordinal);

        private static bool IsTiling(string path) => IsEnvironment(path) && path.Contains("/" + BlenderSync.TilingFolder + "/");

        // A planet painted in Blender (texture-only export): equirectangular maps that wrap round in longitude.
        private static bool IsPlanet(string path) => path.StartsWith(BlenderSync.TargetRoot + "/" + BlenderPlanetCatalog.FolderPrefix, StringComparison.Ordinal);

        // The effect textures painted in Blender (round 20): flipbooks, sprites, ribbons and distortion maps.
        private static bool IsVfx(string path) => path.StartsWith(BlenderSync.TargetRoot + "/" + BlenderVfxCatalog.Folder + "/", StringComparison.Ordinal);

        // The liquid and mist textures (round 24): tiling surfaces, flipbooks and flow maps for Game/Liquid and Game/Mist.
        private static bool IsLiquid(string path) => path.StartsWith(BlenderSync.TargetRoot + "/" + BlenderLiquidCatalog.Folder + "/", StringComparison.Ordinal);

        // Static Blender sets that are neither environment kit nor art models: the menu island, its hive mouth and the
        // upgrade screen's display stage (rounds 26 and 27).
        private static bool IsStaticSet(string path)
        {
            string folder = ExportFolder(path);
            return folder == MenuIslandAsset || folder == MenuMouthAsset || folder == UiDisplayAsset;
        }

        // Round 26: the cave kit and the Burrower eyes (static, read by the cutscene builder), the cutscene sprites and the
        // theme skies (lat-long cubemaps).
        public const string UndergroundFolder = "Underground";
        public const string CutsceneFxFolder = "Cutscene_FX";
        public const string SkiesFolder = "Skies";
        private const int SkyMapSize = 2048;
        private const int SkyLowMapSize = 512;
        private const int CutsceneFxMapSize = 1024;
        private const int CutsceneFxLowMapSize = 512;
        private static bool IsUnderground(string path) => IsOurs(path) && ExportFolder(path) == UndergroundFolder;
        private static bool IsCutsceneFx(string path) => IsOurs(path) && ExportFolder(path) == CutsceneFxFolder;
        private static bool IsSky(string path) => IsOurs(path) && ExportFolder(path) == SkiesFolder;

        // Round 25b: markers, bars, projectiles, debris, the scout and the particle atlas.
        private static bool IsMarker(string path) => IsOurs(path) && BlenderMarkerCatalog.Contains(ExportFolder(path));

        // The export folder an asset path lies in (the first folder under the target root).
        private static string ExportFolder(string path)
        {
            string rest = path.Substring(BlenderSync.TargetRoot.Length + 1);
            int slash = rest.IndexOf('/');
            return slash >= 0 ? rest.Substring(0, slash) : rest;
        }

        private void OnPreprocessModel()
        {
            if (!IsOurs(assetPath))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileUnits = true;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = BlendShapeAssets.Contains(ExportFolder(assetPath));
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.skinWeights = ModelImporterSkinWeights.Standard;
            importer.importAnimation = true;
            importer.removeConstantScaleCurves = false;
            importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
            importer.resampleCurves = true;
            if (IsStaticSet(assetPath))
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
            }

            if (IsUnderground(assetPath))
            {
                // Static kit and eyes: no rig, no takes; the builder and the cave read the meshes (eye rig colours, crevices).
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.isReadable = true;
            }

            if (IsMarker(assetPath))
            {
                // Round 25b pieces are static; the builder bakes their node transforms into its own meshes, so it reads them.
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.isReadable = true;
            }

            if (IsEnvironment(assetPath))
            {
                // Static kit and decor: no rig, no takes, no Animator on the instance.
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                // The generator reads the kit meshes (bounds, the mirrored end caps, the flora sway colours).
                importer.isReadable = true;
            }
        }

        // The far LOD comes as its own file whose mesh node ends in "_LOD1"; without a "_LOD0" beside it Unity warns about
        // LOD naming on every import. Our views build the LOD group themselves, so the node only loses the suffix.
        private void OnPostprocessMeshHierarchy(GameObject root)
        {
            if (!IsOurs(assetPath))
            {
                return;
            }

            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name.EndsWith(LodSuffix, StringComparison.Ordinal))
                {
                    node.name = node.name.Substring(0, node.name.Length - LodSuffix.Length) + FarSuffix;
                }
            }
        }

        // Only the first vertex colour set reaches Unity. Round 17's flora carries exactly one set, Sway; round 8's carries
        // six bake helpers with Sway last (MODEL_TODO "Kleurlagen"), so Unity gets a helper. A flora mesh whose red does
        // not follow its height gets the Sway colours rebuilt from its shape; every other kit mesh drops the helpers.
        private void OnPostprocessModel(GameObject root)
        {
            if (!IsOurs(assetPath) || !IsEnvironment(assetPath))
            {
                return;
            }

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                if (!mesh.name.Contains(FloraMarker))
                {
                    mesh.colors = null;
                }
                else if (!EnvironmentSway.Carries(mesh, filter.transform.localToWorldMatrix))
                {
                    mesh.colors = EnvironmentSway.Build(mesh, filter.transform.localToWorldMatrix);
                }
            }
        }

        // Takes come as "Skitter_Rig|Walk": the clip is called after its action, and the cycles loop.
        private void OnPreprocessAnimation()
        {
            if (!IsOurs(assetPath))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            // The bone take of a clip wins over its shape-key twin: the clips pose the bones (Talk and Blink work through them).
            var takes = new List<ModelImporterClipAnimation>();
            takes.AddRange(Array.FindAll(importer.defaultClipAnimations, take => !IsShapeKeyTake(take)));
            takes.AddRange(Array.FindAll(importer.defaultClipAnimations, IsShapeKeyTake));
            var clips = new List<ModelImporterClipAnimation>(takes.Count);
            var names = new HashSet<string>();
            foreach (ModelImporterClipAnimation take in takes)
            {
                string name = ClipName(take.takeName);
                if (!names.Add(name))
                {
                    continue;
                }

                take.name = name;
                take.loopTime = BlenderArtCatalog.IsLoop(name);
                take.loopPose = false;
                take.lockRootRotation = false;
                take.lockRootHeightY = false;
                take.lockRootPositionXZ = false;
                clips.Add(take);
            }

            importer.clipAnimations = clips.ToArray();
        }

        private static bool IsShapeKeyTake(ModelImporterClipAnimation take) => take.takeName.StartsWith(ShapeKeyTakePrefix, StringComparison.Ordinal);

        public static string ClipName(string take)
        {
            int bar = take.LastIndexOf('|');
            return bar >= 0 ? take.Substring(bar + 1) : take;
        }

        private void OnPreprocessTexture()
        {
            if (!IsOurs(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (IsPlanet(assetPath))
            {
                PreprocessPlanetTexture(importer, file, assetPath.Contains("/" + BlenderSync.LowFolder + "/"));
                return;
            }

            if (IsCutsceneFx(assetPath))
            {
                PreprocessCutsceneFxTexture(importer, file, assetPath.Contains("/" + BlenderSync.LowFolder + "/"));
                return;
            }

            if (IsSky(assetPath))
            {
                PreprocessSkyTexture(importer, assetPath.Contains("/" + BlenderSync.LowFolder + "/"));
                return;
            }

            if (BlenderBossFxCatalog.IsBossFx(assetPath))
            {
                BlenderBossFxCatalog.Preprocess(importer, file, assetPath.Contains("/" + BlenderSync.LowFolder + "/"));
                return;
            }

            if (IsVfx(assetPath))
            {
                PreprocessVfxTexture(importer, file, assetPath.Contains("/" + BlenderSync.LowFolder + "/"));
                return;
            }

            if (BlenderUiBuilder.IsUiTexture(assetPath))
            {
                BlenderUiBuilder.Preprocess(importer, file);
                return;
            }

            if (IsMarker(assetPath) && !BlenderMarkerCatalog.IsLitMap(file))
            {
                PreprocessMarkerTexture(importer, file, assetPath.Contains("/" + BlenderSync.LowFolder + "/"));
                return;
            }

            if (IsLiquid(assetPath))
            {
                PreprocessLiquidTexture(importer, file, assetPath.Contains("/" + BlenderSync.LowFolder + "/"));
                return;
            }

            if (IsStaticSet(assetPath) && file.EndsWith(HeightSuffix, StringComparison.Ordinal))
            {
                PreprocessHeightMap(importer);
                return;
            }

            if (file == DisplayRingTexture)
            {
                // A greyscale halo drawn additively: linear, so the grey steps add up as painted (README round 27).
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = assetPath.Contains("/" + BlenderSync.LowFolder + "/") ? LowMapSize : FullMapSize;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                return;
            }

            bool detail = assetPath.Contains("/" + BlenderSync.DetailFolder + "/");
            bool tiling = IsTiling(assetPath);
            bool low = assetPath.Contains("/" + BlenderSync.LowFolder + "/");
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = false;
            importer.wrapMode = detail || tiling ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            // The ground is seen at the camera's slant over the whole screen: tiling sets get the most filtering.
            importer.anisoLevel = tiling ? TilingAnisoLevel : detail ? DetailAnisoLevel : AnisoLevel;
            importer.maxTextureSize = detail ? DetailMapSize : MapSize(assetPath, low);
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (file.EndsWith("_Normal", StringComparison.Ordinal))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                return;
            }

            importer.textureType = TextureImporterType.Default;
            bool mask = file.EndsWith("_Mask", StringComparison.Ordinal);
            importer.sRGBTexture = !mask;
            importer.alphaSource = mask ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            if (!mask)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
            }
        }

        // Cutscene_FX_report.json: greyscale sprites with RGB = A, tinted additively by the code, so linear data; the beam
        // tiles along U, the gate field along V, everything else clamps.
        private static void PreprocessCutsceneFxTexture(TextureImporter importer, string file, bool low)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 1;
            importer.wrapMode = TextureWrapMode.Clamp;
            if (file == "FX_BeamSoft")
            {
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Clamp;
            }
            else if (file == "FX_GateField")
            {
                importer.wrapModeU = TextureWrapMode.Clamp;
                importer.wrapModeV = TextureWrapMode.Repeat;
            }

            importer.maxTextureSize = low ? CutsceneFxLowMapSize : CutsceneFxMapSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }

        // Skies_report.json: equirectangular skies imported as cubemaps (lat-long layout), sRGB, with mips for the
        // reflection roughness.
        private static void PreprocessSkyTexture(TextureImporter importer, bool low)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.TextureCube;
            importer.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = low ? SkyLowMapSize : SkyMapSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }

        // The menu island's ground height (16-bit grey): read on the CPU by MenuTerrain, so it stays readable, linear,
        // uncompressed at its full 16 bits and unfiltered by mips.
        private static void PreprocessHeightMap(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.SingleChannel;
            importer.sRGBTexture = false;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = MenuHeightSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
            settings.format = TextureImporterFormat.R16;
            settings.maxTextureSize = MenuHeightSize;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(settings);
        }

        private static int MapSize(string path, bool low)
        {
            return MapSizeOverrides.TryGetValue(ExportFolder(path), out (int High, int Low) size)
                ? low ? size.Low : size.High
                : low ? LowMapSize : FullMapSize;
        }

        // README round 25b: channel packs are linear data with their alpha as painted, tinted by the material; ribbons and
        // bands repeat along their length, the rest clamps (the bar atlas would bleed between its rows).
        private static void PreprocessMarkerTexture(TextureImporter importer, string file, bool low)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = BlenderMarkerCatalog.Tiles(file) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.anisoLevel = AnisoLevel;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = low ? LowMapSize : FullMapSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }

        // VFX_report.json: every effect texture is linear data (colour comes from the materials), alpha as painted;
        // flipbooks and sprites clamp so a cell never bleeds into its neighbour at the edge, the tiling noise and ribbon
        // textures repeat. The offset maps of the distortion stay uncompressed: block compression bends the vectors.
        private static void PreprocessVfxTexture(TextureImporter importer, string file, bool low)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = BlenderVfxCatalog.Tiles(file) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.anisoLevel = 1;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = low ? VfxLowMapSize : VfxMapSize;
            importer.textureCompression = BlenderVfxCatalog.IsDistortion(file) ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
        }

        // Liquids_report.json: only the albedo maps are colour, the normal maps import as normal maps, everything else (masks,
        // heat, flow, flipbooks, mist) is linear data with its alpha as painted. The tiling maps repeat; the flipbooks and
        // the vortex clamp, the shore profile repeats along its phase and clamps across. Flow vectors stay uncompressed.
        private static void PreprocessLiquidTexture(TextureImporter importer, string file, bool low)
        {
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = false;
            importer.anisoLevel = TilingAnisoLevel;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = low ? LowMapSize : FullMapSize;
            importer.textureCompression = BlenderLiquidCatalog.IsFlow(file) ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.wrapMode = BlenderLiquidCatalog.Clamps(file) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            if (BlenderLiquidCatalog.IsProfile(file))
            {
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Clamp;
            }

            if (BlenderLiquidCatalog.IsNormal(file))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = BlenderLiquidCatalog.IsColour(file);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
        }

        // README round 20: wrap round in longitude, clamp at the poles; colour maps sRGB, the rest data; the region ids
        // exact (point filter, no mips, uncompressed) so a border never blends two ids into a third.
        private static void PreprocessPlanetTexture(TextureImporter importer, string file, bool low)
        {
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = false;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.wrapModeW = TextureWrapMode.Clamp;
            importer.anisoLevel = PlanetAnisoLevel;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = low ? PlanetLowMapSize : PlanetMapSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaSource = TextureImporterAlphaSource.None;
            if (file.EndsWith("_Normal", StringComparison.Ordinal))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                return;
            }

            importer.textureType = TextureImporterType.Default;
            bool colour = file.EndsWith("_Albedo", StringComparison.Ordinal) || file.EndsWith("_Emission", StringComparison.Ordinal);
            importer.sRGBTexture = colour;
            if (file.EndsWith("_Regions", StringComparison.Ordinal))
            {
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.anisoLevel = 0;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                return;
            }

            // The cloud shader reads the cover from alpha, the mask carries the land ids there, a baked gloss map the smoothness.
            if (file.EndsWith("_Clouds", StringComparison.Ordinal) || file.EndsWith("_Mask", StringComparison.Ordinal) || file.EndsWith(GlossSuffix, StringComparison.Ordinal))
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
            }
        }
    }
}
