using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Drones;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Turns the synced Blender exports of our own side into game data (import stage 3a): one <c>Game/UnitLit</c> material
    /// per export and quality profile, a <see cref="ModelArtDefinition"/> per model file with its clips by role, the gate's
    /// <see cref="GateArtDefinition"/> and the base's <see cref="BaseArtDefinition"/>, and every owner pointed at its art:
    /// the gate towers, the visual themes (base and resource node), the drones and the path objects. Re-running updates
    /// the same assets in place.
    /// </summary>
    public static class BlenderColonyBuilder
    {
        public const string ColonyArtFolder = BlenderArtBuilder.ArtDataFolder + "/Colony";
        private const string DataFolder = "Assets/_Project/Data";
        private const string PreviewPrefix = "__preview__";

        // The exports face -Z once imported (README: "faces -Y in Blender"); our owners face +Z.
        private const float ExportYaw = 180f;

        // Below this share of the screen height a model shows its far LOD; drones and gadgets are small at the play zoom.
        private const float StructureLodScreenHeight = 0.05f;
        private const float SmallLodScreenHeight = 0.02f;

        // The README's span is the distance between the pylon centres from the road centre; the gate is twice that wide.
        private const float GateWidthPerHalfSpan = 2f;

        // The ore vein is built 0.8 across for a 1-cell node; at the play zoom (imp3_r1_mine_full) its gold read small and
        // dim next to the procedural cluster it replaces, so it fills more of its cell and its ore glows hotter.
        private const float ResourceNodeScale = 1.2f;
        private const float ResourceNodeEmission = 3f;

        /// <summary>The colony art built in one run, by owner.</summary>
        public sealed class Built
        {
            public GateArtDefinition Gate;
            public BaseArtDefinition Base;
            public ModelArtDefinition ResourceNode;
            public readonly Dictionary<string, ModelArtDefinition> ByAsset = new Dictionary<string, ModelArtDefinition>();
        }

        [MenuItem("Tools/Generate/Blender Colony (sync, gate, base, drones, gadgets)")]
        public static void BuildMenu()
        {
            Built built = SyncAndBuild();
            int assigned = Apply(built);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender colony: {built.ByAsset.Count} models, {assigned} owners wear one.");
        }

        /// <summary>Syncs the exports, then builds every model that is there.</summary>
        public static Built SyncAndBuild()
        {
            BlenderSync.Sync(BlenderColonyCatalog.SyncedAssets(), BlenderColonyCatalog.ExtraModels());
            var built = new Built
            {
                Gate = BuildGate(),
                Base = BuildBase(),
                ResourceNode = BuildSingle(BlenderColonyCatalog.ResourceNodeAsset, StructureLodScreenHeight, ResourceNodeEmission, ResourceNodeScale),
            };

            foreach (string asset in BlenderColonyCatalog.Drones.Values)
            {
                Add(built, asset, BuildSingle(asset, SmallLodScreenHeight));
            }

            foreach (string asset in BlenderColonyCatalog.PathObjects.Values)
            {
                Add(built, asset, BuildSingle(asset, SmallLodScreenHeight));
            }

            foreach (string asset in BlenderColonyCatalog.Unworn)
            {
                Add(built, asset, BuildSingle(asset, SmallLodScreenHeight));
            }

            return built;
        }

        /// <summary>Points every owner at its art (or at none when the export is missing); returns how many wear one.</summary>
        public static int Apply(Built built)
        {
            int assigned = 0;
            foreach (TowerDefinition tower in Load<TowerDefinition>())
            {
                if (tower.BlocksGroundEnemies)
                {
                    assigned += Assign(tower, "_gateArt", built.Gate);
                }
            }

            foreach (VisualTheme theme in Load<VisualTheme>())
            {
                assigned += Assign(theme, "_baseArt", built.Base);
                assigned += Assign(theme, "_resourceNodeArt", built.ResourceNode);
            }

            foreach (DroneDefinition drone in Load<DroneDefinition>())
            {
                assigned += Assign(drone, "_art", Find(built, BlenderColonyCatalog.Drones, drone.Kind));
            }

            foreach (PathObjectDefinition pathObject in Load<PathObjectDefinition>())
            {
                assigned += Assign(pathObject, "_art", Find(built, BlenderColonyCatalog.PathObjects, pathObject.Look));
            }

            return assigned;
        }

        private static void Add(Built built, string asset, ModelArtDefinition art)
        {
            if (art != null)
            {
                built.ByAsset[asset] = art;
            }
        }

        private static ModelArtDefinition Find<TKey>(Built built, IReadOnlyDictionary<TKey, string> catalog, TKey key)
        {
            return catalog.TryGetValue(key, out string asset) && built.ByAsset.TryGetValue(asset, out ModelArtDefinition art) ? art : null;
        }

        private static int Assign(Object owner, string field, Object art)
        {
            var so = new SerializedObject(owner);
            so.FindProperty(field).objectReferenceValue = art;
            so.ApplyModifiedPropertiesWithoutUndo();
            return art != null ? 1 : 0;
        }

        private static IEnumerable<T> Load<T>() where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { DataFolder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    yield return asset;
                }
            }
        }

        // One export, one model file named after it, one material pair.
        private static ModelArtDefinition BuildSingle(string asset, float lodScreenHeight, float emission = BlenderArtEntry.TowerEmission, float scale = 1f)
        {
            string name = Path.GetFileName(asset);
            (Material high, Material low) = Materials(asset, emission);
            return high == null ? null : BuildArt(name, $"{BlenderArtBuilder.AssetFolder(asset)}/{name}.fbx", high, low, lodScreenHeight, scale);
        }

        // The gate: one model per level on one map set, the wreck, the loose panels and the width from the report.
        private static GateArtDefinition BuildGate()
        {
            string asset = BlenderColonyCatalog.GateAsset;
            string folder = BlenderArtBuilder.AssetFolder(asset);
            (Material high, Material low) = Materials(asset);
            if (high == null)
            {
                return null;
            }

            var levels = new List<ModelArtDefinition>();
            foreach (string level in BlenderColonyCatalog.GateLevels)
            {
                ModelArtDefinition art = BuildArt($"{asset}_{level}", $"{folder}/{asset}_{level}.fbx", high, low, StructureLodScreenHeight);
                if (art != null)
                {
                    levels.Add(art);
                }
            }

            BlenderReport report = BlenderReport.Load($"{folder}/{asset}_report.json");
            var gate = BlenderArtBuilder.LoadOrCreate<GateArtDefinition>($"{ColonyArtFolder}/{asset}Art.asset");
            var so = new SerializedObject(gate);
            WriteArray(so.FindProperty("_levels"), levels);
            so.FindProperty("_wreck").objectReferenceValue = BuildArt(BlenderColonyCatalog.GateWreck, $"{folder}/{BlenderColonyCatalog.GateWreck}.fbx", high, low, StructureLodScreenHeight);
            float halfSpan = report.Number("pylon_centres_x");
            if (halfSpan > 0f)
            {
                so.FindProperty("_modelWidth").floatValue = halfSpan * GateWidthPerHalfSpan;
            }

            List<string> loose = report.Strings("damaged_state_hidden_hexes");
            SerializedProperty parts = so.FindProperty("_looseParts");
            parts.arraySize = loose.Count;
            for (int i = 0; i < loose.Count; i++)
            {
                parts.GetArrayElementAtIndex(i).stringValue = loose[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gate);
            return gate;
        }

        // The base: the modules, the damage overlay and the wreck, all on the base's map set.
        private static BaseArtDefinition BuildBase()
        {
            string asset = BlenderColonyCatalog.BaseAsset;
            string folder = BlenderArtBuilder.AssetFolder(asset);
            (Material high, Material low) = Materials(asset);
            if (high == null)
            {
                return null;
            }

            var art = BlenderArtBuilder.LoadOrCreate<BaseArtDefinition>($"{ColonyArtFolder}/{asset}Art.asset");
            var so = new SerializedObject(art);
            so.FindProperty("_model").objectReferenceValue = BuildArt(asset, $"{folder}/{asset}.fbx", high, low, StructureLodScreenHeight);
            so.FindProperty("_damage").objectReferenceValue = BuildArt(BlenderColonyCatalog.BaseDamage, $"{folder}/{BlenderColonyCatalog.BaseDamage}.fbx", high, low, StructureLodScreenHeight);
            so.FindProperty("_wreck").objectReferenceValue = BuildArt(BlenderColonyCatalog.BaseWreck, $"{folder}/{BlenderColonyCatalog.BaseWreck}.fbx", high, low, StructureLodScreenHeight);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        // The export's High and Low material, rimmed in the colony's cool light, or nulls when the export is not synced.
        private static (Material High, Material Low) Materials(string asset, float emission = BlenderArtEntry.TowerEmission)
        {
            string name = Path.GetFileName(asset);
            string folder = BlenderArtBuilder.AssetFolder(asset);
            if (!File.Exists($"{folder}/{name}_Albedo.png"))
            {
                Debug.LogWarning($"Blender colony: no maps for '{asset}' at {folder}.");
                return (null, null);
            }

            BlenderReport report = BlenderReport.Load($"{folder}/{name}_report.json");
            var look = new BlenderArtEntry(name, name)
            {
                Emission = emission,
                Rim = BlenderArtEntry.TowerRim,
                Colony = true,
            };

            Material high = BlenderArtBuilder.BuildMaterial(look, report, folder, name, false);
            Material low = BlenderArtBuilder.BuildMaterial(look, report, $"{folder}/{BlenderSync.LowFolder}", name + "_Low", true);
            return (high, low);
        }

        private static ModelArtDefinition BuildArt(string name, string modelPath, Material high, Material low, float lodScreenHeight, float scale = 1f)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogWarning($"Blender colony: no model at {modelPath}.");
                return null;
            }

            var art = BlenderArtBuilder.LoadOrCreate<ModelArtDefinition>($"{ColonyArtFolder}/{name}.asset");
            var so = new SerializedObject(art);
            so.FindProperty("_model").objectReferenceValue = model;
            so.FindProperty("_lod1").objectReferenceValue = null;
            so.FindProperty("_material").objectReferenceValue = high;
            so.FindProperty("_lowMaterial").objectReferenceValue = low;
            so.FindProperty("_scale").floatValue = scale;
            so.FindProperty("_yaw").floatValue = ExportYaw;
            so.FindProperty("_bodyCentrePivot").boolValue = false;
            so.FindProperty("_height").floatValue = BlenderArtBuilder.ModelTop(model);
            so.FindProperty("_lodScreenHeight").floatValue = lodScreenHeight;
            so.FindProperty("_deathFade").vector2Value = Vector2.zero;
            so.FindProperty("_embers").boolValue = false;
            so.FindProperty("_bleeds").boolValue = false;
            WriteClips(so.FindProperty("_clips"), modelPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        private static void WriteClips(SerializedProperty array, string modelPath)
        {
            var clips = new List<(ModelClipRole Role, AnimationClip Clip)>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith(PreviewPrefix) && BlenderColonyCatalog.Roles.TryGetValue(clip.name, out ModelClipRole role))
                {
                    clips.Add((role, clip));
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
                element.FindPropertyRelative("_socket").stringValue = string.Empty;
                element.FindPropertyRelative("_weight").floatValue = 1f;
            }
        }

        private static void WriteArray(SerializedProperty array, List<ModelArtDefinition> items)
        {
            array.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }
    }
}
