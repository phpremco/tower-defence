using System.Collections.Generic;
using Game.UI;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// ArtSource README round 25: syncs the UI exports, cuts every sprite the JSON files list (rect, nine-slice border,
    /// pivot) out of its sheet as a sub-asset of <see cref="UiArtSet"/>, hands the pictures to the set and maps the kit
    /// onto the UI skin. A re-run keeps a sprite whose cut did not change, so references and the file stay stable.
    /// </summary>
    public static class BlenderUiBuilder
    {
        public const string Folder = "UI";
        public const string ArtPath = BlenderArtBuilder.ArtDataFolder + "/UI/UiArtSet.asset";
        private const string SkinPath = "Assets/_Project/Settings/UiSkin.asset";
        private const string Report = "UI_report.json";
        // README: the kit and dialogue sheets are 2x masters, so 200 pixels per unit reads their borders at 1x.
        private const float MasterPixelsPerUnit = 200f;
        private const string IconSheet = "UI_Icons";
        private const string IconSheet2x = "UI_Icons@2x";

        private static readonly string[] Files =
        {
            "UI_Icons.png", "UI_Icons@2x.png", "UI_Kit.png", "UI_Dialogue.png", "UI_Dialogue_Noise.png", "UI_Dialogue_Scanlines.png",
            "UI_Dialogue_Radial.png", "UI_Dialogue_Shade.png", "UI_Radar.png", "UI_Starfield.png", "UI_PreviewBackdrop.png", "UI_MapHalo.png",
            "UI_Icons.json", "UI_Kit.json", "UI_Dialogue.json",
        };

        // UiSkin field, kit sprite. Buttons wear the light grey Neutral frame so the colour-tint focus path keeps working.
        private static readonly (string Field, string Sprite)[] SkinSprites =
        {
            ("_panel", "Panel"), ("_panelSquare", "PanelSquare"), ("_button", "Button_Neutral"), ("_buttonSquare", "ButtonSquare_Neutral"),
            ("_barBack", "Bar_Back"), ("_barBlue", "Bar_Blue"), ("_barGreen", "Bar_Green"), ("_barYellow", "Bar_Yellow"), ("_barRed", "Bar_Red"),
            ("_cardBlue", "Card_Blue"), ("_cardGreen", "Card_Green"), ("_cardYellow", "Card_Yellow"), ("_cardRed", "Card_Red"),
        };

        /// <summary>True for a texture of the UI export (the importer gives them sprite-sheet settings).</summary>
        public static bool IsUiTexture(string path) => path.StartsWith(BlenderSync.TargetRoot + "/" + Folder + "/", System.StringComparison.Ordinal);

        [MenuItem("Tools/Generate/Blender UI (sync, icons, kit, dialogue, pictures)")]
        public static void BuildMenu()
        {
            UiArtSet art = SyncAndBuild();
            bool applied = ApplyToSkin(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender UI: {(art != null ? "built" : "missing")}, skin {(applied ? "wears it" : "unchanged")}.");
        }

        public static UiArtSet SyncAndBuild()
        {
            BlenderSync.SyncFiles(Folder, Files, Report);
            string folder = BlenderArtBuilder.AssetFolder(Folder);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/UI_Kit.png") == null)
            {
                Debug.LogWarning($"Blender UI: no UI_Kit.png at {folder}.");
                return null;
            }

            var art = BlenderArtBuilder.LoadOrCreate<UiArtSet>(ArtPath);
            var existing = new Dictionary<string, Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ArtPath))
            {
                if (asset is Sprite sprite)
                {
                    existing[sprite.name] = sprite;
                }
            }

            var entries = new List<(string Name, Sprite Sprite)>();
            Cut(art, existing, entries, folder, "UI_Kit", "UI_Kit", 1f, MasterPixelsPerUnit);
            Cut(art, existing, entries, folder, "UI_Dialogue", "UI_Dialogue", 1f, MasterPixelsPerUnit);
            // Icons are shown from 24 to 96 px: cut from the 2x sheet (rects doubled), white glyphs tinted as before.
            Cut(art, existing, entries, folder, IconSheet, IconSheet2x, 2f, MasterPixelsPerUnit);
            foreach (KeyValuePair<string, Sprite> stale in existing)
            {
                Object.DestroyImmediate(stale.Value, true);
            }

            var so = new SerializedObject(art);
            SerializedProperty sprites = so.FindProperty("_sprites");
            sprites.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty entry = sprites.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Name").stringValue = entries[i].Name;
                entry.FindPropertyRelative("Sprite").objectReferenceValue = entries[i].Sprite;
            }

            SetTexture(so, "_starfield", folder, "UI_Starfield");
            SetTexture(so, "_previewBackdrop", folder, "UI_PreviewBackdrop");
            SetTexture(so, "_mapHalo", folder, "UI_MapHalo");
            SetTexture(so, "_radar", folder, "UI_Radar");
            SetTexture(so, "_dialogueNoise", folder, "UI_Dialogue_Noise");
            SetTexture(so, "_dialogueScanlines", folder, "UI_Dialogue_Scanlines");
            SetTexture(so, "_dialogueRadial", folder, "UI_Dialogue_Radial");
            SetTexture(so, "_dialogueShade", folder, "UI_Dialogue_Shade");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssetIfDirty(art);
            Debug.Log($"Blender UI: {entries.Count} sprites.");
            return art;
        }

        /// <summary>Maps the kit onto the skin and hands it the set; false when there is nothing to hand.</summary>
        public static bool ApplyToSkin(UiArtSet art)
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            return skin != null && art != null && ApplyToSkin(skin, art);
        }

        public static bool ApplyToSkin(UiSkin skin, UiArtSet art)
        {
            if (art == null)
            {
                return false;
            }

            var so = new SerializedObject(skin);
            so.FindProperty("_art").objectReferenceValue = art;
            foreach ((string field, string sprite) in SkinSprites)
            {
                so.FindProperty(field).objectReferenceValue = FindSprite(art, sprite);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skin);
            return true;
        }

        private static Sprite FindSprite(UiArtSet art, string name)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(art)))
            {
                if (asset is Sprite sprite && sprite.name == name)
                {
                    return sprite;
                }
            }

            return null;
        }

        // Every sprite of a JSON sheet: rect (bottom-left based, times the sheet scale), border (left, bottom, right, top) and pivot.
        private static void Cut(UiArtSet art, Dictionary<string, Sprite> existing, List<(string, Sprite)> entries, string folder, string json, string sheet, float scale, float pixelsPerUnit)
        {
            string path = $"{folder}/{json}.json";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{sheet}.png");
            if (!System.IO.File.Exists(path) || texture == null)
            {
                Debug.LogWarning($"Blender UI: no {json}.json or {sheet}.png.");
                return;
            }

            Dictionary<string, object> sprites = BlenderJson.Object(BlenderJson.Field(BlenderJson.Object(BlenderJson.Parse(System.IO.File.ReadAllText(path))), "sprites"));
            foreach (KeyValuePair<string, object> pair in sprites)
            {
                Dictionary<string, object> spec = BlenderJson.Object(pair.Value);
                List<object> r = BlenderJson.Array(BlenderJson.Field(spec, "unity_rect"));
                var rect = new Rect(BlenderJson.Number(r[0]) * scale, BlenderJson.Number(r[1]) * scale, BlenderJson.Number(r[2]) * scale, BlenderJson.Number(r[3]) * scale);
                List<object> b = BlenderJson.Array(BlenderJson.Field(spec, "border_lbrt"));
                Vector4 border = b.Count == 4 ? new Vector4(BlenderJson.Number(b[0]), BlenderJson.Number(b[1]), BlenderJson.Number(b[2]), BlenderJson.Number(b[3])) * scale : Vector4.zero;
                List<object> p = BlenderJson.Array(BlenderJson.Field(spec, "pivot"));
                Vector2 pivot = p.Count == 2 ? new Vector2(BlenderJson.Number(p[0]), BlenderJson.Number(p[1])) : new Vector2(0.5f, 0.5f);
                entries.Add((pair.Key, Keep(art, existing, pair.Key, texture, rect, pivot, border, pixelsPerUnit)));
            }
        }

        private static Sprite Keep(UiArtSet art, Dictionary<string, Sprite> existing, string name, Texture2D texture, Rect rect, Vector2 pivot, Vector4 border, float pixelsPerUnit)
        {
            if (existing.TryGetValue(name, out Sprite sprite))
            {
                existing.Remove(name);
                bool same = sprite.texture == texture && sprite.rect == rect && sprite.border == border && Mathf.Approximately(sprite.pixelsPerUnit, pixelsPerUnit)
                            && (sprite.pivot / rect.size - pivot).sqrMagnitude < 0.0001f;
                if (same)
                {
                    return sprite;
                }

                Object.DestroyImmediate(sprite, true);
            }

            sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            AssetDatabase.AddObjectToAsset(sprite, art);
            return sprite;
        }

        private static void SetTexture(SerializedObject so, string field, string folder, string file)
        {
            so.FindProperty(field).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{file}.png");
        }

        /// <summary>README round 25 import settings: colour pictures with straight alpha; the sheets keep their size (no POT scaling).</summary>
        public static void Preprocess(TextureImporter importer, string file)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = file.StartsWith(IconSheet, System.StringComparison.Ordinal);
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (file == "UI_Dialogue_Noise" || file == "UI_Dialogue_Scanlines")
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            }
        }
    }
}
