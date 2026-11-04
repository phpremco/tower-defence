using System.Collections.Generic;
using System.IO;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Import stage 4a: turns the synced Blender environment exports into one <see cref="EnvironmentSet"/> per continent
    /// (<see cref="BlenderEnvironmentCatalog"/>): the tiling ground layers with their tile sizes from the reports, one
    /// <c>Game/EnvironmentLit</c> material per kit and decor mesh and profile (High with the detail normal, Low without),
    /// mirrored copies of the asymmetric kerb ends, the decor scale that matches the procedural kind it replaces, and
    /// points every campaign level's theme at its continent's set. Re-running updates the same assets in place.
    /// </summary>
    public static class BlenderEnvironmentBuilder
    {
        public const string DataFolder = BlenderArtBuilder.ArtDataFolder + "/Environment";
        private const string MaterialFolder = BlenderArtBuilder.MaterialFolder + "/Env";
        private const string MeshFolder = DataFolder + "/Meshes";
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string LowSuffix = "_Low";
        private const string MirroredSuffix = "_Mirrored";
        private const float Smoothness = 0.85f;
        private const float OcclusionStrength = 0.5f;
        private const float DetailStrength = 0.6f;

        // The pads at rest: a faint outline and a tile darker than its bake, so the field does not read as a lit grid.
        private const float PadCalmGlow = 0.08f;
        private const float PadCalmAlbedo = 0.65f;

        // Glow of each ground layer: the grit and the cliff veins stay a hint, the plaza sigil and the rift fissures burn.
        private const float GroundGlow = 1f;
        private const float AltGlow = 1.3f;
        private const float RoadGlow = 0f;
        private const float PlazaGlow = 1.6f;
        private const float CliffGlow = 1f;
        private const float ElementGlow = 2.2f;

        [MenuItem("Tools/Generate/Blender Environment (sync, ground sets, kit, decor)")]
        public static void BuildMenu()
        {
            Dictionary<string, EnvironmentSet> sets = SyncAndBuild();
            int themes = ApplyFromCampaign(sets);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender environment: {sets.Count} set(s), {themes} theme(s) wear one.");
        }

        /// <summary>Syncs the environment exports and builds every continent row; returns the sets by continent key.</summary>
        public static Dictionary<string, EnvironmentSet> SyncAndBuild()
        {
            BlenderSync.Sync(BlenderEnvironmentCatalog.SyncedAssets());
            var sets = new Dictionary<string, EnvironmentSet>();
            foreach (BlenderEnvironmentCatalog.ContinentRow row in BlenderEnvironmentCatalog.Continents)
            {
                EnvironmentSet set = Build(row);
                if (set != null)
                {
                    sets[row.Key] = set;
                }
            }

            return sets;
        }

        /// <summary>Points each theme at the set of its continent (or at none); returns how many wear one.</summary>
        public static int Apply(IReadOnlyDictionary<string, EnvironmentSet> sets, IEnumerable<(VisualTheme Theme, string Key)> themes)
        {
            int assigned = 0;
            var done = new HashSet<VisualTheme>();
            foreach ((VisualTheme theme, string key) in themes)
            {
                if (theme == null || !done.Add(theme))
                {
                    continue;
                }

                sets.TryGetValue(key, out EnvironmentSet set);
                var so = new SerializedObject(theme);
                so.FindProperty("_environment").objectReferenceValue = set;
                so.ApplyModifiedPropertiesWithoutUndo();
                assigned += set != null ? 1 : 0;
            }

            return assigned;
        }

        // The menu path: the continent of each level comes from the campaign catalog.
        private static int ApplyFromCampaign(IReadOnlyDictionary<string, EnvironmentSet> sets)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            if (catalog == null)
            {
                Debug.LogWarning("Blender environment: no campaign catalog yet; run the full generator.");
                return 0;
            }

            var themes = new List<(VisualTheme, string)>();
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null && node.Region >= 0 && node.Region < catalog.Regions.Count)
                {
                    themes.Add((node.Level.Theme, catalog.Regions[node.Region].Key));
                }
            }

            return Apply(sets, themes);
        }

        private static EnvironmentSet Build(BlenderEnvironmentCatalog.ContinentRow row)
        {
            EnvironmentSet set = BlenderArtBuilder.LoadOrCreate<EnvironmentSet>($"{DataFolder}/{row.SetName}.asset");
            var so = new SerializedObject(set);
            so.FindProperty("_prefix").stringValue = row.Prefix;
            so.FindProperty("_groundShader").objectReferenceValue = Shader.Find(EnvironmentShaders.Ground);
            SetLayer(so.FindProperty("_ground"), row.TilingAsset(row.Ground), GroundGlow);
            SetLayer(so.FindProperty("_groundAlt"), row.TilingAsset(row.GroundAlt), AltGlow);
            SetLayer(so.FindProperty("_road"), row.TilingAsset(row.Road), RoadGlow);
            SetLayer(so.FindProperty("_plaza"), row.TilingAsset(row.Plaza), PlazaGlow);
            SetLayer(so.FindProperty("_cliff"), row.TilingAsset(row.Cliff), CliffGlow);
            SetLayer(so.FindProperty("_element"), row.TilingAsset(row.Element), ElementGlow);
            so.FindProperty("_elementTerrain").enumValueIndex = (int)row.ElementTerrain;
            so.FindProperty("_detailNormal").objectReferenceValue = Texture($"{BlenderSync.TargetRoot}/{BlenderSync.DetailFolder}/{BlenderEnvironmentCatalog.DetailNormal}");

            var pieces = new List<EnvironmentPiece>();
            foreach (KeyValuePair<EnvironmentPieceKind, string> pair in row.Pieces)
            {
                EnvironmentMesh model = BuildMesh(row.PieceAsset(pair.Value));
                if (model == null)
                {
                    continue;
                }

                pieces.Add(new EnvironmentPiece(pair.Key, model));
                EnvironmentPieceKind? mirrored = MirroredKind(pair.Key);
                if (mirrored.HasValue)
                {
                    pieces.Add(new EnvironmentPiece(mirrored.Value, Mirror(model)));
                }
            }

            WriteArray(so.FindProperty("_pieces"), pieces);
            WriteArray(so.FindProperty("_decor"), BuildDecor(row));
            WriteArray(so.FindProperty("_origins"), BuildOrigins(row));
            WriteArray(so.FindProperty("_scatter"), BuildMeshes(row.Scatter));
            WriteArray(so.FindProperty("_nestScatter"), BuildMeshes(row.NestScatter));
            WriteArray(so.FindProperty("_silhouettes"), BuildMeshes(row.Silhouettes));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            return set;
        }

        private static EnvironmentPieceKind? MirroredKind(EnvironmentPieceKind kind)
        {
            switch (kind)
            {
                case EnvironmentPieceKind.RoadEdgeEndCap: return EnvironmentPieceKind.RoadEdgeEndCapMirrored;
                case EnvironmentPieceKind.RoadEdgePlazaJoin: return EnvironmentPieceKind.RoadEdgePlazaJoinMirrored;
                default: return null;
            }
        }

        private static List<EnvironmentDecor> BuildDecor(BlenderEnvironmentCatalog.ContinentRow row)
        {
            var decor = new List<EnvironmentDecor>();
            foreach (BlenderEnvironmentCatalog.DecorRow entry in row.Decor)
            {
                List<EnvironmentMesh> variants = BuildMeshes(entry.Assets);
                if (variants.Count == 0)
                {
                    continue;
                }

                decor.Add(new EnvironmentDecor(entry.Kind, variants.ToArray(), MatchingScale(entry.Kind, variants), entry.Glow));
            }

            return decor;
        }

        private static List<EnvironmentOrigin> BuildOrigins(BlenderEnvironmentCatalog.ContinentRow row)
        {
            var origins = new List<EnvironmentOrigin>();
            foreach ((TerrainKind terrain, string asset, bool tint, float scale) in row.Origins)
            {
                EnvironmentMesh model = BuildMesh(asset);
                if (model != null)
                {
                    origins.Add(new EnvironmentOrigin(terrain, model, tint, scale));
                }
            }

            return origins;
        }

        // The imported decoration stands in at the size the procedural kind had: the scatter's scales were tuned on those.
        private static float MatchingScale(PropKind kind, List<EnvironmentMesh> variants)
        {
            float procedural = 0f;
            for (int v = 0; v < PropFactory.Variants; v++)
            {
                Mesh mesh = PropFactory.Create(kind, v);
                procedural += Largest(mesh.bounds.size);
                Object.DestroyImmediate(mesh);
            }

            procedural /= PropFactory.Variants;
            float imported = 0f;
            foreach (EnvironmentMesh variant in variants)
            {
                imported += Largest(variant.Mesh.bounds.size);
            }

            imported /= variants.Count;
            return imported > 0.001f ? procedural / imported : 1f;
        }

        private static float Largest(Vector3 size) => Mathf.Max(size.x, Mathf.Max(size.y, size.z));

        private static List<EnvironmentMesh> BuildMeshes(IEnumerable<string> assets)
        {
            var meshes = new List<EnvironmentMesh>();
            foreach (string asset in assets)
            {
                EnvironmentMesh model = BuildMesh(asset);
                if (model != null)
                {
                    meshes.Add(model);
                }
            }

            return meshes;
        }

        private static EnvironmentMesh BuildMesh(string asset)
        {
            string name = BlenderEnvironmentCatalog.Name(asset);
            string folder = BlenderArtBuilder.AssetFolder(asset);
            MeshFilter filter = MeshIn($"{folder}/{name}.fbx", name);
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
            {
                Debug.LogWarning($"Blender environment: no mesh in {folder}/{name}.fbx.");
                return null;
            }

            BlenderReport report = BlenderReport.Load($"{folder}/{name}_report.json");
            Material high = BuildMaterial(asset, folder, name, report, false);
            Material low = BuildMaterial(asset, folder, name, report, true);
            // In a model asset the root's world transform is its own, so the node's world matrix is its place in the FBX.
            return new EnvironmentMesh(name, mesh, high, low, BlenderEnvironmentCatalog.CastsShadow(asset), filter.transform.localToWorldMatrix);
        }

        // The mesh node named after the export, or the only (first) one.
        private static MeshFilter MeshIn(string path, string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
            {
                return null;
            }

            MeshFilter first = null;
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                if (filter.sharedMesh.name == name)
                {
                    return filter;
                }

                first ??= filter;
            }

            return first;
        }

        private static Material BuildMaterial(string asset, string folder, string name, BlenderReport report, bool low)
        {
            string path = $"{MaterialFolder}/{name}{(low ? LowSuffix : string.Empty)}.mat";
            Shader shader = Shader.Find(EnvironmentShaders.Lit);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                EnsureFolder(MaterialFolder);
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.shaderKeywords = new string[0];
            string maps = low && File.Exists($"{folder}/{BlenderSync.LowFolder}/{name}_Albedo.png") ? $"{folder}/{BlenderSync.LowFolder}" : folder;
            float albedo = BlenderEnvironmentCatalog.AlbedoOf(asset);
            material.SetColor("_BaseColor", new Color(albedo, albedo, albedo, 1f));
            material.SetTexture("_BaseMap", Texture($"{maps}/{name}_Albedo.png"));
            SetMap(material, "_BumpMap", Texture($"{maps}/{name}_Normal.png"), "_NORMALMAP");
            SetMap(material, "_MaskMap", Texture($"{maps}/{name}_Mask.png"), "_MASKMAP");
            SetMap(material, "_EmissionMap", Texture($"{maps}/{name}_Emission.png"), "_EMISSION");
            material.SetColor("_EmissionColor", Color.white * BlenderEnvironmentCatalog.GlowOf(asset));
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetFloat("_Smoothness", Smoothness);
            material.SetFloat("_OcclusionStrength", OcclusionStrength);
            if (BlenderEnvironmentCatalog.IsPad(asset))
            {
                material.EnableKeyword(EnvironmentShaders.PadKeyword);
                material.SetFloat("_PadCalmGlow", PadCalmGlow);
                material.SetFloat("_PadCalmAlbedo", PadCalmAlbedo);
            }

            if (BlenderEnvironmentCatalog.Sways(asset))
            {
                material.EnableKeyword(EnvironmentShaders.SwayKeyword);
            }

            if (!low)
            {
                SetDetail(material, report);
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        // The primary (mask B) detail family of the report; the environment shader has one detail slot.
        private static void SetDetail(Material material, BlenderReport report)
        {
            foreach (BlenderReport.Detail detail in report.Details())
            {
                if (detail.Channel != 'B')
                {
                    continue;
                }

                Texture2D map = Texture($"{BlenderSync.TargetRoot}/{BlenderSync.DetailFolder}/{detail.Map}");
                if (map == null)
                {
                    return;
                }

                SetMap(material, "_DetailNormalMap", map, "_DETAIL");
                material.SetTextureScale("_DetailNormalMap", Vector2.one * detail.Tiling);
                material.SetFloat("_DetailNormalScale", DetailStrength);
                return;
            }
        }

        private static void SetLayer(SerializedProperty layer, string asset, float glow)
        {
            string name = BlenderEnvironmentCatalog.Name(asset);
            string folder = BlenderArtBuilder.AssetFolder(asset);
            string low = $"{folder}/{BlenderSync.LowFolder}";
            layer.FindPropertyRelative("_albedo").objectReferenceValue = Texture($"{folder}/{name}_Albedo.png");
            layer.FindPropertyRelative("_normal").objectReferenceValue = Texture($"{folder}/{name}_Normal.png");
            layer.FindPropertyRelative("_mask").objectReferenceValue = Texture($"{folder}/{name}_Mask.png");
            layer.FindPropertyRelative("_emission").objectReferenceValue = Texture($"{folder}/{name}_Emission.png");
            layer.FindPropertyRelative("_lowAlbedo").objectReferenceValue = Texture($"{low}/{name}_Albedo.png");
            layer.FindPropertyRelative("_lowNormal").objectReferenceValue = Texture($"{low}/{name}_Normal.png");
            layer.FindPropertyRelative("_lowMask").objectReferenceValue = Texture($"{low}/{name}_Mask.png");
            layer.FindPropertyRelative("_lowEmission").objectReferenceValue = Texture($"{low}/{name}_Emission.png");
            float tile = BlenderReport.Load($"{folder}/{name}_report.json").Number("tile_world");
            layer.FindPropertyRelative("_tileSize").floatValue = tile > 0f ? tile : 2f;
            layer.FindPropertyRelative("_glow").floatValue = glow;
        }

        // A copy mirrored in X (vertices, normals and tangents flipped, triangles turned), saved beside the set, so an
        // asymmetric kerb end needs no negative scale (instanced draws do not flip their culling per instance).
        private static EnvironmentMesh Mirror(EnvironmentMesh model)
        {
            Mesh source = model.Mesh;
            string path = $"{MeshFolder}/{model.Name}{MirroredSuffix}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                EnsureFolder(MeshFolder);
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.name = model.Name + MirroredSuffix;
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector4[] tangents = source.tangents;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].x = -vertices[i].x;
            }

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i].x = -normals[i].x;
            }

            for (int i = 0; i < tangents.Length; i++)
            {
                tangents[i] = new Vector4(-tangents[i].x, tangents[i].y, tangents[i].z, -tangents[i].w);
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.uv = source.uv;
            mesh.colors = source.colors;
            int[] triangles = source.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
            }

            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return new EnvironmentMesh(mesh.name, mesh, model.MaterialFor(false), model.MaterialFor(true), model.CastShadows, model.Node);
        }

        private static void WriteArray<T>(SerializedProperty array, IList<T> items)
        {
            array.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                array.GetArrayElementAtIndex(i).boxedValue = items[i];
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
