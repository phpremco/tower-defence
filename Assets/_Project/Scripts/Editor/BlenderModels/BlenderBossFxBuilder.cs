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
    /// Imports the round-29 boss art (ArtSource README round 29) without touching any other art: syncs <c>Boss_FX</c>, the
    /// ruin gate, the seal crystals, the gate ice block, the drill-rig wreck, the drained gate maps and the dampened icon
    /// (and since round 30 Gravemaw's crack slabs);
    /// builds the ground-mark materials (<c>Game/BossDecal</c>), the flipbook materials (<c>Game/VfxParticle</c>) and the
    /// props' art, writes them into <see cref="BossFxArt"/>, hangs that on the <see cref="VfxLibrary"/> and the wreck on
    /// the colony rig. Re-running updates the same assets in place.
    /// </summary>
    public static class BlenderBossFxBuilder
    {
        private const string ArtPath = "Assets/_Project/Data/Vfx/BossFxArt.asset";
        private const string VfxLibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";
        private const string RigPath = "Assets/_Project/Data/Art/Environment/ColonyRig_CrystalPlains.asset";
        private const string PropArtFolder = "Assets/_Project/Data/Art/Bosses";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Vfx/Bosses";
        private const string DecalShader = "Game/BossDecal";
        private const string ParticleShader = "Game/VfxParticle";
        private const string FissureRockShader = "Game/FissureRock";
        private const string FissurePunchShader = "Game/DepthPunch";
        // README round 34: emission (1, 0.42, 0.1) from the map at about x2.6 while hot.
        private static readonly Color FissureGlow = new Color(2.6f, 1.09f, 0.26f, 1f);
        private const float PropLodScreenHeight = 0.05f;
        private const float ExportYaw = 180f;
        private const float SealCrystalEmission = 1.6f;
        // README round 30: the slabs' baked glow at about x3 while the crack is hot; the view cools it with the strip.
        private const float CrackSlabEmission = 3f;
        private const float RuinGateEmission = 1f;
        // README round 29: the gate is 8.7 m wide; on a small map about 0.65 so it does not hide the road beside it.
        private const float RuinGateScale = 0.65f;
        private const float IceSmoothness = 0.92f;
        private const float IceRimAlpha = 0.7f;
        private const float SoftDistance = 0.4f;
        // The dark gap along a mark's middle, as a share of its width either side (the fissure between the heaved plates).
        private const float DarkGapHalfWidth = 0.13f;

        // README round 29: the ruin gate's takes by role.
        private static readonly Dictionary<string, ModelClipRole> RuinGateRoles = new Dictionary<string, ModelClipRole>
        {
            { "Closed_Idle", ModelClipRole.Closed }, { "Open", ModelClipRole.Open }, { "Opened_Idle", ModelClipRole.Idle },
        };

        // README round 31: the wreck's collapse, from the standing rig to the wreck pose, played as its Death role.
        private static readonly Dictionary<string, ModelClipRole> WreckRoles = new Dictionary<string, ModelClipRole>
        {
            { "Collapse", ModelClipRole.Death },
            // The held wreck pose as its rest cycle (a model with clips needs one).
            { "Wrecked", ModelClipRole.Idle },
        };

        private const float ClipFramesPerSecond = 30f;

        [MenuItem("Tools/Generate/Blender Boss FX (sync, marks, sheets, boss props)")]
        public static void BuildMenu()
        {
            BossFxArt art = SyncAndBuild();
            AssetDatabase.SaveAssets();
            Debug.Log(art != null ? "Blender boss FX: built and on the effect library." : "Blender boss FX: nothing built.");
        }

        public static BossFxArt SyncAndBuild()
        {
            Sync();
            EnsureFolder(MaterialFolder);
            EnsureFolder(PropArtFolder);
            BossFxArt art = BlenderArtBuilder.LoadOrCreate<BossFxArt>(ArtPath);
            var so = new SerializedObject(art);
            WriteMarks(so.FindProperty("_marks"));
            WriteSheets(so.FindProperty("_sheets"));
            WriteRuinGate(so);
            so.FindProperty("_sealCrystals").objectReferenceValue = BuildProp(BlenderBossFxCatalog.SealCrystal, SealCrystalEmission, null);
            so.FindProperty("_iceBlock").objectReferenceValue = BuildIceBlock();
            so.FindProperty("_crackSlabs").objectReferenceValue = BuildProp(BlenderBossFxCatalog.CrackSlabs, CrackSlabEmission, null);
            WriteDrained(so.FindProperty("_drainedMaps"));
            WriteFissure(so);
            so.FindProperty("_dampened").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"{BlenderSync.TargetRoot}/{BlenderBossFxCatalog.UiFolder}/{BlenderBossFxCatalog.Dampened}@2x.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);

            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(VfxLibraryPath);
            if (library != null)
            {
                var lib = new SerializedObject(library);
                lib.FindProperty("_bossFx").objectReferenceValue = art;
                lib.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(library);
            }

            ApplyWreck();
            return art;
        }

        /// <summary>
        /// Syncs and rebuilds the ruin gate alone (a re-export of that one prop), leaving every other boss effect and prop as
        /// it is. Returns the gate's art, or null when its export or the effect asset is missing.
        /// </summary>
        public static ModelArtDefinition SyncAndBuildRuinGate()
        {
            var art = AssetDatabase.LoadAssetAtPath<BossFxArt>(ArtPath);
            if (art == null)
            {
                Debug.LogWarning("Blender boss FX: no BossFxArt yet; run the full boss FX step first.");
                return null;
            }

            BlenderSync.Sync(new[] { BlenderBossFxCatalog.RuinGate });
            EnsureFolder(PropArtFolder);
            var so = new SerializedObject(art);
            WriteRuinGate(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art.RuinGate;
        }

        private static void WriteRuinGate(SerializedObject so)
        {
            so.FindProperty("_ruinGate").objectReferenceValue = BuildProp(BlenderBossFxCatalog.RuinGate, RuinGateEmission, RuinGateRoles);
            so.FindProperty("_ruinGateScale").floatValue = RuinGateScale;
        }

        private static void Sync()
        {
            var files = new List<string>();
            foreach (BlenderBossFxCatalog.MarkSpec mark in BlenderBossFxCatalog.Marks)
            {
                files.Add(mark.File + "_Albedo.png");
                files.Add(mark.File + "_Emission.png");
            }

            foreach (BlenderBossFxCatalog.SheetSpec sheet in BlenderBossFxCatalog.Sheets)
            {
                files.Add(sheet.File + ".png");
            }

            BlenderSync.SyncFiles(BlenderBossFxCatalog.Folder, files, BlenderBossFxCatalog.Report);
            BlenderSync.Sync(new[]
            {
                BlenderBossFxCatalog.RuinGate, BlenderBossFxCatalog.SealCrystal, BlenderBossFxCatalog.IceBlock, BlenderBossFxCatalog.DrillWreck,
                BlenderBossFxCatalog.CrackSlabs, BlenderBossFxCatalog.Fissure,
            });
            foreach ((string folder, string _, string drained) in BlenderBossFxCatalog.DrainedMaps)
            {
                BlenderSync.SyncFiles(folder, new[] { drained + ".png" }, folder + "_report.json");
            }

            BlenderSync.SyncFiles(BlenderBossFxCatalog.UiFolder, new[] { BlenderBossFxCatalog.Dampened + ".png", BlenderBossFxCatalog.Dampened + "@2x.png" }, "UI_report.json");
        }

        private static void WriteMarks(SerializedProperty array)
        {
            var built = new List<(BlenderBossFxCatalog.MarkSpec Spec, Material High, Material Low)>();
            foreach (BlenderBossFxCatalog.MarkSpec spec in BlenderBossFxCatalog.Marks)
            {
                Material high = MarkMaterial(spec, false);
                if (high != null)
                {
                    built.Add((spec, high, MarkMaterial(spec, true)));
                }
            }

            array.arraySize = built.Count;
            for (int i = 0; i < built.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_mark").enumValueIndex = (int)built[i].Spec.Mark;
                element.FindPropertyRelative("_material").objectReferenceValue = built[i].High;
                element.FindPropertyRelative("_lowMaterial").objectReferenceValue = built[i].Low;
                element.FindPropertyRelative("_width").floatValue = built[i].Spec.Width;
                element.FindPropertyRelative("_tileLength").floatValue = built[i].Spec.Tile;
            }
        }

        private static Material MarkMaterial(BlenderBossFxCatalog.MarkSpec spec, bool low)
        {
            string folder = FxFolder(low);
            Texture2D albedo = Texture($"{folder}/{spec.File}_Albedo.png") ?? Texture($"{FxFolder(false)}/{spec.File}_Albedo.png");
            Texture2D pack = Texture($"{folder}/{spec.File}_Emission.png") ?? Texture($"{FxFolder(false)}/{spec.File}_Emission.png");
            if (albedo == null || pack == null)
            {
                Debug.LogWarning($"Blender boss FX: no maps for {spec.File}.");
                return null;
            }

            Material material = LoadOrCreateMaterial($"{MaterialFolder}/{spec.File}{(low ? "_Low" : string.Empty)}.mat", DecalShader);
            material.SetTexture("_BaseMap", albedo);
            material.SetTexture("_GlowMap", pack);
            material.SetColor("_EmissionColor", spec.Glow);
            material.SetFloat("_Mode", (float)spec.Mode);
            material.SetFloat("_Depth", spec.Depth);
            material.SetFloat("_LipHeight", spec.Lip);
            material.SetFloat("_EdgeGlow", spec.EdgeGlow);
            material.SetFloat("_DepthFrom", DarkGapHalfWidth);
            material.SetVector("_Pulse", new Vector4(spec.Pulse, 2.4f, 0f, 0f));
            material.SetFloat("_Progress", 1f);
            material.SetFloat("_OpenDistance", -1f);
            // The frost atlas: one cell of four, picked per piece by the view's offset.
            material.SetTextureScale("_BaseMap", spec.Atlas ? new Vector2(0.5f, 0.5f) : Vector2.one);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void WriteSheets(SerializedProperty array)
        {
            var built = new List<(BlenderBossFxCatalog.SheetSpec Spec, Material High, Material Low)>();
            foreach (BlenderBossFxCatalog.SheetSpec spec in BlenderBossFxCatalog.Sheets)
            {
                Material high = SheetMaterial(spec, false);
                if (high != null)
                {
                    built.Add((spec, high, SheetMaterial(spec, true)));
                }
            }

            array.arraySize = built.Count;
            for (int i = 0; i < built.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_sheet").enumValueIndex = (int)built[i].Spec.Sheet;
                element.FindPropertyRelative("_material").objectReferenceValue = built[i].High;
                element.FindPropertyRelative("_lowMaterial").objectReferenceValue = built[i].Low;
                element.FindPropertyRelative("_framesPerSecond").floatValue = built[i].Spec.Fps;
            }
        }

        private static Material SheetMaterial(BlenderBossFxCatalog.SheetSpec spec, bool low)
        {
            Texture2D sheet = Texture($"{FxFolder(low)}/{spec.File}.png") ?? Texture($"{FxFolder(false)}/{spec.File}.png");
            if (sheet == null)
            {
                Debug.LogWarning($"Blender boss FX: no sheet {spec.File}.");
                return null;
            }

            Material material = LoadOrCreateMaterial($"{MaterialFolder}/{spec.File}{(low ? "_Low" : string.Empty)}.mat", ParticleShader);
            material.shaderKeywords = new string[0];
            material.SetTexture("_MainTex", sheet);
            material.SetColor("_CoreColor", spec.Core);
            material.SetColor("_GlowColor", spec.Glow);
            material.SetColor("_ThirdColor", spec.Third);
            material.SetColor("_Tint", Color.white);
            material.SetFloat("_Intensity", 1f);
            material.SetFloat("_Opacity", spec.Opacity);
            material.SetVector("_Grid", new Vector4(BossFxArt.SheetColumns, BossFxArt.SheetRows, 0f, 0f));
            material.SetFloat("_SoftDistance", SoftDistance);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            bool soft = spec.Soft && !low;
            material.SetFloat("_SoftParticles", soft ? 1f : 0f);
            if (soft)
            {
                material.EnableKeyword("_SOFT_PARTICLES");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        // A static or clip-driven prop on its own map set, like the colony's structures.
        private static ModelArtDefinition BuildProp(string asset, float emission, IReadOnlyDictionary<string, ModelClipRole> roles)
        {
            string folder = BlenderArtBuilder.AssetFolder(asset);
            string modelPath = $"{folder}/{asset}.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null || !File.Exists($"{folder}/{asset}_Albedo.png"))
            {
                Debug.LogWarning($"Blender boss FX: no model or maps for {asset}.");
                return null;
            }

            BlenderReport report = BlenderReport.Load($"{folder}/{asset}_report.json");
            var look = new BlenderArtEntry(asset, asset) { Emission = emission, Rim = BlenderArtEntry.DefaultRim };
            Material high = BlenderArtBuilder.BuildMaterial(look, report, folder, asset, false);
            Material low = BlenderArtBuilder.BuildMaterial(look, report, $"{folder}/{BlenderSync.LowFolder}", asset + "_Low", true);
            var art = BlenderArtBuilder.LoadOrCreate<ModelArtDefinition>($"{PropArtFolder}/{asset}.asset");
            var so = new SerializedObject(art);
            so.FindProperty("_model").objectReferenceValue = model;
            so.FindProperty("_lod1").objectReferenceValue = null;
            so.FindProperty("_material").objectReferenceValue = high;
            so.FindProperty("_lowMaterial").objectReferenceValue = low;
            so.FindProperty("_scale").floatValue = 1f;
            so.FindProperty("_yaw").floatValue = ExportYaw;
            so.FindProperty("_bodyCentrePivot").boolValue = false;
            so.FindProperty("_height").floatValue = BlenderArtBuilder.ModelTop(model);
            so.FindProperty("_lodScreenHeight").floatValue = PropLodScreenHeight;
            so.FindProperty("_deathFade").vector2Value = Vector2.zero;
            so.FindProperty("_embers").boolValue = false;
            so.FindProperty("_bleeds").boolValue = false;
            WriteClips(so.FindProperty("_clips"), modelPath, roles);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        // The ice block: the prop's lit material made see-through (albedo alpha), glossy, its silhouette dense like real ice.
        private static ModelArtDefinition BuildIceBlock()
        {
            ModelArtDefinition art = BuildProp(BlenderBossFxCatalog.IceBlock, 1.2f, null);
            if (art == null)
            {
                return null;
            }

            foreach (Material material in new[] { art.Material, art.LowMaterial })
            {
                if (material == null)
                {
                    continue;
                }

                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 1f);
                material.SetFloat("_Smoothness", IceSmoothness);
                material.SetFloat("_RimAlpha", IceRimAlpha);
                material.SetColor("_RimColor", new Color(0.6f, 0.9f, 1f));
                material.SetFloat("_RimStrength", 1.1f);
                material.SetFloat("_RimPower", 2.2f);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetOverrideTag("RenderType", "Transparent");
                EditorUtility.SetDirty(material);
            }

            return art;
        }

        private static void WriteClips(SerializedProperty array, string modelPath, IReadOnlyDictionary<string, ModelClipRole> roles)
        {
            var clips = new List<(ModelClipRole Role, AnimationClip Clip)>();
            if (roles != null)
            {
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__") && roles.TryGetValue(clip.name, out ModelClipRole role))
                    {
                        clips.Add((role, clip));
                    }
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

        // README round 34: the fissure kit's two models, its rock (Game/FissureRock, one atlas, a low set) and the depth punch
        // of its gap opening. The scale and timings keep whatever the asset holds.
        private static void WriteFissure(SerializedObject so)
        {
            string folder = BlenderArtBuilder.AssetFolder(BlenderBossFxCatalog.Fissure);
            var kit = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{BlenderBossFxCatalog.Fissure}.fbx");
            var kitLow = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{BlenderBossFxCatalog.Fissure}_LOD1.fbx");
            Material rock = FissureRock(folder, false);
            if (kit == null || rock == null)
            {
                Debug.LogWarning("Blender boss FX: no fissure kit or maps.");
                return;
            }

            so.FindProperty("_fissureKit").objectReferenceValue = kit;
            so.FindProperty("_fissureKitLow").objectReferenceValue = kitLow;
            so.FindProperty("_fissureRock").objectReferenceValue = rock;
            so.FindProperty("_fissureRockLow").objectReferenceValue = FissureRock($"{folder}/{BlenderSync.LowFolder}", true) ?? rock;
            so.FindProperty("_fissurePunch").objectReferenceValue = PunchStage("_Punch", false);
            so.FindProperty("_fissureClear").objectReferenceValue = PunchStage("_PunchClear", true);
        }

        // The two stages of Game/DepthPunch: the mark sets a stencil bit where the opening passes the depth test against the
        // ground (queue +1); the clear writes the far depth there and resets the bit (queue +2); the rock draws after (+3).
        private static Material PunchStage(string suffix, bool clear)
        {
            Material material = LoadOrCreateMaterial($"{MaterialFolder}/{BlenderBossFxCatalog.Fissure}{suffix}.mat", FissurePunchShader);
            material.SetFloat("_ZTest", (float)(clear ? UnityEngine.Rendering.CompareFunction.Always : UnityEngine.Rendering.CompareFunction.LessEqual));
            material.SetFloat("_ZWrite", clear ? 1f : 0f);
            material.SetFloat("_StencilComp", (float)(clear ? UnityEngine.Rendering.CompareFunction.Equal : UnityEngine.Rendering.CompareFunction.Always));
            material.SetFloat("_StencilPass", (float)(clear ? UnityEngine.Rendering.StencilOp.Zero : UnityEngine.Rendering.StencilOp.Replace));
            material.SetFloat("_FarDepth", clear ? 1f : 0f);
            if (clear)
            {
                material.EnableKeyword("_FAR_DEPTH");
            }
            else
            {
                material.DisableKeyword("_FAR_DEPTH");
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry + (clear ? 2 : 1);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material FissureRock(string folder, bool low)
        {
            string name = BlenderBossFxCatalog.Fissure;
            Texture2D albedo = Texture($"{folder}/{name}_Albedo.png");
            if (albedo == null)
            {
                return null;
            }

            Material material = LoadOrCreateMaterial($"{MaterialFolder}/{name}_Rock{(low ? "_Low" : string.Empty)}.mat", FissureRockShader);
            material.SetTexture("_BaseMap", albedo);
            material.SetTexture("_BumpMap", Texture($"{folder}/{name}_Normal.png"));
            material.SetTexture("_EmissionMap", Texture($"{folder}/{name}_Emission.png"));
            material.SetTexture("_MaskMap", Texture($"{folder}/{name}_Mask.png"));
            material.SetColor("_EmissionColor", FissureGlow);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void WriteDrained(SerializedProperty array)
        {
            var pairs = new List<(Texture Lit, Texture Drained)>();
            foreach ((string folder, string lit, string drained) in BlenderBossFxCatalog.DrainedMaps)
            {
                string root = BlenderArtBuilder.AssetFolder(folder);
                Texture litMap = Texture($"{root}/{lit}.png");
                Texture drainedMap = Texture($"{root}/{drained}.png");
                if (litMap != null && drainedMap != null)
                {
                    pairs.Add((litMap, drainedMap));
                }

                Texture litLow = Texture($"{root}/{BlenderSync.LowFolder}/{lit}.png");
                Texture drainedLow = Texture($"{root}/{BlenderSync.LowFolder}/{drained}.png");
                if (litLow != null && drainedLow != null)
                {
                    pairs.Add((litLow, drainedLow));
                }
            }

            array.arraySize = pairs.Count;
            for (int i = 0; i < pairs.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_lit").objectReferenceValue = pairs[i].Lit;
                element.FindPropertyRelative("_drained").objectReferenceValue = pairs[i].Drained;
            }
        }

        // The wreck stands where the derrick stood: same scale and turn as the cutscene drill it replaces.
        private static void ApplyWreck()
        {
            var rig = AssetDatabase.LoadAssetAtPath<ColonyRigDefinition>(RigPath);
            ModelArtDefinition wreck = BuildProp(BlenderBossFxCatalog.DrillWreck, BlenderArtEntry.WreckEmission, WreckRoles);
            if (rig == null || wreck == null)
            {
                return;
            }

            // README round 31: the collapse's main moment (the top hitting the ground) for the sparks and the second dust.
            string report = $"{BlenderArtBuilder.AssetFolder(BlenderBossFxCatalog.DrillWreck)}/{BlenderBossFxCatalog.DrillWreck}_report.json";
            float impact = File.Exists(report) ? CollapseFrame(File.ReadAllText(report)) : 0f;
            var clips = new SerializedObject(wreck).FindProperty("_clips");
            for (int i = 0; i < clips.arraySize; i++)
            {
                SerializedProperty clip = clips.GetArrayElementAtIndex(i);
                if (clip.FindPropertyRelative("_role").enumValueIndex == (int)ModelClipRole.Death)
                {
                    clip.FindPropertyRelative("_eventSeconds").floatValue = impact / ClipFramesPerSecond;
                }
            }

            clips.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (rig.Drill != null)
            {
                var wreckObject = new SerializedObject(wreck);
                wreckObject.FindProperty("_scale").floatValue = rig.Drill.Scale;
                wreckObject.FindProperty("_yaw").floatValue = rig.Drill.Yaw;
                wreckObject.ApplyModifiedPropertiesWithoutUndo();
            }

            var so = new SerializedObject(rig);
            so.FindProperty("_wreck").objectReferenceValue = wreck;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rig);
        }

        private static float CollapseFrame(string json)
        {
            Dictionary<string, object> root = BlenderJson.Object(BlenderJson.Parse(json));
            Dictionary<string, object> collapse = BlenderJson.Object(BlenderJson.Field(BlenderJson.Object(BlenderJson.Field(root, "clips")), "Collapse"));
            return BlenderJson.Number(BlenderJson.Field(BlenderJson.Object(BlenderJson.Field(collapse, "events")), "top_hits_ground"));
        }

        private static string FxFolder(bool low) =>
            $"{BlenderSync.TargetRoot}/{BlenderBossFxCatalog.Folder}{(low ? "/" + BlenderSync.LowFolder : string.Empty)}";

        private static Texture2D Texture(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        private static Material LoadOrCreateMaterial(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new System.InvalidOperationException($"Shader '{shaderName}' not found; is it compiled?");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            return material;
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
