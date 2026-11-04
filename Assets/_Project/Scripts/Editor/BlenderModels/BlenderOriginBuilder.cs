using System.Collections.Generic;
using System.IO;
using Game.Combat;
using Game.Core;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The living spawn origins and route gates (ArtSource README rounds 23 and 25b): syncs the exports, builds a model art
    /// definition per origin and gate (the takes by role), and one <see cref="SpawnOriginDefinition"/> with the timing from
    /// the burrow's report and the burst from the effect library, which every campaign level then points at. Re-running
    /// updates the same assets in place.
    /// </summary>
    public static class BlenderOriginBuilder
    {
        public const string DataFolder = BlenderArtBuilder.ArtDataFolder + "/Origins";
        private const string DefinitionPath = DataFolder + "/SpawnOrigins_CrystalPlains.asset";
        private const string LevelFolder = "Assets/_Project/Data/Levels";
        private const string PreviewPrefix = "__preview__";
        private const float OriginEmission = 2f;
        private const float GateEmission = 1f;
        private const float LodScreenHeight = 0.04f;
        private const float FramesPerSecond = 30f;

        [MenuItem("Tools/Generate/Blender Origins (sync, spawn origins, route gates)")]
        public static void BuildMenu()
        {
            SpawnOriginDefinition definition = SyncAndBuild();
            int levels = Apply(definition);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender origins: {(definition != null ? "built" : "missing")}, {levels} level(s) wear it.");
        }

        /// <summary>Syncs the exports and builds the origin and gate art and the definition holding them.</summary>
        public static SpawnOriginDefinition SyncAndBuild()
        {
            BlenderSync.Sync(BlenderOriginCatalog.SyncedAssets());
            var origins = new List<SpawnOriginArt>();
            BlenderReport timing = null;
            foreach (BlenderOriginCatalog.Row row in BlenderOriginCatalog.Origins)
            {
                ModelArtDefinition art = BuildArt(row.Asset, OriginEmission);
                if (art == null)
                {
                    continue;
                }

                timing ??= Report(row.Asset);
                foreach (TerrainKind terrain in row.Terrains)
                {
                    origins.Add(new SpawnOriginArt(terrain, art, row.TintWithElement, row.GlowMarks));
                }
            }

            var definition = BlenderArtBuilder.LoadOrCreate<SpawnOriginDefinition>(DefinitionPath);
            var so = new SerializedObject(definition);
            SerializedProperty array = so.FindProperty("_origins");
            array.arraySize = origins.Count;
            for (int i = 0; i < origins.Count; i++)
            {
                array.GetArrayElementAtIndex(i).boxedValue = origins[i];
            }

            if (timing != null)
            {
                WriteTiming(so, timing);
            }

            so.FindProperty("_burstVfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ImpactVfxDefinition>(BlenderOriginCatalog.BurstVfxPath);
            so.FindProperty("_groundGate").objectReferenceValue = BuildArt(BlenderOriginCatalog.GroundGate, GateEmission);
            so.FindProperty("_airGate").objectReferenceValue = BuildArt(BlenderOriginCatalog.AirGate, GateEmission);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        /// <summary>Points every level at the definition; returns how many levels there are.</summary>
        public static int Apply(SpawnOriginDefinition definition)
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (level == null)
                {
                    continue;
                }

                var so = new SerializedObject(level);
                so.FindProperty("_spawnOrigins").objectReferenceValue = definition;
                so.ApplyModifiedPropertiesWithoutUndo();
                count++;
            }

            return count;
        }

        // The opening's moments come from the report (frames at 30 fps): the burst, the first creature, the settled maw.
        private static void WriteTiming(SerializedObject so, BlenderReport report)
        {
            float burst = report.Number("burst_frame");
            float emerge = report.Number("first_emerge_from_frame");
            float settled = report.Number("settled_frame");
            if (burst > 0f)
            {
                so.FindProperty("_burst").floatValue = burst / FramesPerSecond;
            }

            if (emerge > 0f)
            {
                so.FindProperty("_emerge").floatValue = emerge / FramesPerSecond;
            }

            if (settled > 0f)
            {
                so.FindProperty("_open").floatValue = settled / FramesPerSecond;
            }
        }

        private static BlenderReport Report(string asset)
        {
            string name = Path.GetFileName(asset);
            return BlenderReport.Load($"{BlenderArtBuilder.AssetFolder(asset)}/{name}_report.json");
        }

        private static ModelArtDefinition BuildArt(string asset, float emission)
        {
            string name = Path.GetFileName(asset);
            string folder = BlenderArtBuilder.AssetFolder(asset);
            string modelPath = $"{folder}/{name}.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null || !File.Exists($"{folder}/{name}_Albedo.png"))
            {
                Debug.LogWarning($"Blender origins: no model or maps for '{asset}' at {folder}.");
                return null;
            }

            BlenderReport report = Report(asset);
            var look = new BlenderArtEntry(name, name) { Emission = emission };
            Material high = BlenderArtBuilder.BuildMaterial(look, report, folder, name, false);
            Material low = BlenderArtBuilder.BuildMaterial(look, report, $"{folder}/{BlenderSync.LowFolder}", name + "_Low", true);
            var art = BlenderArtBuilder.LoadOrCreate<ModelArtDefinition>($"{DataFolder}/{name}.asset");
            var so = new SerializedObject(art);
            so.FindProperty("_model").objectReferenceValue = model;
            so.FindProperty("_lod1").objectReferenceValue = null;
            so.FindProperty("_material").objectReferenceValue = high;
            so.FindProperty("_lowMaterial").objectReferenceValue = low;
            so.FindProperty("_scale").floatValue = 1f;
            so.FindProperty("_yaw").floatValue = 0f;
            so.FindProperty("_bodyCentrePivot").boolValue = false;
            so.FindProperty("_height").floatValue = BlenderArtBuilder.ModelTop(model);
            so.FindProperty("_lodScreenHeight").floatValue = LodScreenHeight;
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
                if (asset is AnimationClip clip && !clip.name.StartsWith(PreviewPrefix) && BlenderOriginCatalog.Roles.TryGetValue(clip.name, out ModelClipRole role))
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
                element.FindPropertyRelative("_upperBody").boolValue = false;
            }
        }
    }
}
