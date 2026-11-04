using System.Collections.Generic;
using System.IO;
using Game.Levels;
using Game.UI;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// ArtSource README round 26: syncs the menu island and the hive mouth, builds their <c>Game/EnvironmentLit</c>
    /// materials (High with the full maps, Low with the half-size ones) and writes <see cref="MenuDioramaArt"/> from
    /// <c>Menu_Island_layout.json</c> (camera, height map, pool outlines, lights and the 105 placed exports, which are the
    /// level exports already imported), then hands it to the main menu stage. The layout is in Unity world space, so it
    /// is applied as written. Re-running updates the same assets in place.
    /// </summary>
    public static class BlenderMenuBuilder
    {
        public const string ArtPath = BlenderArtBuilder.ArtDataFolder + "/Menu/MenuDioramaArt.asset";
        private const string IslandMeshPath = BlenderArtBuilder.ArtDataFolder + "/Menu/Menu_Island_Split.asset";
        private const string StagePath = "Assets/_Project/Data/Menu/MainMenuStage.asset";
        private const string MenuMaterialFolder = BlenderArtBuilder.MaterialFolder + "/Menu";
        private const string EnvMaterialFolder = BlenderArtBuilder.MaterialFolder + "/Env";
        private const string Island = BlenderModelImporter.MenuIslandAsset;
        private const string Mouth = BlenderModelImporter.MenuMouthAsset;
        private const string Layout = "Menu_Island_layout.json";
        private const string HeightMap = "Menu_Island_Height.png";
        private const string ExportPrefix = "ArtSource/Exports/";
        private const string LowSuffix = "_Low";
        private const string FarSuffix = "_Far";
        private static readonly string[] IslandParts = { "Menu_Island_Top", "Menu_Island_Cliff" };
        // README round 26: top u = (x - 3.6 + 13) / 26, v = (z - 2.2 + 13) / 26.
        private static readonly Vector2 HeightOrigin = new Vector2(3.6f - 13f, 2.2f - 13f);
        private const float HeightSize = 26f;
        // Our FBX imports come out turned half a turn about Y from the README's frame (COMMANDS, import stage 3a), so the
        // island is turned back and every placed export gets the same half turn on top of its layout yaw.
        private const float ImportYaw = 180f;
        // How far (in UV) a vertex may stray from the Top mapping and still count as Top.
        private const float TopUvTolerance = 0.004f;
        private const float Smoothness = 0.85f;
        private const float OcclusionStrength = 0.6f;
        // The island is painted for Blender's brighter preview light; under the hive theme's rig its near-black rock needs a
        // lift to read as ground (the levels' ground sets are painted lighter). The glow keeps its painted strength.
        private const float IslandAlbedo = 2.2f;
        private const float IslandGlow = 1.4f;
        private static readonly string[] PoseClips = { "Idle", "IdleSweep" };
        private static readonly string[] FlatPrefixes = { "Scatter_", "BuildPadTile" };

        [MenuItem("Tools/Generate/Blender Menu Diorama (sync, island, props, pools)")]
        public static void BuildMenu()
        {
            MenuDioramaArt art = SyncAndBuild();
            bool applied = Apply(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender menu: {(art != null ? $"{art.Placements.Length} placement(s), {art.Pools.Length} pool(s)" : "no island")}, stage {(applied ? "wears it" : "unchanged")}.");
        }

        /// <summary>Syncs the exports and writes the art; null when the island is not in Assets.</summary>
        public static MenuDioramaArt SyncAndBuild()
        {
            BlenderSync.Sync(new[] { Island, Mouth });
            BlenderSync.SyncFiles(Island, new[] { HeightMap }, Layout);
            string folder = BlenderArtBuilder.AssetFolder(Island);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{Island}.fbx");
            string layoutPath = $"{folder}/{Layout}";
            if (model == null || !File.Exists(layoutPath))
            {
                Debug.LogWarning($"Blender menu: no island or layout at {folder}.");
                return null;
            }

            Dictionary<string, object> layout = BlenderJson.Object(BlenderJson.Parse(File.ReadAllText(layoutPath)));
            var art = BlenderArtBuilder.LoadOrCreate<MenuDioramaArt>(ArtPath);
            var so = new SerializedObject(art);
            WriteIsland(so, model, folder);
            WriteHeight(so, BlenderJson.Object(BlenderJson.Field(layout, "height_map")), folder);
            WriteCamera(so, BlenderJson.Object(BlenderJson.Field(layout, "camera")));
            WriteItems(so, BlenderJson.Array(BlenderJson.Field(layout, "items")));
            WritePools(so, BlenderJson.Array(BlenderJson.Field(layout, "pools")));
            WriteLights(so, BlenderJson.Array(BlenderJson.Field(layout, "lights")));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        /// <summary>Hands the art to the main menu stage; false when there is none to hand.</summary>
        public static bool Apply(MenuDioramaArt art)
        {
            var stage = AssetDatabase.LoadAssetAtPath<MenuStageDefinition>(StagePath);
            if (stage == null || art == null)
            {
                return false;
            }

            var so = new SerializedObject(stage);
            so.FindProperty("_art").objectReferenceValue = art;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
            return true;
        }

        private static void WriteIsland(SerializedObject so, GameObject model, string folder)
        {
            so.FindProperty("_island").objectReferenceValue = SplitIsland(model);
            SerializedProperty high = so.FindProperty("_islandMaterials");
            SerializedProperty low = so.FindProperty("_islandLowMaterials");
            high.arraySize = IslandParts.Length;
            low.arraySize = IslandParts.Length;
            for (int i = 0; i < IslandParts.Length; i++)
            {
                high.GetArrayElementAtIndex(i).objectReferenceValue = BuildMaterial(folder, IslandParts[i], false);
                low.GetArrayElementAtIndex(i).objectReferenceValue = BuildMaterial(folder, IslandParts[i], true);
            }
        }

        // The export carries one material slot for the whole island, so the faces are sorted here: a triangle whose UVs
        // follow the Top mapping (README: u = (x + 9.4) / 26, v = (z + 10.8) / 26) is Top, the rest is the Cliff. The
        // vertices are baked into the layout's world frame, so the island is placed at the origin unturned.
        private static Mesh SplitIsland(GameObject model)
        {
            MeshFilter filter = model.GetComponentInChildren<MeshFilter>();
            Mesh source = filter.sharedMesh;
            Matrix4x4 frame = Matrix4x4.Rotate(Quaternion.Euler(0f, ImportYaw, 0f)) * filter.transform.localToWorldMatrix;
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector4[] tangents = source.tangents;
            Vector2[] uv = source.uv;
            var top = new bool[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = frame.MultiplyPoint3x4(vertices[i]);
                normals[i] = frame.MultiplyVector(normals[i]).normalized;
                Vector3 tangent = frame.MultiplyVector(tangents[i]).normalized;
                tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[i].w);
                var expected = new Vector2((vertices[i].x - HeightOrigin.x) / HeightSize, (vertices[i].z - HeightOrigin.y) / HeightSize);
                top[i] = (uv[i] - expected).sqrMagnitude < TopUvTolerance * TopUvTolerance;
            }

            int[] triangles = source.triangles;
            var topTriangles = new List<int>(triangles.Length);
            var cliffTriangles = new List<int>(triangles.Length);
            for (int t = 0; t < triangles.Length; t += 3)
            {
                bool isTop = top[triangles[t]] && top[triangles[t + 1]] && top[triangles[t + 2]];
                List<int> list = isTop ? topTriangles : cliffTriangles;
                list.Add(triangles[t]);
                list.Add(triangles[t + 1]);
                list.Add(triangles[t + 2]);
            }

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(IslandMeshPath);
            if (mesh == null)
            {
                BlenderArtBuilder.EnsureFolder(Path.GetDirectoryName(IslandMeshPath)?.Replace('\\', '/'));
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, IslandMeshPath);
            }

            mesh.Clear();
            mesh.name = Path.GetFileNameWithoutExtension(IslandMeshPath);
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.uv = uv;
            mesh.subMeshCount = IslandParts.Length;
            mesh.SetTriangles(topTriangles, 0);
            mesh.SetTriangles(cliffTriangles, 1);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            Debug.Log($"Blender menu: island split into {topTriangles.Count / 3} top and {cliffTriangles.Count / 3} cliff triangles.");
            return mesh;
        }

        private static void WriteHeight(SerializedObject so, Dictionary<string, object> height, string folder)
        {
            so.FindProperty("_heightMap").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{HeightMap}");
            so.FindProperty("_minHeight").floatValue = BlenderJson.Number(BlenderJson.Field(height, "min_y"));
            so.FindProperty("_maxHeight").floatValue = BlenderJson.Number(BlenderJson.Field(height, "max_y"));
            so.FindProperty("_heightOrigin").vector2Value = HeightOrigin;
            so.FindProperty("_heightSize").floatValue = HeightSize;
        }

        private static void WriteCamera(SerializedObject so, Dictionary<string, object> camera)
        {
            so.FindProperty("_cameraFocus").vector3Value = BlenderJson.Vector(BlenderJson.Field(camera, "focus"));
            so.FindProperty("_cameraDistance").floatValue = BlenderJson.Number(BlenderJson.Field(camera, "distance"), 15.5f);
            so.FindProperty("_cameraPitch").floatValue = BlenderJson.Number(BlenderJson.Field(camera, "pitch"), 29f);
            so.FindProperty("_cameraYaw").floatValue = BlenderJson.Number(BlenderJson.Field(camera, "yaw"));
            so.FindProperty("_cameraFieldOfView").floatValue = BlenderJson.Number(BlenderJson.Field(camera, "fov"), 36f);
        }

        // One model per distinct export, then one placement per item pointing at it.
        private static void WriteItems(SerializedObject so, List<object> items)
        {
            var models = new List<MenuDioramaModel>();
            var index = new Dictionary<string, int>();
            var placements = new List<MenuDioramaPlacement>();
            foreach (object entry in items)
            {
                Dictionary<string, object> item = BlenderJson.Object(entry);
                string asset = BlenderJson.Text(BlenderJson.Field(item, "asset"));
                string folder = BlenderJson.Text(BlenderJson.Field(item, "folder")) ?? string.Empty;
                if (string.IsNullOrEmpty(asset))
                {
                    continue;
                }

                if (!index.TryGetValue(asset, out int modelIndex))
                {
                    MenuDioramaModel model = BuildModel(asset, folder.StartsWith(ExportPrefix) ? folder.Substring(ExportPrefix.Length) : folder);
                    if (model == null)
                    {
                        continue;
                    }

                    modelIndex = models.Count;
                    models.Add(model);
                    index[asset] = modelIndex;
                }

                placements.Add(new MenuDioramaPlacement
                {
                    Model = modelIndex,
                    Position = BlenderJson.Vector(BlenderJson.Field(item, "position")),
                    Yaw = BlenderJson.Number(BlenderJson.Field(item, "yaw")) + ImportYaw,
                    Scale = BlenderJson.Number(BlenderJson.Field(item, "scale"), 1f),
                    GlowPool = BlenderJson.Flag(BlenderJson.Field(item, "glow_pool")),
                    Theme = GroupTheme(BlenderJson.Text(BlenderJson.Field(item, "group"))),
                });
            }

            WriteArray(so.FindProperty("_models"), models);
            WriteArray(so.FindProperty("_placements"), placements);
        }

        private static MenuDioramaModel BuildModel(string asset, string exportFolder)
        {
            string path = $"{BlenderSync.TargetRoot}/{exportFolder}/{asset}.fbx";
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                Debug.LogWarning($"Blender menu: {asset} is not imported ({path}); it is left out.");
                return null;
            }

            bool mouth = asset == Mouth;
            Material high = mouth ? BuildMaterial(BlenderArtBuilder.AssetFolder(Mouth), Mouth, false) : FindMaterial(asset, exportFolder, false);
            Material low = mouth ? BuildMaterial(BlenderArtBuilder.AssetFolder(Mouth), Mouth, true) : FindMaterial(asset, exportFolder, true);
            if (high == null)
            {
                Debug.LogWarning($"Blender menu: no material for {asset}; it is left out.");
                return null;
            }

            if (root.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            {
                return new MenuDioramaModel(asset, null, root, high, low, PoseOf(path), false);
            }

            MeshFilter filter = NearMesh(root);
            if (filter == null)
            {
                Debug.LogWarning($"Blender menu: no mesh in {path}; it is left out.");
                return null;
            }

            // In a model asset the root's world transform is its own, so the node's world matrix is its place in the FBX.
            var mesh = new EnvironmentMesh(asset, filter.sharedMesh, high, low, CastsShadow(asset), filter.transform.localToWorldMatrix);
            return new MenuDioramaModel(asset, mesh, null, null, null, null, mouth);
        }

        private static MeshFilter NearMesh(GameObject root)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.name.EndsWith(FarSuffix))
                {
                    return filter;
                }
            }

            return null;
        }

        private static AnimationClip PoseOf(string path)
        {
            AnimationClip first = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(asset is AnimationClip clip) || clip.name.StartsWith("__preview__"))
                {
                    continue;
                }

                if (System.Array.IndexOf(PoseClips, clip.name) >= 0)
                {
                    return clip;
                }

                first ??= clip;
            }

            return first;
        }

        // The level kit's own material (environment folder first, then the art model's, then the tower family's).
        private static Material FindMaterial(string asset, string exportFolder, bool low)
        {
            string suffix = low ? LowSuffix : string.Empty;
            string family = Path.GetFileName(exportFolder);
            foreach (string candidate in new[] { $"{EnvMaterialFolder}/{asset}{suffix}.mat", $"{BlenderArtBuilder.MaterialFolder}/{asset}{suffix}.mat", $"{BlenderArtBuilder.MaterialFolder}/{family}{suffix}.mat" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(candidate);
                if (material != null)
                {
                    return material;
                }
            }

            return null;
        }

        private static bool CastsShadow(string asset)
        {
            foreach (string prefix in FlatPrefixes)
            {
                if (asset.StartsWith(prefix))
                {
                    return false;
                }
            }

            return true;
        }

        // Pools, lights and glows take the colours of the corner they belong to; the open ground takes the hive's.
        private static MenuThemeSlot GroupTheme(string group)
        {
            switch (group)
            {
                case "pond": return MenuThemeSlot.Forest;
                case "lava": return MenuThemeSlot.Lava;
                case "sand": return MenuThemeSlot.Ruins;
                case "ice": return MenuThemeSlot.Ice;
                case "bog": return MenuThemeSlot.Swamp;
                default: return MenuThemeSlot.Nest;
            }
        }

        private static MenuThemeSlot ThemeSlot(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return MenuThemeSlot.None;
            }

            string theme = name.Split('.')[0];
            if (theme.EndsWith("Theme"))
            {
                theme = theme.Substring(0, theme.Length - "Theme".Length);
            }

            return System.Enum.TryParse(theme, out MenuThemeSlot slot) ? slot : MenuThemeSlot.None;
        }

        private static void WritePools(SerializedObject so, List<object> pools)
        {
            var result = new List<MenuDioramaPool>();
            foreach (object entry in pools)
            {
                Dictionary<string, object> pool = BlenderJson.Object(entry);
                if (!System.Enum.TryParse(BlenderJson.Text(BlenderJson.Field(pool, "kind")), out TerrainKind kind))
                {
                    continue;
                }

                List<object> points = BlenderJson.Array(BlenderJson.Field(pool, "outline_xz"));
                var outline = new Vector2[points.Count];
                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 point = BlenderJson.Vector(points[i]);
                    outline[i] = new Vector2(point.x, point.y);
                }

                Vector3 centre = BlenderJson.Vector(BlenderJson.Field(pool, "centre"));
                result.Add(new MenuDioramaPool(
                    kind,
                    ThemeSlot(BlenderJson.Text(BlenderJson.Field(pool, "theme"))),
                    new Vector2(centre.x, centre.y),
                    BlenderJson.Number(BlenderJson.Field(pool, "radius")),
                    BlenderJson.Number(BlenderJson.Field(pool, "surface_y")),
                    BlenderJson.Number(BlenderJson.Field(pool, "bank_degrees")),
                    outline));
            }

            WriteArray(so.FindProperty("_pools"), result);
        }

        // A light's colour is either a theme's accent ("NestTheme.AccentColor") or its own [r, g, b].
        private static void WriteLights(SerializedObject so, List<object> lights)
        {
            var result = new List<MenuDioramaLight>();
            foreach (object entry in lights)
            {
                Dictionary<string, object> light = BlenderJson.Object(entry);
                object colour = BlenderJson.Field(light, "colour");
                Vector3 rgb = colour is string ? Vector3.one : BlenderJson.Vector(colour);
                result.Add(new MenuDioramaLight
                {
                    Name = BlenderJson.Text(BlenderJson.Field(light, "name")) ?? "Light",
                    Position = BlenderJson.Vector(BlenderJson.Field(light, "position")),
                    Theme = ThemeSlot(colour as string),
                    Colour = new Color(rgb.x, rgb.y, rgb.z, 1f),
                    Intensity = BlenderJson.Number(BlenderJson.Field(light, "intensity"), 1f),
                    Range = BlenderJson.Number(BlenderJson.Field(light, "range"), 4f),
                });
            }

            WriteArray(so.FindProperty("_lights"), result);
        }

        // The environment material setup (README round 26: Albedo and Emission sRGB, Normal, Mask linear R AO G gloss).
        private static Material BuildMaterial(string folder, string name, bool low)
        {
            string path = $"{MenuMaterialFolder}/{name}{(low ? LowSuffix : string.Empty)}.mat";
            Shader shader = Shader.Find(EnvironmentShaders.Lit);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                BlenderArtBuilder.EnsureFolder(MenuMaterialFolder);
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.shaderKeywords = new string[0];
            string maps = low && File.Exists($"{folder}/{BlenderSync.LowFolder}/{name}_Albedo.png") ? $"{folder}/{BlenderSync.LowFolder}" : folder;
            float albedo = name == Mouth ? 1f : IslandAlbedo;
            material.SetColor("_BaseColor", new Color(albedo, albedo, albedo, 1f));
            material.SetTexture("_BaseMap", Texture($"{maps}/{name}_Albedo.png"));
            SetMap(material, "_BumpMap", Texture($"{maps}/{name}_Normal.png"), "_NORMALMAP");
            SetMap(material, "_MaskMap", Texture($"{maps}/{name}_Mask.png"), "_MASKMAP");
            SetMap(material, "_EmissionMap", Texture($"{maps}/{name}_Emission.png"), "_EMISSION");
            material.SetColor("_EmissionColor", Color.white * (name == Mouth ? 1f : IslandGlow));
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetFloat("_Smoothness", Smoothness);
            material.SetFloat("_OcclusionStrength", OcclusionStrength);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
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

        private static void WriteArray<T>(SerializedProperty array, IList<T> items)
        {
            array.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                array.GetArrayElementAtIndex(i).boxedValue = items[i];
            }
        }
    }
}
