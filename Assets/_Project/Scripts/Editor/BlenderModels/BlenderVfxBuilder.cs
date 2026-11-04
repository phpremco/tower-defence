using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Game.Combat;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Brings the effect textures painted in Blender into the game: syncs <c>ArtSource/Exports/VFX</c>, builds a High
    /// and a Low (Switch) material per sheet on the effect shaders (<c>Game/VfxParticle</c>, <c>Game/VfxSmoke</c>,
    /// <c>Game/VfxDistortion</c>) and a <see cref="VfxSheet"/> with the atlas layout from the report. Re-running updates
    /// the same assets in place. The weapon and impact recipes that wear the sheets are the generator's effect library.
    /// </summary>
    public static class BlenderVfxBuilder
    {
        public const string SheetFolder = "Assets/_Project/Data/Vfx/Sheets";
        public const string MaterialFolder = "Assets/_Project/Art/Materials/Vfx";
        public const string ParticleShader = "Game/VfxParticle";
        public const string SmokeShader = "Game/VfxSmoke";
        public const string DistortionShader = "Game/VfxDistortion";

        private const float ShockStrength = 0.03f;
        private const float HazeStrength = 0.008f;
        private static readonly Vector4 HazeScroll = new Vector4(0f, -0.45f, 0f, 0f);
        private const float SoftDistance = 0.4f;
        private const float SmokeSoftDistance = 0.8f;
        private const float RibbonNoiseRepeats = 4f;
        // The big sims reach the side of their cell in their last frames; this much of the cell fades out at its rim.
        private const float SmokeEdgeFade = 0.14f;

        [MenuItem("Tools/Generate/Blender VFX (sync, materials, effect library)")]
        public static void BuildMenu()
        {
            Dictionary<string, VfxSheet> sheets = SyncAndBuild();
            ProjectGenerator.CreateVfxLibraryFrom(sheets);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender VFX: {sheets.Count} sheet(s) built, effect library updated.");
        }

        /// <summary>Syncs the export and builds every sheet whose textures are in Assets, by name.</summary>
        public static Dictionary<string, VfxSheet> SyncAndBuild()
        {
            BlenderSync.SyncFiles(BlenderVfxCatalog.Folder, BlenderVfxCatalog.AllFiles(), BlenderVfxCatalog.Report);
            string folder = BlenderArtBuilder.AssetFolder(BlenderVfxCatalog.Folder);
            string low = $"{folder}/{BlenderSync.LowFolder}";
            string report = File.Exists(Path.Combine(folder, BlenderVfxCatalog.Report)) ? File.ReadAllText(Path.Combine(folder, BlenderVfxCatalog.Report)) : string.Empty;
            BlenderArtBuilder.EnsureFolder(SheetFolder);
            BlenderArtBuilder.EnsureFolder(MaterialFolder);

            var sheets = new Dictionary<string, VfxSheet>();
            foreach (BlenderVfxCatalog.Sheet sheet in BlenderVfxCatalog.Sheets)
            {
                string[] files = BlenderVfxCatalog.Files(sheet);
                if (AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{files[0]}") == null)
                {
                    Debug.LogWarning($"Blender VFX: no texture for '{sheet.Name}' at {folder}.");
                    continue;
                }

                Layout layout = Layout.Read(report, sheet.SixWay ? sheet.Name + "_SixWayPos" : sheet.Name);
                Material high = BuildMaterial(sheet, layout, folder, folder, false);
                Material lowMaterial = BuildMaterial(sheet, layout, low, folder, true);
                sheets[sheet.Name] = BuildSheet(sheet, layout, high, lowMaterial);
            }

            return sheets;
        }

        private static VfxSheet BuildSheet(BlenderVfxCatalog.Sheet entry, Layout layout, Material high, Material low)
        {
            var sheet = BlenderArtBuilder.LoadOrCreate<VfxSheet>($"{SheetFolder}/{entry.Name}.asset");
            var so = new SerializedObject(sheet);
            so.FindProperty("_material").objectReferenceValue = high;
            so.FindProperty("_lowMaterial").objectReferenceValue = low;
            so.FindProperty("_columns").intValue = layout.Columns;
            so.FindProperty("_rows").intValue = layout.Rows;
            so.FindProperty("_frames").intValue = layout.Frames;
            so.FindProperty("_fps").floatValue = layout.Fps;
            so.FindProperty("_lifetime").floatValue = layout.Lifetime;
            so.FindProperty("_worldSize").floatValue = layout.WorldSize;
            so.FindProperty("_pivot").vector2Value = layout.Pivot;
            so.FindProperty("_cellAspect").floatValue = layout.CellAspect;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sheet);
            return sheet;
        }

        // The low material wears the half-size texture and skips the soft fade and the noise layer.
        private static Material BuildMaterial(BlenderVfxCatalog.Sheet entry, Layout layout, string textureFolder, string highFolder, bool low)
        {
            string path = $"{MaterialFolder}/{entry.Name}{(low ? "_Low" : string.Empty)}.mat";
            string shaderName = entry.Kind == BlenderVfxCatalog.Kind.SixWay ? SmokeShader : entry.Kind == BlenderVfxCatalog.Kind.Distortion ? DistortionShader : ParticleShader;
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new System.InvalidOperationException($"Effect shader '{shaderName}' not found; is it compiled?");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.shaderKeywords = new string[0];
            string[] files = BlenderVfxCatalog.Files(entry);
            Texture2D main = Texture(textureFolder, highFolder, files[0]);
            switch (entry.Kind)
            {
                case BlenderVfxCatalog.Kind.SixWay:
                    material.SetTexture("_PosMap", main);
                    material.SetTexture("_NegMap", Texture(textureFolder, highFolder, files[1]));
                    material.SetColor("_Albedo", entry.Core);
                    material.SetFloat("_FireStrength", entry.Glow.r);
                    material.SetFloat("_SoftDistance", SmokeSoftDistance);
                    material.SetVector("_Grid", new Vector4(layout.Columns, layout.Rows, 0f, 0f));
                    material.SetFloat("_EdgeFade", SmokeEdgeFade);
                    Keyword(material, "_FLIPBOOK_BLEND", "_FlipbookBlend", true);
                    Keyword(material, "_SOFT_PARTICLES", "_SoftParticles", !low);
                    Keyword(material, "_HEAT_STREAM", "_HeatStream", true);
                    break;
                case BlenderVfxCatalog.Kind.Distortion:
                    bool haze = entry.Name.StartsWith("HeatHaze", System.StringComparison.Ordinal);
                    material.SetTexture("_MainTex", main);
                    material.SetFloat("_Strength", haze ? HazeStrength : ShockStrength);
                    material.SetVector("_Scroll", haze ? HazeScroll : Vector4.zero);
                    material.SetFloat("_RadialMask", haze ? 1f : 0f);
                    material.SetFloat("_UseMask", haze ? 0f : 1f);
                    break;
                default:
                    material.SetTexture("_MainTex", main);
                    material.SetColor("_CoreColor", entry.Core);
                    material.SetColor("_GlowColor", entry.Glow);
                    material.SetColor("_ThirdColor", entry.Third);
                    material.SetColor("_Tint", Color.white);
                    material.SetFloat("_Intensity", 1f);
                    material.SetFloat("_Opacity", entry.Opacity);
                    material.SetVector("_Grid", new Vector4(layout.Columns, layout.Rows, 0f, 0f));
                    material.SetFloat("_SoftDistance", SoftDistance);
                    material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                    bool particle = entry.Kind == BlenderVfxCatalog.Kind.Glow || entry.Kind == BlenderVfxCatalog.Kind.Cover;
                    Keyword(material, "_FLIPBOOK_BLEND", "_FlipbookBlend", particle && layout.Frames > 1 && layout.Fps > 0f);
                    Keyword(material, "_SOFT_PARTICLES", "_SoftParticles", particle && !low);
                    Keyword(material, "_ATLAS_FRAME", "_AtlasFrame", entry.Kind == BlenderVfxCatalog.Kind.Card);
                    bool noise = entry.Noise != null && !low;
                    Keyword(material, "_NOISE", "_Noise", noise);
                    material.SetTexture("_NoiseTex", noise ? Texture(textureFolder, highFolder, entry.Noise + ".png") : null);
                    material.SetFloat("_NoiseStrength", noise ? entry.NoiseStrength : 0f);
                    // Beams and ribbons stretch their texture over their length; the noise repeats along it instead.
                    material.SetTextureScale("_NoiseTex", new Vector2(RibbonNoiseRepeats, 1f));
                    material.SetVector("_NoiseScroll", entry.NoiseScroll);
                    break;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Keyword(Material material, string keyword, string toggle, bool on)
        {
            material.SetFloat(toggle, on ? 1f : 0f);
            if (on)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        // The Low folder holds every texture; should one be missing there, the full size stands in.
        private static Texture2D Texture(string folder, string highFolder, string file)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{file}");
            return texture != null ? texture : AssetDatabase.LoadAssetAtPath<Texture2D>($"{highFolder}/{file}");
        }

        /// <summary>The atlas layout of one texture as the report gives it.</summary>
        private readonly struct Layout
        {
            private Layout(int columns, int rows, int frames, float fps, float lifetime, float worldSize, Vector2 pivot, float cellAspect)
            {
                Columns = columns;
                Rows = rows;
                Frames = frames;
                Fps = fps;
                Lifetime = lifetime;
                WorldSize = worldSize;
                Pivot = pivot;
                CellAspect = cellAspect;
            }

            public int Columns { get; }
            public int Rows { get; }
            public int Frames { get; }
            public float Fps { get; }
            public float Lifetime { get; }
            public float WorldSize { get; }
            public Vector2 Pivot { get; }
            public float CellAspect { get; }

            public static Layout Read(string report, string key)
            {
                string block = Block(report, key);
                int columns = Mathf.Max(1, (int)Number(block, "cols", 1f));
                int rows = Mathf.Max(1, (int)Number(block, "rows", 1f));
                int frames = Mathf.Max(1, (int)Number(block, "frames", columns * rows));
                float fps = Number(block, "fps", 0f);
                float lifetime = Number(block, "lifetime", fps > 0f ? frames / fps : 0f);
                Vector2 size = Pair(block, "size", new Vector2(1f, 1f));
                Vector2 cell = Pair(block, "cell", new Vector2(size.x / columns, size.y / rows));
                // A "single" sheet may list the cell it was painted at; one cell covers the whole texture then.
                if (columns * rows == 1)
                {
                    cell = size;
                }

                return new Layout(columns, rows, frames, fps, lifetime, Number(block, "world_size", 0f), Pair(block, "pivot", new Vector2(0.5f, 0.5f)), cell.x > 0f ? cell.y / cell.x : 1f);
            }

            // The object of one texture in the report: from its key to the matching closing brace.
            private static string Block(string report, string key)
            {
                int start = report.IndexOf("\"" + key + "\": {", System.StringComparison.Ordinal);
                if (start < 0)
                {
                    return string.Empty;
                }

                int depth = 0;
                for (int i = report.IndexOf('{', start); i < report.Length; i++)
                {
                    depth += report[i] == '{' ? 1 : report[i] == '}' ? -1 : 0;
                    if (depth == 0)
                    {
                        return report.Substring(start, i - start + 1);
                    }
                }

                return report.Substring(start);
            }

            private static float Number(string block, string key, float fallback)
            {
                Match match = Regex.Match(block, "\"" + key + "\"\\s*:\\s*(-?[0-9.]+)");
                return match.Success ? float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : fallback;
            }

            private static Vector2 Pair(string block, string key, Vector2 fallback)
            {
                Match match = Regex.Match(block, "\"" + key + "\"\\s*:\\s*\\[\\s*(-?[0-9.]+)\\s*,\\s*(-?[0-9.]+)");
                return match.Success
                    ? new Vector2(float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture))
                    : fallback;
            }
        }
    }
}
