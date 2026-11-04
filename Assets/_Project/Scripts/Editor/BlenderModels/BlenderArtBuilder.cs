using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Enemies;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Turns the synced Blender exports into game data: one <c>Game/UnitLit</c> material per model and quality profile
    /// (High: 1024 maps and both detail normals; Low: the 512 maps and one), and one <see cref="ModelArtDefinition"/> per
    /// catalog row with its clips by role, moments, scale and pivot. Re-running updates the same assets in place.
    /// </summary>
    public static class BlenderArtBuilder
    {
        public const string ArtDataFolder = "Assets/_Project/Data/Art";
        public const string MaterialFolder = "Assets/_Project/Art/Materials/Blender";
        private const string EnemyDataFolder = "Assets/_Project/Data/Enemies";
        private const float FramesPerSecond = 30f;
        private const string PreviewPrefix = "__preview__";

        // Dead things do not glow: without frames of its own the glow fades over this share of the Death clip.
        private const float DefaultFadeFrom = 0.25f;
        private const float DefaultFadeTo = 0.85f;

        // The baked albedo already carries the occlusion; the mask's share only shapes the ambient light (README).
        private const float OcclusionStrength = 0.5f;

        // Below this share of the screen height a creature shows its far LOD.
        private const float CreatureLodScreenHeight = 0.05f;
        private const float BossLodScreenHeight = 0.09f;

        // One clip as the builder collects it, before it is written into the art definition.
        private readonly struct ClipRow
        {
            public ClipRow(ModelClipRole role, AnimationClip clip, AnimationClip farClip, float distance, float eventSeconds, string trigger, string socket, float weight)
            {
                Role = role;
                Clip = clip;
                FarClip = farClip;
                DistancePerLoop = distance;
                EventSeconds = eventSeconds;
                Trigger = trigger;
                Socket = socket;
                Weight = weight;
            }

            public ModelClipRole Role { get; }
            public AnimationClip Clip { get; }
            public AnimationClip FarClip { get; }
            public float DistancePerLoop { get; }
            public float EventSeconds { get; }
            public string Trigger { get; }
            public string Socket { get; }
            public float Weight { get; }
            public float Length => Clip.length;
        }

        private static readonly Dictionary<string, ModelClipRole> RolesByName = new Dictionary<string, ModelClipRole>
        {
            { "Walk", ModelClipRole.Move }, { "Run", ModelClipRole.Move }, { "Crawl", ModelClipRole.Move },
            { "Fly", ModelClipRole.Move }, { "Float", ModelClipRole.Move }, { "Idle", ModelClipRole.Idle },
            { "Hit", ModelClipRole.Hit }, { "Death", ModelClipRole.Death }, { "Curl", ModelClipRole.Curl },
            { "Roll", ModelClipRole.Roll }, { "Uncurl", ModelClipRole.Uncurl }, { "Land", ModelClipRole.Land },
            { "Grounded", ModelClipRole.Grounded }, { "Swim", ModelClipRole.Swim }, { "WalkEnraged", ModelClipRole.EnragedMove },
        };

        // A hit only flinches: most of the stride or wing beat keeps going underneath it.
        private const float HitWeight = 0.6f;

        [MenuItem("Tools/Generate/Blender Creatures (sync, materials, enemies)")]
        public static void BuildMenu()
        {
            Dictionary<string, ModelArtDefinition> art = SyncAndBuildCreatures();
            int assigned = ApplyToEnemies(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender creatures: {art.Count} art definitions, {assigned} enemies wear a model.");
        }

        /// <summary>Syncs the creature exports, then builds every creature row that has its model; returns them by id.</summary>
        public static Dictionary<string, ModelArtDefinition> SyncAndBuildCreatures()
        {
            BlenderSync.Sync(BlenderArtCatalog.SyncedAssets());
            var built = new Dictionary<string, ModelArtDefinition>();
            foreach (BlenderArtEntry entry in BlenderArtCatalog.Creatures)
            {
                ModelArtDefinition art = Build(entry);
                if (art != null)
                {
                    built[entry.Id] = art;
                }
            }

            return built;
        }

        /// <summary>Points every enemy definition at its art (or at none); returns how many wear a model.</summary>
        public static int ApplyToEnemies(IReadOnlyDictionary<string, ModelArtDefinition> art)
        {
            int assigned = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyDataFolder }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                ModelArtDefinition wear = null;
                if (BlenderArtCatalog.EnemyArt.TryGetValue(enemy.name, out string id))
                {
                    art.TryGetValue(id, out wear);
                }

                var so = new SerializedObject(enemy);
                so.FindProperty("_art").objectReferenceValue = wear;
                so.ApplyModifiedPropertiesWithoutUndo();
                assigned += wear != null ? 1 : 0;
            }

            return assigned;
        }

        internal static string AssetFolder(string asset) => $"{BlenderSync.TargetRoot}/{asset}";

        private static ModelArtDefinition Build(BlenderArtEntry entry)
        {
            string folder = AssetFolder(entry.Asset);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{entry.Asset}.fbx");
            if (model == null)
            {
                Debug.LogWarning($"Blender art '{entry.Id}': no model at {folder}/{entry.Asset}.fbx.");
                return null;
            }

            var lod1 = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{entry.Asset}_LOD1.fbx");
            BlenderReport report = BlenderReport.Load($"{folder}/{entry.Asset}_report.json");
            string maps = entry.Skin == null ? folder : $"{folder}/{BlenderSync.SkinFolder}/{entry.Skin}";
            List<BlenderReport.Detail> details = report.Details();
            List<BlenderReport.Detail> bodyDetails = entry.SecondMaterial == null ? details : DetailsOfPart(details, 0);
            Material high = BuildMaterial(entry, entry.MapPrefix, bodyDetails, maps, entry.Id, false);
            Material low = BuildMaterial(entry, entry.MapPrefix, bodyDetails, $"{maps}/{BlenderSync.LowFolder}", entry.Id + "_Low", true);
            Material second = null;
            Material lowSecond = null;
            if (entry.SecondMaterial != null)
            {
                // The second slot (a boss's crystals or ice) has maps of its own and the report's second detail block.
                string prefix = $"{entry.MapPrefix}_{entry.SecondMaterial}";
                List<BlenderReport.Detail> secondDetails = DetailsOfPart(details, 1);
                second = BuildMaterial(entry, prefix, secondDetails, maps, $"{entry.Id}_{entry.SecondMaterial}", false);
                lowSecond = BuildMaterial(entry, prefix, secondDetails, $"{maps}/{BlenderSync.LowFolder}", $"{entry.Id}_{entry.SecondMaterial}_Low", true);
            }

            List<ClipRow> clips = CollectClips(entry, report, $"{folder}/{entry.Asset}.fbx", lod1 != null ? $"{folder}/{entry.Asset}_LOD1.fbx" : null);

            var art = LoadOrCreate<ModelArtDefinition>($"{ArtDataFolder}/{entry.Id}.asset");
            var so = new SerializedObject(art);
            so.FindProperty("_model").objectReferenceValue = model;
            so.FindProperty("_lod1").objectReferenceValue = lod1;
            so.FindProperty("_material").objectReferenceValue = high;
            so.FindProperty("_lowMaterial").objectReferenceValue = low;
            so.FindProperty("_secondMaterial").objectReferenceValue = second;
            so.FindProperty("_lowSecondMaterial").objectReferenceValue = lowSecond;
            so.FindProperty("_scale").floatValue = entry.Scale;
            so.FindProperty("_bodyCentrePivot").boolValue = entry.BodyCentrePivot;
            so.FindProperty("_height").floatValue = ModelTop(model);
            so.FindProperty("_lodScreenHeight").floatValue = entry.Scale < BlenderArtEntry.SpeciesScale ? BossLodScreenHeight : CreatureLodScreenHeight;
            so.FindProperty("_deathFade").vector2Value = DeathFade(entry, clips);
            so.FindProperty("_embers").boolValue = entry.Embers;
            so.FindProperty("_bleeds").boolValue = entry.Bleeds;
            WriteStrings(so.FindProperty("_shedBones"), entry.ShedBonesKey != null ? report.Strings(entry.ShedBonesKey) : new List<string>());
            WriteClips(so.FindProperty("_clips"), clips);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        // Every take of the model, by role: the name says the role of the common ones, the row adds moments and powers.
        private static List<ClipRow> CollectClips(BlenderArtEntry entry, BlenderReport report, string modelPath, string farPath)
        {
            var clips = new List<ClipRow>();
            Dictionary<string, AnimationClip> far = ClipsByName(farPath);
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (!(asset is AnimationClip clip) || clip.name.StartsWith(PreviewPrefix))
                {
                    continue;
                }

                BlenderClipSpec? spec = SpecFor(entry, clip.name);
                if (spec == null && !RolesByName.ContainsKey(clip.name))
                {
                    continue;
                }

                ModelClipRole role = spec?.Role ?? RolesByName[clip.name];
                float distance = DistancePerLoop(role, entry, report);
                float weight = spec?.Weight ?? (role == ModelClipRole.Hit ? HitWeight : 1f);
                far.TryGetValue(clip.name, out AnimationClip farClip);
                clips.Add(new ClipRow(role, clip, farClip, distance, (spec?.EventFrame ?? 0) / FramesPerSecond, spec?.Trigger ?? string.Empty, spec?.Socket ?? string.Empty, weight));
            }

            return clips;
        }

        private static Dictionary<string, AnimationClip> ClipsByName(string path)
        {
            var clips = new Dictionary<string, AnimationClip>();
            if (path == null)
            {
                return clips;
            }

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith(PreviewPrefix))
                {
                    clips[clip.name] = clip;
                }
            }

            return clips;
        }

        private static BlenderClipSpec? SpecFor(BlenderArtEntry entry, string clip)
        {
            foreach (BlenderClipSpec spec in entry.Clips)
            {
                if (spec.Clip == clip)
                {
                    return spec;
                }
            }

            return null;
        }

        // Model units one loop of a travel cycle covers, from the report (or the row for the older exports).
        private static float DistancePerLoop(ModelClipRole role, BlenderArtEntry entry, BlenderReport report)
        {
            switch (role)
            {
                case ModelClipRole.Move: return MoveDistance(entry, report);
                case ModelClipRole.Roll: return report.Number("roll_distance_per_loop");
                case ModelClipRole.Swim: return report.Number("swim_distance_per_loop");
                case ModelClipRole.EnragedMove: return report.Number("walk_enraged_distance_per_loop");
                default: return 0f;
            }
        }

        private static float MoveDistance(BlenderArtEntry entry, BlenderReport report)
        {
            float reported = report.Number("walk_distance_per_loop", "run_distance_per_loop", "crawl_distance_per_loop");
            return reported > 0f ? reported : entry.MoveDistance;
        }

        private static void WriteClips(SerializedProperty array, List<ClipRow> clips)
        {
            array.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                ClipRow clip = clips[i];
                element.FindPropertyRelative("_role").enumValueIndex = (int)clip.Role;
                element.FindPropertyRelative("_clip").objectReferenceValue = clip.Clip;
                element.FindPropertyRelative("_farClip").objectReferenceValue = clip.FarClip;
                element.FindPropertyRelative("_distancePerLoop").floatValue = clip.DistancePerLoop;
                element.FindPropertyRelative("_eventSeconds").floatValue = clip.EventSeconds;
                element.FindPropertyRelative("_trigger").stringValue = clip.Trigger;
                element.FindPropertyRelative("_socket").stringValue = clip.Socket;
                element.FindPropertyRelative("_weight").floatValue = clip.Weight;
            }
        }

        private static Vector2 DeathFade(BlenderArtEntry entry, List<ClipRow> clips)
        {
            if (entry.DeathFadeTo > entry.DeathFadeFrom)
            {
                return new Vector2(entry.DeathFadeFrom / FramesPerSecond, entry.DeathFadeTo / FramesPerSecond);
            }

            foreach (ClipRow clip in clips)
            {
                if (clip.Role == ModelClipRole.Death)
                {
                    return new Vector2(clip.Length * DefaultFadeFrom, clip.Length * DefaultFadeTo);
                }
            }

            return Vector2.zero;
        }

        // The top of the model in its own units at rest, for the health bar.
        internal static float ModelTop(GameObject model)
        {
            GameObject probe = Object.Instantiate(model);
            try
            {
                float top = 0f;
                foreach (Renderer renderer in probe.GetComponentsInChildren<Renderer>())
                {
                    top = Mathf.Max(top, renderer.bounds.max.y);
                }

                return Mathf.Max(0.05f, top);
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }

        internal static Material BuildMaterial(BlenderArtEntry entry, BlenderReport report, string maps, string name, bool low)
        {
            return BuildMaterial(entry, entry.MapPrefix, report.Details(), maps, name, low);
        }

        private static Material BuildMaterial(BlenderArtEntry entry, string prefix, List<BlenderReport.Detail> details, string maps, string name, bool low)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                EnsureFolder(MaterialFolder);
                material = new Material(Shader.Find(SurfaceMaterials.UnitShaderName));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find(SurfaceMaterials.UnitShaderName);
            material.shaderKeywords = new string[0];
            material.SetColor("_BaseColor", Color.white * entry.Brightness);
            material.SetTexture("_BaseMap", Texture($"{maps}/{prefix}_Albedo.png"));
            SetMap(material, "_BumpMap", Texture($"{maps}/{prefix}_Normal.png"), "_NORMALMAP");
            SetMap(material, "_MaskMap", Texture($"{maps}/{prefix}_Mask.png"), "_MASKMAP");
            SetMap(material, "_EmissionMap", Texture($"{maps}/{prefix}_Emission.png"), "_EMISSION");
            material.SetColor("_EmissionColor", Color.white * entry.Emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetFloat("_Smoothness", entry.Smoothness);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_OcclusionStrength", OcclusionStrength);
            SurfaceMaterials.SetRim(material, entry.Colony ? SurfaceMaterials.ColonyRim : SurfaceMaterials.AlienRim, entry.Rim);
            SetDetails(material, details, entry, low);
            EditorUtility.SetDirty(material);
            return material;
        }

        // The mask's B channel says where the first family's detail normal goes, A the second; Low keeps only the first.
        private static void SetDetails(Material material, List<BlenderReport.Detail> details, BlenderArtEntry entry, bool low)
        {
            foreach (BlenderReport.Detail detail in details)
            {
                bool second = detail.Channel == 'A';
                if (second && low)
                {
                    continue;
                }

                var map = Texture($"{BlenderSync.TargetRoot}/{BlenderSync.DetailFolder}/{detail.Map}");
                if (map == null)
                {
                    continue;
                }

                string slot = second ? "_DetailNormalMap2" : "_DetailNormalMap";
                SetMap(material, slot, map, second ? "_DETAIL2" : "_DETAIL");
                material.SetTextureScale(slot, Vector2.one * detail.Tiling);
                material.SetFloat(second ? "_DetailNormalScale2" : "_DetailNormalScale", second ? entry.Detail2 : entry.Detail);
            }
        }

        // A two-material report lists the body's detail families first, then the second slot's; each block starts with
        // its mask-B family, so a block runs from one B to the next.
        private static List<BlenderReport.Detail> DetailsOfPart(List<BlenderReport.Detail> details, int part)
        {
            var found = new List<BlenderReport.Detail>(2);
            int block = -1;
            foreach (BlenderReport.Detail detail in details)
            {
                if (detail.Channel == 'B')
                {
                    block++;
                }

                if (block == part)
                {
                    found.Add(detail);
                }
            }

            return found;
        }

        private static void WriteStrings(SerializedProperty array, List<string> values)
        {
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                array.GetArrayElementAtIndex(i).stringValue = values[i];
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

        internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
