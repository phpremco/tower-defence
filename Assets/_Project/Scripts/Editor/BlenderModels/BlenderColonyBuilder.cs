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

        // The base report's sockets for its own guns (left first: one gun at base weapon level one) and its drone docks.
        private static readonly string[] BaseTurretSockets = { "TurretLink_L", "TurretLink_R" };
        private const string BaseDockPrefix = "Socket_DroneBay";

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

        [MenuItem("Tools/Generate/Blender Colony Flag (sync, flag on the base)")]
        public static void RebuildFlagMenu()
        {
            Debug.Log($"Blender colony flag: {(RebuildFlag() ? "the base flies the flag" : "no flag export or no base art")}.");
        }

        /// <summary>
        /// Syncs and builds only the colony flag and points the base art at it, leaving every other colony export alone.
        /// True when the base art flies a flag afterwards.
        /// </summary>
        public static bool RebuildFlag()
        {
            BlenderSync.Sync(new[] { BlenderColonyCatalog.FlagAsset });
            var art = AssetDatabase.LoadAssetAtPath<BaseArtDefinition>($"{ColonyArtFolder}/{BlenderColonyCatalog.BaseAsset}Art.asset");
            if (art == null)
            {
                return false;
            }

            var so = new SerializedObject(art);
            WriteFlag(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return art.Flag != null;
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
            WriteFootprint(so, so.FindProperty("_model").objectReferenceValue as ModelArtDefinition);
            WriteFlag(so);
            List<string> order = BlenderReport.Load($"{folder}/{asset}_report.json").Strings("timelapse_order");
            SerializedProperty modules = so.FindProperty("_timelapseOrder");
            modules.arraySize = order.Count;
            for (int i = 0; i < order.Count; i++)
            {
                modules.GetArrayElementAtIndex(i).stringValue = $"{asset}_{order[i]}";
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        // The NTI flag (Spencer pass 3/4): its model and where its report puts the pole in the base export's frame (pass 4's
        // courtyard spot behind the command post). Without the export the base flies no flag.
        private static void WriteFlag(SerializedObject so)
        {
            string asset = BlenderColonyCatalog.FlagAsset;
            ModelArtDefinition flag = BuildSingle(asset, SmallLodScreenHeight);
            so.FindProperty("_flag").objectReferenceValue = flag;
            if (flag == null)
            {
                return;
            }

            Dictionary<string, object> placement = BlenderJson.Object(BlenderJson.Field(BlenderReport.Load($"{BlenderArtBuilder.AssetFolder(asset)}/{asset}_report.json").Root(), "placement"));
            // The report's own "base_local_unity" assumes the raw FBX frame (-x, z, -y), but under the base's root the pieces sit
            // at Blender (x, z, y): the Command and Habitat pivots of Base_report land there (round 23e), and the unconverted
            // spot put the pole inside the command post. So the Blender spot is mapped the way the base itself maps, and the
            // Blender turn about +Z becomes the opposite turn about Unity's +Y (that mapping swaps handedness).
            List<object> position = BlenderJson.Array(BlenderJson.Field(placement, "base_local_blender"));
            if (position.Count == 3)
            {
                so.FindProperty("_flagPosition").vector3Value = new Vector3(BlenderJson.Number(position[0]), BlenderJson.Number(position[2]), BlenderJson.Number(position[1]));
            }

            object yaw = BlenderJson.Field(placement, "yaw_deg");
            if (yaw != null)
            {
                so.FindProperty("_flagEuler").vector3Value = new Vector3(0f, -BlenderJson.Number(yaw), 0f);
            }
        }

        // The ground the model covers in the base's own frame (the art's yaw and scale applied), from its mesh bounds, so
        // the build grid keeps exactly that free whatever size the next export has.
        private static void WriteFootprint(SerializedObject so, ModelArtDefinition art)
        {
            if (art == null || art.Model == null)
            {
                return;
            }

            Matrix4x4 placed = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, art.Yaw, 0f), Vector3.one * art.Scale) * art.Model.transform.worldToLocalMatrix;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (MeshFilter filter in art.Model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds bounds = filter.sharedMesh.bounds;
                Matrix4x4 matrix = placed * filter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(new Vector3(
                        (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (corner & 4) == 0 ? bounds.min.z : bounds.max.z));
                    min = Vector2.Min(min, new Vector2(point.x, point.z));
                    max = Vector2.Max(max, new Vector2(point.x, point.z));
                }
            }

            if (min.x < max.x)
            {
                so.FindProperty("_footprintMin").vector2Value = min;
                so.FindProperty("_footprintMax").vector2Value = max;
            }

            WritePlaces(so.FindProperty("_turretMounts"), art.Model.transform, placed, BaseTurretSockets);
            WritePlaces(so.FindProperty("_dockPoints"), art.Model.transform, placed, NumberedSockets(art.Model.transform, BaseDockPrefix));
        }

        // The named sockets that exist, in the base's own frame.
        private static void WritePlaces(SerializedProperty array, Transform model, Matrix4x4 placed, IReadOnlyList<string> sockets)
        {
            var points = new List<Vector3>();
            foreach (string socket in sockets)
            {
                Transform node = FindDeep(model, socket);
                if (node != null)
                {
                    points.Add(placed.MultiplyPoint3x4(node.position));
                }
            }

            array.arraySize = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                array.GetArrayElementAtIndex(i).vector3Value = points[i];
            }
        }

        private static List<string> NumberedSockets(Transform model, string prefix)
        {
            var names = new List<string>();
            for (int n = 1; FindDeep(model, $"{prefix}_{n}") != null; n++)
            {
                names.Add($"{prefix}_{n}");
            }

            return names;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            return null;
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
