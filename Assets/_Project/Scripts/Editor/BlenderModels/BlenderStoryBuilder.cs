using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Hero;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Turns the synced Blender exports of the story into game data (import stage 3b): Mercer's body with his clips by
    /// role (the upper-body ones marked, the moments and gait distances from his report), his weapons in grip space, the
    /// <see cref="HeroArtDefinition"/> worn by every hero definition; a <see cref="PortraitArtDefinition"/> per dialogue
    /// bust worn by the speakers of that portrait; and the cutscene's machines and drones worn by the cutscene cast. One
    /// <c>Game/UnitLit</c> material pair per export. Re-running updates the same assets in place.
    /// </summary>
    public static class BlenderStoryBuilder
    {
        public const string StoryArtFolder = BlenderArtBuilder.ArtDataFolder + "/Story";
        private const string DataFolder = "Assets/_Project/Data";
        private const string PreviewPrefix = "__preview__";
        private const float FramesPerSecond = 30f;

        // The exports face -Z once imported (stage 3a); the weapons are modelled in their socket's grip space and stay unturned.
        private const float ExportYaw = 180f;

        // README round 6 ("Emission x 2.5") and round 13 ("emission x 2.2").
        private const float HeroEmission = 2.5f;
        private const float StoryEmission = 2.2f;

        // Spencer pass 4 report: the face "emission 1 (iris catchlight only)", the visor "emission map x2", "smoothness
        // 0.95"; the visor's optional fresnel ("more opaque at grazing angles") as a little rim cover.
        private const float PartEmission = 1f;
        private const float GlassEmission = 2f;
        private const float GlassSmoothness = 0.95f;
        private const float GlassRimCover = 0.4f;

        // The hero is small at the play zoom; the cutscene's machines fill the picture.
        private const float HeroLodScreenHeight = 0.04f;
        private const float CutsceneLodScreenHeight = 0.02f;

        /// <summary>The story art built in one run.</summary>
        public sealed class Built
        {
            public HeroArtDefinition Hero;
            public readonly Dictionary<PortraitModel, PortraitArtDefinition> Busts = new Dictionary<PortraitModel, PortraitArtDefinition>();
            public readonly Dictionary<string, ModelArtDefinition> Cutscene = new Dictionary<string, ModelArtDefinition>();
        }

        [MenuItem("Tools/Generate/Blender Story (sync, hero, portraits, cutscene)")]
        public static void BuildMenu()
        {
            Built built = SyncAndBuild();
            int assigned = Apply(built);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender story: hero {(built.Hero != null ? "built" : "missing")}, {built.Busts.Count} busts, {built.Cutscene.Count} cutscene models, {assigned} owners wear one.");
        }

        /// <summary>Syncs the exports, then builds every model that is there.</summary>
        public static Built SyncAndBuild()
        {
            BlenderSync.Sync(BlenderStoryCatalog.SyncedAssets(), BlenderStoryCatalog.ExtraModels());
            var built = new Built { Hero = BuildHero() };
            foreach (BlenderStoryCatalog.Bust bust in BlenderStoryCatalog.Busts)
            {
                ModelArtDefinition model = BuildSingle(bust.Asset, StoryEmission, CutsceneLodScreenHeight);
                if (model != null)
                {
                    built.Busts[bust.Portrait] = BuildBust(bust, model);
                }
            }

            foreach (string asset in BlenderStoryCatalog.CutsceneModels)
            {
                ModelArtDefinition model = BuildSingle(asset, StoryEmission, CutsceneLodScreenHeight);
                if (model != null)
                {
                    built.Cutscene[asset] = model;
                }
            }

            return built;
        }

        /// <summary>
        /// Syncs and rebuilds only the named cutscene models and busts in place, leaving every other story export alone
        /// (one being reworked in Blender is not picked up half-done). Their art assets keep their owners, so nothing needs
        /// pointing again. Returns how many were rebuilt.
        /// </summary>
        public static int RebuildOnly(params string[] assets)
        {
            BlenderSync.Sync(assets);
            int rebuilt = 0;
            foreach (string asset in assets)
            {
                ModelArtDefinition model = BuildSingle(asset, StoryEmission, CutsceneLodScreenHeight);
                if (model == null)
                {
                    continue;
                }

                rebuilt++;
                foreach (BlenderStoryCatalog.Bust bust in BlenderStoryCatalog.Busts)
                {
                    if (bust.Asset == asset)
                    {
                        BuildBust(bust, model);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            return rebuilt;
        }

        /// <summary>Points the heroes, the speakers and the cutscene casts at their art; returns how many wear one.</summary>
        public static int Apply(Built built)
        {
            int assigned = 0;
            foreach (HeroDefinition hero in Load<HeroDefinition>())
            {
                assigned += Assign(hero, "_art", built.Hero);
            }

            foreach (SpeakerDefinition speaker in Load<SpeakerDefinition>())
            {
                // The hero's bust follows his own art, so a different commander gets a different picture.
                built.Busts.TryGetValue(speaker.Portrait, out PortraitArtDefinition bust);
                assigned += Assign(speaker, "_portraitArt", speaker.Portrait == PortraitModel.Hero ? null : bust);
            }

            foreach (CutsceneCast cast in Load<CutsceneCast>())
            {
                assigned += Assign(cast, "_mothershipArt", Cutscene(built, BlenderStoryCatalog.Mothership));
                assigned += Assign(cast, "_capsuleArt", Cutscene(built, BlenderStoryCatalog.Capsule));
                assigned += Assign(cast, "_drillArt", Cutscene(built, BlenderStoryCatalog.Drill));
                assigned += Assign(cast, "_beaconArt", Cutscene(built, BlenderStoryCatalog.Beacon));
                assigned += Assign(cast, "_builderArt", Cutscene(built, BlenderStoryCatalog.Builder));
                assigned += Assign(cast, "_haulerArt", Cutscene(built, BlenderStoryCatalog.Hauler));
                assigned += Assign(cast, "_consoleArt", Cutscene(built, BlenderStoryCatalog.Console));
            }

            return assigned;
        }

        private static ModelArtDefinition Cutscene(Built built, string asset) => built.Cutscene.TryGetValue(asset, out ModelArtDefinition art) ? art : null;

        // Mercer: the body with every clip, the three weapon files on one map set, and his bust.
        private static HeroArtDefinition BuildHero()
        {
            string asset = BlenderStoryCatalog.HeroAsset;
            string folder = BlenderArtBuilder.AssetFolder(asset);
            (Material high, Material low) = Materials(folder, asset, HeroEmission);
            if (high == null)
            {
                return null;
            }

            BlenderReport report = BlenderReport.Load($"{folder}/{asset}_report.json");
            ModelArtDefinition body = BuildArt(asset, $"{folder}/{asset}.fbx", null, high, low, HeroLodScreenHeight, ExportYaw, report, BlenderStoryCatalog.UpperBodyBone);
            if (body == null)
            {
                return null;
            }

            WriteOwnMaterialParts(body, folder, report);
            string weaponFolder = $"{folder}/{BlenderStoryCatalog.WeaponFolder}";
            (Material weaponHigh, Material weaponLow) = Materials(weaponFolder, BlenderStoryCatalog.WeaponMaps, HeroEmission);
            var art = BlenderArtBuilder.LoadOrCreate<HeroArtDefinition>($"{StoryArtFolder}/{asset}Art.asset");
            var so = new SerializedObject(art);
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_pistol").objectReferenceValue = Weapon(weaponFolder, BlenderStoryCatalog.Pistol, weaponHigh, weaponLow);
            so.FindProperty("_swordHilt").objectReferenceValue = Weapon(weaponFolder, BlenderStoryCatalog.SwordHilt, weaponHigh, weaponLow);
            so.FindProperty("_swordBlade").objectReferenceValue = Weapon(weaponFolder, BlenderStoryCatalog.SwordBlade, weaponHigh, weaponLow);
            WriteStrings(so.FindProperty("_hiddenParts"), BlenderStoryCatalog.HiddenWeaponParts);
            BlenderReport weapons = BlenderReport.Load($"{weaponFolder}/{BlenderStoryCatalog.WeaponMaps}_report.json");
            List<float> muzzle = weapons.Numbers("muzzle");
            if (muzzle.Count == 3)
            {
                so.FindProperty("_muzzle").vector3Value = new Vector3(muzzle[0], muzzle[1], muzzle[2]);
            }

            so.FindProperty("_bust").objectReferenceValue = BuildBust(BlenderStoryCatalog.HeroBust, body);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        private static ModelArtDefinition Weapon(string folder, string name, Material high, Material low)
        {
            return high == null ? null : BuildArt(name, $"{folder}/{name}.fbx", null, high, low, HeroLodScreenHeight, 0f, null, string.Empty);
        }

        private static PortraitArtDefinition BuildBust(BlenderStoryCatalog.Bust bust, ModelArtDefinition model)
        {
            var art = BlenderArtBuilder.LoadOrCreate<PortraitArtDefinition>($"{StoryArtFolder}/Portrait_{bust.Asset}.asset");
            var so = new SerializedObject(art);
            so.FindProperty("_model").objectReferenceValue = model;
            so.FindProperty("_poseRole").enumValueIndex = (int)ModelClipRole.Idle;
            so.FindProperty("_poseSeconds").floatValue = bust.PoseSeconds;
            so.FindProperty("_centre").vector3Value = new Vector3(0f, bust.CentreY, 0f);
            so.FindProperty("_turn").floatValue = bust.Turn;
            so.FindProperty("_zoom").floatValue = bust.Zoom;
            so.FindProperty("_glow").floatValue = bust.Glow;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        private static ModelArtDefinition BuildSingle(string asset, float emission, float lodScreenHeight)
        {
            string folder = BlenderArtBuilder.AssetFolder(asset);
            (Material high, Material low) = Materials(folder, asset, emission);
            // A far mesh of its own (the round-19 mothership) is worn from its own file; the others have none.
            string lod1 = $"{folder}/{asset}_LOD1.fbx";
            return high == null ? null : BuildArt(asset, $"{folder}/{asset}.fbx", File.Exists(lod1) ? lod1 : null, high, low, lodScreenHeight, ExportYaw, null, string.Empty);
        }

        // The export's High and Low material in the colony's cool rim, or nulls when the maps are not synced. An export
        // without a Low set (the weapons) wears its own maps on Low too.
        private static (Material High, Material Low) Materials(string folder, string name, float emission)
        {
            if (!File.Exists($"{folder}/{name}_Albedo.png"))
            {
                Debug.LogWarning($"Blender story: no maps for '{name}' at {folder}.");
                return (null, null);
            }

            BlenderReport report = BlenderReport.Load($"{folder}/{name}_report.json");
            var look = new BlenderArtEntry(name, name)
            {
                Emission = emission,
                Rim = BlenderArtEntry.TowerRim,
                Colony = true,
            };

            string lowFolder = $"{folder}/{BlenderSync.LowFolder}";
            Material high = BlenderArtBuilder.BuildMaterial(look, report, folder, name, false);
            Material low = BlenderArtBuilder.BuildMaterial(look, report, File.Exists($"{lowFolder}/{name}_Albedo.png") ? lowFolder : folder, name + "_Low", true);
            return (high, low);
        }

        private static ModelArtDefinition BuildArt(string name, string modelPath, string lod1Path, Material high, Material low, float lodScreenHeight, float yaw, BlenderReport report, string upperBodyBone)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogWarning($"Blender story: no model at {modelPath}.");
                return null;
            }

            var art = BlenderArtBuilder.LoadOrCreate<ModelArtDefinition>($"{StoryArtFolder}/{name}.asset");
            var so = new SerializedObject(art);
            so.FindProperty("_model").objectReferenceValue = model;
            so.FindProperty("_lod1").objectReferenceValue = lod1Path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(lod1Path) : null;
            so.FindProperty("_material").objectReferenceValue = high;
            so.FindProperty("_lowMaterial").objectReferenceValue = low;
            so.FindProperty("_scale").floatValue = BlenderStoryCatalog.ModelScales.TryGetValue(name, out float scale) ? scale : 1f;
            so.FindProperty("_yaw").floatValue = yaw;
            so.FindProperty("_bodyCentrePivot").boolValue = false;
            so.FindProperty("_height").floatValue = BlenderArtBuilder.ModelTop(model);
            so.FindProperty("_lodScreenHeight").floatValue = lodScreenHeight;
            so.FindProperty("_upperBodyBone").stringValue = upperBodyBone;
            so.FindProperty("_deathFade").vector2Value = Vector2.zero;
            so.FindProperty("_embers").boolValue = false;
            so.FindProperty("_bleeds").boolValue = false;
            WriteClips(so.FindProperty("_clips"), modelPath, lod1Path, report);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        private static void WriteClips(SerializedProperty array, string modelPath, string lod1Path, BlenderReport report)
        {
            Dictionary<string, AnimationClip> far = ClipsByName(lod1Path);
            var clips = new List<AnimationClip>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith(PreviewPrefix) && BlenderStoryCatalog.Roles.ContainsKey(clip.name))
                {
                    clips.Add(clip);
                }
            }

            // Asset order is not stable between imports; sorted by name, re-running writes the same array.
            clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            array.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
            {
                AnimationClip clip = clips[i];
                bool upper = BlenderStoryCatalog.UpperBody.TryGetValue(clip.name, out float weight);
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_role").enumValueIndex = (int)BlenderStoryCatalog.Roles[clip.name];
                element.FindPropertyRelative("_clip").objectReferenceValue = clip;
                element.FindPropertyRelative("_farClip").objectReferenceValue = far.TryGetValue(clip.name, out AnimationClip farClip) ? farClip : null;
                element.FindPropertyRelative("_distancePerLoop").floatValue = Report(report, BlenderStoryCatalog.DistanceKeys, clip.name, 1f);
                element.FindPropertyRelative("_eventSeconds").floatValue = Moment(report, BlenderStoryCatalog.EventKeys, clip.name);
                element.FindPropertyRelative("_cueSeconds").floatValue = Moment(report, BlenderStoryCatalog.CueKeys, clip.name);
                element.FindPropertyRelative("_trigger").stringValue = string.Empty;
                element.FindPropertyRelative("_socket").stringValue = string.Empty;
                element.FindPropertyRelative("_weight").floatValue = upper ? weight : 1f;
                element.FindPropertyRelative("_upperBody").boolValue = upper;
            }
        }

        // A number from the report under the clip's key, times a unit (frames to seconds), or zero.
        private static float Report(BlenderReport report, IReadOnlyDictionary<string, string> keys, string clip, float unit)
        {
            return report != null && keys.TryGetValue(clip, out string key) ? report.Number(key) * unit : 0f;
        }

        // A clip's moment in seconds from the report's events block (the clip's own entry, since the draws share names).
        private static float Moment(BlenderReport report, IReadOnlyDictionary<string, string> keys, string clip)
        {
            return report != null && keys.TryGetValue(clip, out string key) ? report.Event(clip, key) / FramesPerSecond : 0f;
        }

        // Spencer pass 4: the meshes the report lists with maps of their own (the face, the visor's glass) wear their own
        // material instead of the body's: an opaque lit one with the face maps, or a see-through glass. Their emission
        // follows the report ("emission 1" for the face's catchlights, "x2" for the visor's faint HUD ticks).
        private static void WriteOwnMaterialParts(ModelArtDefinition body, string folder, BlenderReport report)
        {
            Dictionary<string, object> root = report.Root();
            var parts = new List<ModelPart>();
            foreach (string section in BlenderStoryCatalog.OwnMaterialMeshes)
            {
                Dictionary<string, object> entry = BlenderJson.Object(BlenderJson.Field(root, section));
                string mesh = BlenderJson.Text(BlenderJson.Field(entry, "mesh"));
                if (string.IsNullOrEmpty(mesh) || !File.Exists($"{folder}/{mesh}_Albedo.png"))
                {
                    continue;
                }

                string material = BlenderJson.Text(BlenderJson.Field(entry, "material")) ?? string.Empty;
                bool seeThrough = material.IndexOf("transparent", System.StringComparison.OrdinalIgnoreCase) >= 0;
                (Material high, Material low) = seeThrough ? GlassMaterials(folder, mesh) : PartMaterials(folder, mesh);
                parts.Add(new ModelPart(mesh, high, low, castShadows: !seeThrough));
            }

            var so = new SerializedObject(body);
            SerializedProperty array = so.FindProperty("_parts");
            array.arraySize = parts.Count;
            for (int i = 0; i < parts.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_object").stringValue = parts[i].Object;
                element.FindPropertyRelative("_material").objectReferenceValue = parts[i].Material;
                element.FindPropertyRelative("_lowMaterial").objectReferenceValue = parts[i].MaterialFor(true);
                element.FindPropertyRelative("_castShadows").boolValue = parts[i].CastShadows;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(body);
        }

        // The face: the hero's lit material with its own maps; its mask carries no detail families.
        private static (Material High, Material Low) PartMaterials(string folder, string mesh)
        {
            var look = new BlenderArtEntry(mesh, mesh)
            {
                Emission = PartEmission,
                Rim = BlenderArtEntry.TowerRim,
                Colony = true,
            };

            BlenderReport none = BlenderReport.Load(string.Empty);
            string lowFolder = $"{folder}/{BlenderSync.LowFolder}";
            Material high = BlenderArtBuilder.BuildMaterial(look, none, folder, mesh, false);
            Material low = BlenderArtBuilder.BuildMaterial(look, none, File.Exists($"{lowFolder}/{mesh}_Albedo.png") ? lowFolder : folder, mesh + "_Low", true);
            return (high, low);
        }

        // The visor's glass: albedo alpha is its opacity (clear in the middle, denser at the rim), glossy, a faint glow,
        // blended after the opaque body without writing depth; the rim adds a little cover at grazing angles.
        private static (Material High, Material Low) GlassMaterials(string folder, string mesh)
        {
            var look = new BlenderArtEntry(mesh, mesh)
            {
                Emission = GlassEmission,
                Smoothness = GlassSmoothness,
                Rim = BlenderArtEntry.TowerRim,
                Colony = true,
            };

            BlenderReport none = BlenderReport.Load(string.Empty);
            Material high = BlenderArtBuilder.BuildMaterial(look, none, folder, mesh, false);
            Material low = BlenderArtBuilder.BuildMaterial(look, none, folder, mesh + "_Low", true);
            foreach (Material material in new[] { high, low })
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_RimAlpha", GlassRimCover);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetOverrideTag("RenderType", "Transparent");
                EditorUtility.SetDirty(material);
            }

            return (high, low);
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

        private static void WriteStrings(SerializedProperty array, string[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }
    }
}
