using System.Collections.Generic;
using Game.Core;
using Game.Towers;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Turns the synced Blender tower exports into game data (import stage 2): one <c>Game/UnitLit</c> material per tower
    /// and quality profile (its tiers share one map set), one <see cref="ModelArtDefinition"/> per tier with its clips by
    /// role, one <see cref="TowerArtDefinition"/> per tower mapping levels to tiers (level 3 on path A by default, path B
    /// recorded beside it), the shared wreck, and the tower definitions pointed at their art. Re-running updates the same
    /// assets in place.
    /// </summary>
    public static class BlenderTowerBuilder
    {
        public const string TowerArtFolder = BlenderArtBuilder.ArtDataFolder + "/Towers";
        private const string TowerDataFolder = "Assets/_Project/Data/Towers";
        private const string FirePrefix = "Fire_";
        private const string CutPathTier = "T3B";

        // Below this share of the screen height a tower shows its far LOD: at the play zoom a tower is about 7 %.
        private const float TowerLodScreenHeight = 0.05f;

        private static readonly Dictionary<string, ModelClipRole> RolesByName = new Dictionary<string, ModelClipRole>
        {
            { "IdleSweep", ModelClipRole.Idle },
            { "ChargeUp", ModelClipRole.Charge },
            { "Beam", ModelClipRole.Firing },
            { "Spin", ModelClipRole.Firing },
            { "FireAlternate", ModelClipRole.Firing },
            { "Spray", ModelClipRole.Firing },
        };

        [MenuItem("Tools/Generate/Blender Towers (sync, materials, towers)")]
        public static void BuildMenu()
        {
            Dictionary<string, TowerArtDefinition> art = SyncAndBuild();
            int assigned = ApplyToTowers(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender towers: {art.Count} tower art definitions, {assigned} towers wear a model.");
        }

        /// <summary>
        /// Takes the level-3 path-B looks out of the art data that exists (round 67: the A/B paths were cut): the tier rows
        /// on every tower art definition and the tier's own art asset. What a fresh build writes; safe to run again.
        /// </summary>
        public static string DropCutPathLooks()
        {
            int rows = 0;
            int assets = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:TowerArtDefinition", new[] { TowerArtFolder }))
            {
                var art = AssetDatabase.LoadAssetAtPath<TowerArtDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var so = new SerializedObject(art);
                SerializedProperty tiers = so.FindProperty("_tiers");
                for (int i = tiers.arraySize - 1; i >= 0; i--)
                {
                    if (tiers.GetArrayElementAtIndex(i).FindPropertyRelative("_tier").stringValue == CutPathTier)
                    {
                        tiers.DeleteArrayElementAtIndex(i);
                        rows++;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                // The dropped path fields leave the file only when it is written again.
                EditorUtility.SetDirty(art);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:ModelArtDefinition", new[] { TowerArtFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path).EndsWith("_" + CutPathTier))
                {
                    AssetDatabase.DeleteAsset(path);
                    assets++;
                }
            }

            AssetDatabase.SaveAssets();
            return $"Tower art: {rows} path-B tier rows and {assets} path-B art assets removed.\n";
        }

        /// <summary>Syncs the tower exports, then builds every tower row whose tiers are there; returns them by asset.</summary>
        public static Dictionary<string, TowerArtDefinition> SyncAndBuild()
        {
            BlenderSync.Sync(BlenderTowerCatalog.SyncedAssets(), BlenderTowerCatalog.TierFiles());
            ModelArtDefinition wreck = BuildWreck();
            GameObject star = AssetDatabase.LoadAssetAtPath<GameObject>($"{BlenderArtBuilder.AssetFolder(BlenderTowerCatalog.StarAsset)}/{BlenderTowerCatalog.StarAsset}.fbx");
            var built = new Dictionary<string, TowerArtDefinition>();
            foreach (BlenderTowerEntry entry in BlenderTowerCatalog.Towers)
            {
                TowerArtDefinition art = Build(entry, wreck, star);
                if (art != null)
                {
                    built[entry.Asset] = art;
                }
            }

            return built;
        }

        /// <summary>Points every catalogued tower definition at its art; returns how many wear a model.</summary>
        public static int ApplyToTowers(IReadOnlyDictionary<string, TowerArtDefinition> art)
        {
            int assigned = 0;
            foreach (BlenderTowerEntry entry in BlenderTowerCatalog.Towers)
            {
                if (entry.Tower == null)
                {
                    continue;
                }

                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{TowerDataFolder}/{entry.Tower}.asset");
                if (tower == null)
                {
                    Debug.LogWarning($"Blender towers: no tower definition '{entry.Tower}' for {entry.Asset}.");
                    continue;
                }

                art.TryGetValue(entry.Asset, out TowerArtDefinition wear);
                var so = new SerializedObject(tower);
                so.FindProperty("_art").objectReferenceValue = wear;
                so.ApplyModifiedPropertiesWithoutUndo();
                assigned += wear != null ? 1 : 0;
            }

            return assigned;
        }

        private static TowerArtDefinition Build(BlenderTowerEntry entry, ModelArtDefinition wreck, GameObject star)
        {
            string folder = BlenderArtBuilder.AssetFolder(entry.Asset);
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{entry.ModelFile(entry.Tiers[0])}") == null)
            {
                Debug.LogWarning($"Blender towers '{entry.Asset}': no model at {folder}/{entry.ModelFile(entry.Tiers[0])}.");
                return null;
            }

            BlenderReport report = BlenderReport.Load($"{folder}/{entry.Asset}_report.json");
            BlenderArtEntry look = entry.MaterialEntry();
            Material high = BlenderArtBuilder.BuildMaterial(look, report, folder, entry.Asset, false);
            Material low = BlenderArtBuilder.BuildMaterial(look, report, $"{folder}/{BlenderSync.LowFolder}", entry.Asset + "_Low", true);

            var tiers = new List<(string Tier, int Level, ModelArtDefinition Art)>();
            for (int i = 0; i < entry.Tiers.Length; i++)
            {
                string tier = entry.Tiers[i];
                ModelArtDefinition art = BuildTier(entry, $"{folder}/{entry.ModelFile(tier)}", tier, high, low);
                if (art == null)
                {
                    continue;
                }

                tiers.Add((tier, Mathf.Min(i, TowerDefinition.LevelCount - 1), art));
            }

            var tower = BlenderArtBuilder.LoadOrCreate<TowerArtDefinition>($"{TowerArtFolder}/{entry.Asset}.asset");
            var so = new SerializedObject(tower);
            SerializedProperty array = so.FindProperty("_tiers");
            array.arraySize = tiers.Count;
            for (int i = 0; i < tiers.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_tier").stringValue = tiers[i].Tier;
                element.FindPropertyRelative("_levelIndex").intValue = tiers[i].Level;
                element.FindPropertyRelative("_art").objectReferenceValue = tiers[i].Art;
            }

            so.FindProperty("_aim").enumValueIndex = (int)entry.Aim;
            so.FindProperty("_muzzlePrefix").stringValue = entry.MuzzlePrefix;
            so.FindProperty("_muzzleChoice").enumValueIndex = (int)entry.MuzzleChoice;
            so.FindProperty("_chargeGlow").floatValue = entry.ChargeGlow;
            so.FindProperty("_hideStars").boolValue = entry.HideStars;
            so.FindProperty("_wreck").objectReferenceValue = entry.Debris != null ? wreck : null;
            so.FindProperty("_debris").stringValue = entry.Debris ?? string.Empty;
            so.FindProperty("_star").objectReferenceValue = star;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tower);
            return tower;
        }

        private static ModelArtDefinition BuildTier(BlenderTowerEntry entry, string modelPath, string tier, Material high, Material low)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogWarning($"Blender towers '{entry.Asset}': no tier {tier} at {modelPath}.");
                return null;
            }

            var art = BlenderArtBuilder.LoadOrCreate<ModelArtDefinition>($"{TowerArtFolder}/{entry.Asset}_{tier}.asset");
            var so = new SerializedObject(art);
            so.FindProperty("_model").objectReferenceValue = model;
            so.FindProperty("_lod1").objectReferenceValue = null;
            so.FindProperty("_material").objectReferenceValue = high;
            so.FindProperty("_lowMaterial").objectReferenceValue = low;
            so.FindProperty("_scale").floatValue = 1f;
            so.FindProperty("_bodyCentrePivot").boolValue = false;
            so.FindProperty("_height").floatValue = BlenderArtBuilder.ModelTop(model);
            so.FindProperty("_lodScreenHeight").floatValue = TowerLodScreenHeight;
            so.FindProperty("_deathFade").vector2Value = Vector2.zero;
            so.FindProperty("_embers").boolValue = false;
            so.FindProperty("_bleeds").boolValue = false;
            WriteClips(so.FindProperty("_clips"), modelPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        // Every take by role: the cycles and the wind-up by name, and Fire_n as the recoil of the barrel ending in Muzzle_n.
        private static void WriteClips(SerializedProperty array, string modelPath)
        {
            var clips = new List<(ModelClipRole Role, AnimationClip Clip, string Socket)>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (!(asset is AnimationClip clip) || clip.name.StartsWith("__preview__"))
                {
                    continue;
                }

                if (clip.name.StartsWith(FirePrefix))
                {
                    string barrel = clip.name.Substring(FirePrefix.Length);
                    clips.Add((ModelClipRole.Fire, clip, $"{TowerArtDefinition.DefaultMuzzlePrefix}_{barrel}"));
                }
                else if (RolesByName.TryGetValue(clip.name, out ModelClipRole role))
                {
                    clips.Add((role, clip, string.Empty));
                }
            }

            array.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_role").enumValueIndex = (int)clips[i].Role;
                element.FindPropertyRelative("_clip").objectReferenceValue = clips[i].Clip;
                element.FindPropertyRelative("_farClip").objectReferenceValue = null;
                element.FindPropertyRelative("_distancePerLoop").floatValue = 0f;
                element.FindPropertyRelative("_eventSeconds").floatValue = 0f;
                element.FindPropertyRelative("_trigger").stringValue = string.Empty;
                element.FindPropertyRelative("_socket").stringValue = clips[i].Socket;
                element.FindPropertyRelative("_weight").floatValue = 1f;
            }
        }

        // The shared wreck: burnt pad, husk and every tower's debris in one static model; each tower keeps its own debris.
        private static ModelArtDefinition BuildWreck()
        {
            string asset = BlenderTowerCatalog.WreckAsset;
            string folder = BlenderArtBuilder.AssetFolder(asset);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{asset}.fbx");
            if (model == null)
            {
                Debug.LogWarning($"Blender towers: no wreck at {folder}/{asset}.fbx.");
                return null;
            }

            BlenderReport report = BlenderReport.Load($"{folder}/{asset}_report.json");
            var look = new BlenderArtEntry(asset, asset) { Emission = BlenderArtEntry.WreckEmission, Brightness = BlenderArtEntry.WreckBrightness, Rim = BlenderArtEntry.TowerRim, Colony = true };
            Material high = BlenderArtBuilder.BuildMaterial(look, report, folder, asset, false);
            Material low = BlenderArtBuilder.BuildMaterial(look, report, $"{folder}/{BlenderSync.LowFolder}", asset + "_Low", true);
            var art = BlenderArtBuilder.LoadOrCreate<ModelArtDefinition>($"{TowerArtFolder}/{asset}.asset");
            var so = new SerializedObject(art);
            so.FindProperty("_model").objectReferenceValue = model;
            so.FindProperty("_material").objectReferenceValue = high;
            so.FindProperty("_lowMaterial").objectReferenceValue = low;
            so.FindProperty("_scale").floatValue = 1f;
            so.FindProperty("_height").floatValue = BlenderArtBuilder.ModelTop(model);
            so.FindProperty("_bleeds").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }
    }
}
