using System.Collections.Generic;
using System.IO;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// ArtSource README round 26 for the opening cutscene and the theme skies: syncs the underground cave kit and the
    /// three Burrower eyes (static, <c>Game/EnvironmentLit</c> and <c>Game/BurrowerEye</c> materials, High and Low) into
    /// <see cref="UndergroundArt"/>, the glow / trail / crack sprites into <see cref="CutsceneFxSet"/>, the dying Earth
    /// into the cast's <see cref="EarthMaps"/> (its gloss map baked from the mask's wetness: 0.05 + 0.22 x G), and the
    /// lat-long skies onto every theme with their report's exposure. Re-running updates the same assets in place; a
    /// missing export leaves what is there.
    /// </summary>
    public static class BlenderCutsceneBuilder
    {
        private const string UndergroundPath = BlenderArtBuilder.ArtDataFolder + "/Story/UndergroundArt.asset";
        private const string FxPath = BlenderArtBuilder.ArtDataFolder + "/Story/CutsceneFxSet.asset";
        private const string CastPath = "Assets/_Project/Data/Story/Cutscene/CutsceneCast.asset";
        private const string MaterialFolder = BlenderArtBuilder.MaterialFolder + "/Underground";
        private const string DataFolder = "Assets/_Project/Data";
        private const string Underground = BlenderModelImporter.UndergroundFolder;
        private const string Earth = "Planet_Earth";
        private const string FxReport = "Cutscene_FX_report.json";
        private const string SkyReport = "Skies_report.json";
        private const string LowSuffix = "_Low";
        private const string EyeMaterialSuffix = "_Eye";
        private const string CorneaSuffix = "_Cornea";
        private const string EyeShader = "Game/BurrowerEye";
        private const float DefaultExposure = 1.3f;

        // README round 26: smoothness = 0.05 + 0.22 x the mask's wetness (sea 0.27, land 0.08, ice 0.20).
        private const float GlossBase = 0.05f;
        private const float GlossPerWetness = 0.22f;

        // The cave is lit by its crystals alone: the rock's albedo lifted a little so it reads at all, the veins at the kit glow.
        private const float CaveAlbedo = 2f;
        private const float CaveGlow = 2.4f;
        private const float CaveSmoothness = 0.8f;
        private const float CaveOcclusion = 0.7f;
        private const float EyeSmoothness = 0.75f;
        private const float CorneaOpacity = 0.12f;
        private const float CorneaSmoothness = 0.97f;

        /// <summary>The kit piece per <see cref="CavePiece"/>, in enum order.</summary>
        private static readonly string[] Pieces =
        {
            "Cave_Wall_A", "Cave_Wall_B", "Cave_Ceiling", "Cave_Stalactites", "Cave_Floor_A", "Cave_Floor_B", "Cave_Boulder",
            "Cave_CrystalCluster", "Cave_Vein",
        };

        private static readonly string[] Eyes = { "Eye_Burrower_A", "Eye_Burrower_B", "Eye_Burrower_C" };

        /// <summary>The sprite file per <see cref="CutsceneSprite"/>, in enum order.</summary>
        private static readonly string[] Sprites =
        {
            "FX_GlowSoft", "FX_BeamSoft", "FX_SunCore", "FX_SunHalo", "FX_SunStreak", "FX_Glint", "FX_EngineGlow", "FX_ExhaustPlume",
            "FX_RunningLight", "FX_PlasmaTrail", "FX_DrillCracks", "FX_CrackStrip_Drill_A", "FX_CrackStrip_Drill_B",
            "FX_CrackStrip_Swarm_A", "FX_CrackStrip_Swarm_B", "FX_ScreamRing", "FX_GateField",
        };

        private static readonly string[] EarthMapNames = { "Albedo", "Normal", "Mask", "Emission", "Clouds" };

        /// <summary>Which sky each theme wears: by theme asset name prefix, the report key.</summary>
        private static readonly (string ThemePrefix, string Sky)[] ThemeSkies =
        {
            ("Theme_C1_", "CrystalPlains"),
            ("Theme_Earth", "Earth"),
            ("Theme_Underground", "Underground"),
        };

        /// <summary>What one run built.</summary>
        public sealed class Built
        {
            public UndergroundArt Underground;
            public CutsceneFxSet Fx;
            public EarthMaps Earth;
            public EarthMaps EarthLow;
            public readonly Dictionary<string, (Cubemap High, Cubemap Low, float Exposure)> Skies = new Dictionary<string, (Cubemap, Cubemap, float)>();
        }

        [MenuItem("Tools/Generate/Blender Cutscene (cave, eyes, Earth, FX, skies)")]
        public static void BuildMenu()
        {
            Built built = SyncAndBuild();
            int applied = Apply(built);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender cutscene: cave {(built.Underground != null && built.Underground.IsComplete ? "complete" : "incomplete")}, FX {(built.Fx != null ? "built" : "missing")}, Earth {(built.Earth != null ? "built" : "missing")}, {built.Skies.Count} skies, {applied} owners wear them.");
        }

        public static Built SyncAndBuild()
        {
            var assets = new List<string>();
            foreach (string name in Pieces)
            {
                assets.Add($"{Underground}/{name}");
            }

            foreach (string name in Eyes)
            {
                assets.Add($"{Underground}/{name}");
            }

            BlenderSync.Sync(assets);
            BlenderSync.SyncFiles(BlenderModelImporter.CutsceneFxFolder, Png(Sprites), FxReport);
            BlenderSync.SyncFiles(BlenderModelImporter.SkiesFolder, Png(SkyFiles()), SkyReport);
            BlenderSync.SyncTextures(Earth, EarthMapNames);
            var built = new Built
            {
                Underground = BuildUnderground(),
                Fx = BuildFx(),
                Earth = BuildEarth(false),
                EarthLow = BuildEarth(true),
            };

            BuildSkies(built);
            return built;
        }

        /// <summary>Hands the art to the cutscene cast and the skies to the themes; returns how many owners changed.</summary>
        public static int Apply(Built built)
        {
            int applied = 0;
            var cast = AssetDatabase.LoadAssetAtPath<CutsceneCast>(CastPath);
            if (cast != null)
            {
                var so = new SerializedObject(cast);
                so.FindProperty("_underground").objectReferenceValue = built.Underground;
                so.FindProperty("_fx").objectReferenceValue = built.Fx;
                WriteEarth(so.FindProperty("_earthMaps"), built.Earth);
                WriteEarth(so.FindProperty("_earthMapsLow"), built.EarthLow);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cast);
                applied++;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:VisualTheme", new[] { DataFolder }))
            {
                var theme = AssetDatabase.LoadAssetAtPath<VisualTheme>(AssetDatabase.GUIDToAssetPath(guid));
                if (theme == null || !SkyOf(theme.name, out string key) || !built.Skies.TryGetValue(key, out (Cubemap High, Cubemap Low, float Exposure) sky))
                {
                    continue;
                }

                var so = new SerializedObject(theme);
                so.FindProperty("_skyCubemap").objectReferenceValue = sky.High;
                so.FindProperty("_skyCubemapLow").objectReferenceValue = sky.Low;
                so.FindProperty("_skyExposure").floatValue = sky.Exposure;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(theme);
                applied++;
            }

            return applied;
        }

        // The report holds one block per sky; its exposure restores the glow the PNG keeps under a soft shoulder.
        private static void BuildSkies(Built built)
        {
            string folder = BlenderArtBuilder.AssetFolder(BlenderModelImporter.SkiesFolder);
            string reportPath = $"{folder}/{SkyReport}";
            Dictionary<string, object> skies = File.Exists(reportPath) ? BlenderJson.Object(BlenderJson.Field(BlenderJson.Object(BlenderJson.Parse(File.ReadAllText(reportPath))), "skies")) : null;
            foreach ((string _, string key) in ThemeSkies)
            {
                var high = AssetDatabase.LoadAssetAtPath<Cubemap>($"{folder}/Sky_{key}.png");
                if (high == null)
                {
                    continue;
                }

                var low = AssetDatabase.LoadAssetAtPath<Cubemap>($"{folder}/{BlenderSync.LowFolder}/Sky_{key}.png");
                float exposure = BlenderJson.Number(BlenderJson.Field(BlenderJson.Object(BlenderJson.Field(skies, key)), "exposure"));
                built.Skies[key] = (high, low, exposure > 0f ? exposure : DefaultExposure);
            }
        }

        private static bool SkyOf(string theme, out string key)
        {
            foreach ((string prefix, string sky) in ThemeSkies)
            {
                if (theme.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    key = sky;
                    return true;
                }
            }

            key = null;
            return false;
        }

        private static IEnumerable<string> SkyFiles()
        {
            foreach ((string _, string key) in ThemeSkies)
            {
                yield return $"Sky_{key}";
            }
        }

        private static IEnumerable<string> Png(IEnumerable<string> names)
        {
            foreach (string name in names)
            {
                yield return name + ".png";
            }
        }

        private static UndergroundArt BuildUnderground()
        {
            var pieces = new EnvironmentMesh[Pieces.Length];
            for (int i = 0; i < Pieces.Length; i++)
            {
                pieces[i] = BuildPiece(Pieces[i]);
            }

            var eyes = new List<BurrowerEyeArt>();
            foreach (string name in Eyes)
            {
                BurrowerEyeArt eye = BuildEye(name);
                if (eye != null)
                {
                    eyes.Add(eye);
                }
            }

            var art = BlenderArtBuilder.LoadOrCreate<UndergroundArt>(UndergroundPath);
            var so = new SerializedObject(art);
            WriteArray(so.FindProperty("_pieces"), pieces);
            WriteArray(so.FindProperty("_eyes"), eyes);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        private static EnvironmentMesh BuildPiece(string name)
        {
            string folder = BlenderArtBuilder.AssetFolder($"{Underground}/{name}");
            MeshFilter filter = MeshIn($"{folder}/{name}.fbx");
            if (filter == null)
            {
                Debug.LogWarning($"Blender cutscene: no mesh in {folder}/{name}.fbx.");
                return null;
            }

            Material high = CaveMaterial(folder, name, false);
            Material low = CaveMaterial(folder, name, true);
            // In a model asset the root's world transform is its own, so the node's world matrix is its place in the FBX.
            return new EnvironmentMesh(name, filter.sharedMesh, high, low, true, filter.transform.localToWorldMatrix);
        }

        private static BurrowerEyeArt BuildEye(string name)
        {
            string folder = BlenderArtBuilder.AssetFolder($"{Underground}/{name}");
            MeshFilter filter = MeshIn($"{folder}/{name}.fbx");
            if (filter == null)
            {
                Debug.LogWarning($"Blender cutscene: no eye mesh in {folder}/{name}.fbx.");
                return null;
            }

            BlenderReport report = BlenderReport.Load($"{folder}/{name}_report.json");
            List<float> axis = report.Numbers("axis_unity");
            Vector3 hinge = axis.Count == 3 ? new Vector3(axis[0], axis[1], axis[2]) : Vector3.left;
            return new BurrowerEyeArt(
                name,
                filter.sharedMesh,
                filter.transform.localToWorldMatrix,
                EyeMaterial(folder, name, false, false),
                EyeMaterial(folder, name, true, false),
                EyeMaterial(folder, name, false, true),
                Fallback(report.Number("upper_lid_deg"), 32f),
                Fallback(report.Number("lower_lid_deg"), 22f),
                Fallback(report.Number("deg"), 22f),
                Fallback(report.Number("up_down_deg"), 15f),
                hinge);
        }

        private static float Fallback(float value, float fallback) => value > 0f ? value : fallback;

        private static MeshFilter MeshIn(string path)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
            {
                return null;
            }

            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    return filter;
                }
            }

            return null;
        }

        private static Material CaveMaterial(string folder, string name, bool low)
        {
            Material material = LoadOrCreateMaterial($"{MaterialFolder}/{name}{(low ? LowSuffix : string.Empty)}.mat", Shader.Find(EnvironmentShaders.Lit));
            string maps = Maps(folder, name, low);
            material.SetColor("_BaseColor", new Color(CaveAlbedo, CaveAlbedo, CaveAlbedo, 1f));
            material.SetTexture("_BaseMap", Texture($"{maps}/{name}_Albedo.png"));
            SetMap(material, "_BumpMap", Texture($"{maps}/{name}_Normal.png"), "_NORMALMAP");
            SetMap(material, "_MaskMap", Texture($"{maps}/{name}_Mask.png"), "_MASKMAP");
            SetMap(material, "_EmissionMap", Texture($"{maps}/{name}_Emission.png"), "_EMISSION");
            material.SetColor("_EmissionColor", Color.white * CaveGlow);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetFloat("_Smoothness", CaveSmoothness);
            material.SetFloat("_OcclusionStrength", CaveOcclusion);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        // The eye is opaque; the cornea the same maps and shader alpha blended over it, glossy and nearly clear.
        private static Material EyeMaterial(string folder, string name, bool low, bool cornea)
        {
            string suffix = cornea ? CorneaSuffix : EyeMaterialSuffix;
            Material material = LoadOrCreateMaterial($"{MaterialFolder}/{name}{suffix}{(low ? LowSuffix : string.Empty)}.mat", Shader.Find(EyeShader));
            string maps = Maps(folder, name, low);
            material.SetTexture("_BaseMap", Texture($"{maps}/{name}_Albedo.png"));
            material.SetTexture("_BumpMap", Texture($"{maps}/{name}_Normal.png"));
            material.SetTexture("_MaskMap", Texture($"{maps}/{name}_Mask.png"));
            material.SetTexture("_EmissionMap", cornea ? null : Texture($"{maps}/{name}_Emission.png"));
            material.SetColor("_BaseColor", new Color(1f, 1f, 1f, cornea ? CorneaOpacity : 1f));
            material.SetFloat("_Smoothness", cornea ? CorneaSmoothness : EyeSmoothness);
            material.SetFloat("_SrcBlend", (float)(cornea ? UnityEngine.Rendering.BlendMode.SrcAlpha : UnityEngine.Rendering.BlendMode.One));
            material.SetFloat("_DstBlend", (float)(cornea ? UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : UnityEngine.Rendering.BlendMode.Zero));
            material.SetFloat("_ZWrite", cornea ? 0f : 1f);
            material.renderQueue = cornea ? (int)UnityEngine.Rendering.RenderQueue.Transparent : -1;
            material.SetOverrideTag("RenderType", cornea ? "Transparent" : "Opaque");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string Maps(string folder, string name, bool low) =>
            low && File.Exists($"{folder}/{BlenderSync.LowFolder}/{name}_Albedo.png") ? $"{folder}/{BlenderSync.LowFolder}" : folder;

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                BlenderArtBuilder.EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.shaderKeywords = new string[0];
            return material;
        }

        private static CutsceneFxSet BuildFx()
        {
            string folder = BlenderArtBuilder.AssetFolder(BlenderModelImporter.CutsceneFxFolder);
            if (Texture($"{folder}/{Sprites[0]}.png") == null)
            {
                Debug.LogWarning($"Blender cutscene: no sprites at {folder}.");
                return null;
            }

            var high = new Texture2D[Sprites.Length];
            var low = new Texture2D[Sprites.Length];
            for (int i = 0; i < Sprites.Length; i++)
            {
                high[i] = Texture($"{folder}/{Sprites[i]}.png");
                low[i] = Texture($"{folder}/{BlenderSync.LowFolder}/{Sprites[i]}.png");
            }

            var set = BlenderArtBuilder.LoadOrCreate<CutsceneFxSet>(FxPath);
            var so = new SerializedObject(set);
            WriteObjects(so.FindProperty("_high"), high);
            WriteObjects(so.FindProperty("_low"), low);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            return set;
        }

        private static EarthMaps BuildEarth(bool low)
        {
            string folder = BlenderArtBuilder.AssetFolder(Earth);
            string maps = low ? $"{folder}/{BlenderSync.LowFolder}" : folder;
            string mask = $"{maps}/{Earth}_Mask.png";
            if (!File.Exists(mask))
            {
                Debug.LogWarning($"Blender cutscene: no Earth maps at {maps}.");
                return null;
            }

            string gloss = $"{maps}/{Earth}_Gloss.png";
            BakeGloss(mask, gloss);
            return new EarthMaps
            {
                Albedo = Texture($"{maps}/{Earth}_Albedo.png"),
                Normal = Texture($"{maps}/{Earth}_Normal.png"),
                Gloss = Texture(gloss),
                Emission = Texture($"{maps}/{Earth}_Emission.png"),
                Smog = Texture($"{maps}/{Earth}_Clouds.png"),
            };
        }

        // The planet shader keeps smoothness in the gloss map's alpha; the Blender mask has wetness in G. Baked only when
        // the mask is newer than the bake, so a re-run with an unchanged export writes nothing.
        private static void BakeGloss(string maskPath, string glossPath)
        {
            if (File.Exists(glossPath) && File.GetLastWriteTimeUtc(glossPath) >= File.GetLastWriteTimeUtc(maskPath))
            {
                return;
            }

            var mask = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            mask.LoadImage(File.ReadAllBytes(maskPath));
            Color32[] pixels = mask.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                float smoothness = GlossBase + GlossPerWetness * (pixels[i].g / 255f);
                pixels[i] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(smoothness * 255f));
            }

            var gloss = new Texture2D(mask.width, mask.height, TextureFormat.RGBA32, false, true);
            gloss.SetPixels32(pixels);
            File.WriteAllBytes(glossPath, gloss.EncodeToPNG());
            Object.DestroyImmediate(mask);
            Object.DestroyImmediate(gloss);
            AssetDatabase.ImportAsset(glossPath);
        }

        private static void WriteEarth(SerializedProperty maps, EarthMaps earth)
        {
            if (maps == null || earth == null)
            {
                return;
            }

            maps.FindPropertyRelative("Albedo").objectReferenceValue = earth.Albedo;
            maps.FindPropertyRelative("Normal").objectReferenceValue = earth.Normal;
            maps.FindPropertyRelative("Gloss").objectReferenceValue = earth.Gloss;
            maps.FindPropertyRelative("Emission").objectReferenceValue = earth.Emission;
            maps.FindPropertyRelative("Smog").objectReferenceValue = earth.Smog;
        }

        private static void WriteArray<T>(SerializedProperty array, IList<T> items)
        {
            array.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                array.GetArrayElementAtIndex(i).boxedValue = items[i];
            }
        }

        private static void WriteObjects(SerializedProperty array, Object[] items)
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static void SetMap(Material material, string slot, Texture texture, string keyword)
        {
            material.SetTexture(slot, texture);
            if (texture != null)
            {
                material.EnableKeyword(keyword);
            }
        }

        private static Texture2D Texture(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
