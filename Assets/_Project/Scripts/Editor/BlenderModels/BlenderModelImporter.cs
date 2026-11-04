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

        // Bumped whenever these settings change, so Unity reimports the models with them.
        public override uint GetVersion() => 2;

        private static bool IsOurs(string path) => path.StartsWith(BlenderSync.TargetRoot + "/", StringComparison.Ordinal);

        // The environment kit (import stage 4a): static meshes without a rig, and tiling ground sets that repeat. The
        // resource node lives in the same folder but is a stage 3a art model (its own import settings).
        private static bool IsEnvironment(string path) =>
            path.StartsWith(BlenderSync.TargetRoot + "/" + BlenderSync.EnvironmentFolder + "/", StringComparison.Ordinal) &&
            !path.StartsWith(BlenderSync.TargetRoot + "/" + BlenderColonyCatalog.ResourceNodeAsset + "/", StringComparison.Ordinal);

        private static bool IsTiling(string path) => IsEnvironment(path) && path.Contains("/" + BlenderSync.TilingFolder + "/");

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
            importer.importBlendShapes = false;
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
            ModelImporterClipAnimation[] takes = importer.defaultClipAnimations;
            var clips = new List<ModelImporterClipAnimation>(takes.Length);
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
            bool detail = assetPath.Contains("/" + BlenderSync.DetailFolder + "/");
            bool tiling = IsTiling(assetPath);
            bool low = assetPath.Contains("/" + BlenderSync.LowFolder + "/");
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = false;
            importer.wrapMode = detail || tiling ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            // The ground is seen at the camera's slant over the whole screen: tiling sets get the most filtering.
            importer.anisoLevel = tiling ? TilingAnisoLevel : detail ? DetailAnisoLevel : AnisoLevel;
            importer.maxTextureSize = detail ? DetailMapSize : low ? LowMapSize : FullMapSize;
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
    }
}
