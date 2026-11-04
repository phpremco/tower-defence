using System.Collections.Generic;
using System.IO;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Turns the synced Blender liquid and mist textures (README round 24) into data: one <c>Game/Liquid</c> material per
    /// kind and profile, a <see cref="LiquidSet"/> holding them, a <see cref="MistDefinition"/> with the three mist sheets,
    /// and points every visual theme at both. Re-running updates the same assets in place.
    /// </summary>
    public static class BlenderLiquidBuilder
    {
        public const string DataFolder = BlenderArtBuilder.ArtDataFolder + "/Liquids";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Liquids";
        private const string SetPath = DataFolder + "/Liquids_CrystalPlains.asset";
        private const string MistPath = DataFolder + "/Mist_CrystalPlains.asset";
        private const string LiquidShader = "Game/Liquid";
        private const string MistShader = "Game/Mist";
        private const string LowKeyword = "_LIQUID_LOW";
        private const string RefractKeyword = "_LIQUID_REFRACT";
        private const string ThemeFolder = "Assets/_Project/Data";
        private const string LevelFolder = "Assets/_Project/Data/Levels";

        [MenuItem("Tools/Generate/Blender Liquids (sync, materials, mist)")]
        public static void BuildMenu()
        {
            (LiquidSet set, MistDefinition mist) = SyncAndBuild();
            int themes = Apply(set, mist);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender liquids: {set.Looks.Length} kind(s), {mist.Layers.Length} mist sheet(s), {themes} theme(s) wear them.");
        }

        /// <summary>Syncs the textures and builds the materials, the set and the mist.</summary>
        public static (LiquidSet Set, MistDefinition Mist) SyncAndBuild()
        {
            BlenderSync.SyncFiles(BlenderLiquidCatalog.Folder, new string[0], BlenderLiquidCatalog.Report);
            foreach ((string folder, string[] files) in BlenderLiquidCatalog.Files())
            {
                BlenderSync.SyncFiles($"{BlenderLiquidCatalog.Folder}/{folder}", files, string.Empty);
            }

            var looks = new List<LiquidLook>();
            foreach (BlenderLiquidCatalog.Row row in BlenderLiquidCatalog.Rows)
            {
                Material high = BuildMaterial(row, false);
                if (high != null)
                {
                    looks.Add(new LiquidLook(row.Kind, high, BuildMaterial(row, true), row.BankLight));
                }
            }

            BlenderLiquidCatalog.Row corruption = BlenderLiquidCatalog.Corruption;
            Material corruptHigh = BuildMaterial(corruption, false);
            var set = BlenderArtBuilder.LoadOrCreate<LiquidSet>(SetPath);
            var so = new SerializedObject(set);
            WriteArray(so.FindProperty("_looks"), looks);
            so.FindProperty("_corruption").boxedValue = new LiquidLook(corruption.Kind, corruptHigh, corruptHigh != null ? BuildMaterial(corruption, true) : null, Color.black);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            return (set, BuildMist());
        }

        /// <summary>The generator step: points every theme at what <see cref="SyncAndBuild"/> made.</summary>
        public static int ApplyBuilt((LiquidSet Set, MistDefinition Mist) built) => Apply(built.Set, built.Mist);

        /// <summary>
        /// Points the theme of every level at the set and the mist (the menu and cutscene themes keep their own look);
        /// returns how many themes wear them.
        /// </summary>
        public static int Apply(LiquidSet set, MistDefinition mist)
        {
            var levelThemes = new HashSet<VisualTheme>();
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (level != null && level.Theme != null)
                {
                    levelThemes.Add(level.Theme);
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:VisualTheme", new[] { ThemeFolder }))
            {
                var theme = AssetDatabase.LoadAssetAtPath<VisualTheme>(AssetDatabase.GUIDToAssetPath(guid));
                if (theme == null)
                {
                    continue;
                }

                bool wears = levelThemes.Contains(theme);
                var so = new SerializedObject(theme);
                so.FindProperty("_liquids").objectReferenceValue = wears ? set : null;
                so.FindProperty("_mist").objectReferenceValue = wears ? mist : null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return levelThemes.Count;
        }

        private static Material BuildMaterial(BlenderLiquidCatalog.Row row, bool low)
        {
            string folder = $"{BlenderSync.TargetRoot}/{BlenderLiquidCatalog.Folder}/{row.Name}";
            string maps = low ? $"{folder}/{BlenderSync.LowFolder}" : folder;
            if (!File.Exists($"{folder}/{row.Maps[0].File}.png"))
            {
                Debug.LogWarning($"Blender liquids: no maps for {row.Name} at {folder}.");
                return null;
            }

            string path = $"{MaterialFolder}/Liquid_{row.Name}{(low ? "_Low" : string.Empty)}.mat";
            Shader shader = Shader.Find(LiquidShader);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                EnsureFolder(MaterialFolder);
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.shaderKeywords = new string[0];
            material.EnableKeyword(row.Keyword);
            material.SetFloat("_Liquid", KindIndex(row.Keyword));
            material.SetFloat("_Low", low ? 1f : 0f);
            material.SetFloat("_Refract", !low && row.Refracts ? 1f : 0f);
            if (low)
            {
                material.EnableKeyword(LowKeyword);
            }
            else if (row.Refracts)
            {
                material.EnableKeyword(RefractKeyword);
            }

            foreach (BlenderLiquidCatalog.Map map in row.Maps)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{maps}/{map.File}.png") ?? AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{map.File}.png");
                material.SetTexture(map.Slot, texture);
            }

            foreach (KeyValuePair<string, Vector4> pair in row.Vectors)
            {
                material.SetVector(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, Color> pair in row.Colours)
            {
                material.SetColor(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, float> pair in row.Floats)
            {
                material.SetFloat(pair.Key, pair.Value);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        // The index of the kind in the shader's KeywordEnum, so the inspector shows the same kind the keyword says.
        private static float KindIndex(string keyword)
        {
            string[] order = { "_LIQUID_WATER", "_LIQUID_LAVA", "_LIQUID_BOG", "_LIQUID_QUICKSAND", "_LIQUID_ICE", "_LIQUID_CORRUPTION" };
            return System.Array.IndexOf(order, keyword);
        }

        private static MistDefinition BuildMist()
        {
            string folder = $"{BlenderSync.TargetRoot}/{BlenderLiquidCatalog.Folder}/{BlenderLiquidCatalog.MistFolder}";
            var layers = new List<MistLayer>();
            for (int i = 0; i < BlenderLiquidCatalog.MistLayers.Length; i++)
            {
                string file = BlenderLiquidCatalog.MistLayers[i];
                var density = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{file}.png");
                if (density == null)
                {
                    continue;
                }

                var lowDensity = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{BlenderSync.LowFolder}/{file}.png");
                (float height, float alpha, float tile, Vector2 scroll) = BlenderLiquidCatalog.MistLook[i];
                layers.Add(new MistLayer(density, lowDensity, height, alpha, tile, scroll));
            }

            var mist = BlenderArtBuilder.LoadOrCreate<MistDefinition>(MistPath);
            var so = new SerializedObject(mist);
            so.FindProperty("_shader").objectReferenceValue = Shader.Find(MistShader);
            WriteArray(so.FindProperty("_layers"), layers);
            so.FindProperty("_colour").colorValue = BlenderLiquidCatalog.MistColour;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mist);
            return mist;
        }

        private static void WriteArray<T>(SerializedProperty array, IList<T> items)
        {
            array.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                array.GetArrayElementAtIndex(i).boxedValue = items[i];
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
