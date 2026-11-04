using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class BlenderMarkerBuilder
    {
        private const string ShaderName = "Game/MarkerPack";
        private const string VfxLibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";
        private const string BoltCoreTexture = BlenderSync.TargetRoot + "/" + BlenderVfxCatalog.Folder + "/Bolt_Core.png";
        private const string ParticleShader = "Game/VfxParticle";
        private const string ParticleFolder = "Particles_Atlas";
        private const string DebrisFolder = "Debris_Chunks";
        private const int CullOff = 0;
        private const int CullBack = 2;
        private const int ZTestLessEqual = 4;
        // The bar plate sits 0.004 behind the lines (README); turned to face the camera it would come out in front.
        private const float PlateBehind = 0.008f;

        // Every round-25b FBX arrives turned half a turn about Y from the README frame (as the menu island and the stage
        // 3a exports): missile and scout sockets, bolt heads and the beam cone all point to -Z, the bar fills to +X.
        private static readonly Matrix4x4 HalfTurn = Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));

        // The README's ten chunks; the two hex pieces first (they glow: the old ember cubes).
        private static readonly string[] DebrisNodes =
        {
            "Debris_HexPanel", "Debris_HexHalf", "Debris_PlateWhite", "Debris_PlateBlue", "Debris_Strut", "Debris_Pipe",
            "Debris_Bracket", "Debris_Hazard", "Debris_RockA", "Debris_RockB",
        };

        /// <summary>How one piece is drawn: its mesh, its channel pack and the gains of the README recipe.</summary>
        private sealed class Spec
        {
            public MarkerKind Kind;
            public string Folder;
            public string Fbx;
            public string Node;
            public string Texture;
            public string TexturePath;
            public Color Core = Color.white;
            public Color Glow = Color.white;
            public Color Third = Color.black;
            public float CoreTint;
            public float Intensity = 1f;
            public float Opacity;
            public Vector2 Scroll;
            public Vector3 SecondScroll;
            public Vector3 SecondMask;
            public bool Ripple;
            public float Darken;
            public int Cull = CullOff;
            public Vector3 Offset;
        }

        private static Color Grey(float value, float alpha = 1f) => new Color(value, value, value, alpha);

        private static readonly Spec[] Specs =
        {
            // R the old radial pool, G a wider halo, B the pulse ring (left dark: pools do not pulse).
            new Spec { Kind = MarkerKind.GlowPool, Folder = "Decal_GlowPool", Fbx = "Decal_Quad", Node = "Quad_Flat", Texture = "Decal_GlowPool", Glow = Grey(0.3f), CoreTint = 1f },
            new Spec { Kind = MarkerKind.GlowBlade, Folder = "Decal_GlowPool", Fbx = "Decal_Quad", Node = "Quad_Upright", Texture = "Glow_Blade", Glow = Grey(0.6f), Third = Grey(0.25f), CoreTint = 1f },
            new Spec { Kind = MarkerKind.EnergyField, Folder = "Decal_GlowPool", Fbx = "Decal_Quad", Node = "Quad_Upright", Texture = "Field_Energy", Glow = Grey(0.35f), Third = Grey(0.3f), CoreTint = 1f },
            new Spec { Kind = MarkerKind.BlobShadow, Folder = "Decal_GlowPool", Fbx = "Decal_Quad", Node = "Quad_Flat", Texture = "Decal_BlobShadow", TexturePath = "Decal_BlobShadow", Core = Color.black, Glow = Color.black, Opacity = 1f },
            new Spec { Kind = MarkerKind.PlacementCursor, Folder = "Marker_PlacementCursor", Fbx = "Marker_PlacementCursor", Node = "Marker_PlacementCursor", Texture = "Marker_PlacementCursor", Glow = Grey(0.35f), Third = Grey(0.5f), CoreTint = 0.7f, Intensity = 1.6f },
            new Spec { Kind = MarkerKind.FreeCell, Folder = "Marker_PlacementCursor", Fbx = "Marker_PlacementCursor", Node = "Marker_FreeCell", Texture = "Marker_FreeCell", Core = Grey(0.6f), Glow = Grey(0.4f), CoreTint = 1f },
            // README: the fill's R and the band's G share one gain so they meet without a seam; the rim stays under the bloom threshold.
            new Spec { Kind = MarkerKind.RangeBand, Folder = "Marker_RangeRing", Fbx = "Marker_RangeRing", Node = "RangeRing_Band", Texture = "Marker_RangeRing", Core = Grey(0.9f), Glow = Grey(0.6f), Third = Grey(0.5f), CoreTint = 0.6f, Scroll = new Vector2(0.02f, 0f) },
            new Spec { Kind = MarkerKind.RangeFill, Folder = "Marker_RangeRing", Fbx = "Marker_RangeRing", Node = "RangeRing_Fill", Texture = "Marker_RangeFill", Core = Grey(0.6f), Glow = Grey(0.3f), CoreTint = 1f },
            new Spec { Kind = MarkerKind.HeroSpot, Folder = "Marker_HeroSpot", Fbx = "Marker_HeroSpot", Node = "Marker_HeroSpot", Texture = "Marker_HeroSpot", Glow = Grey(0.6f), Third = Grey(0.8f), CoreTint = 1f },
            new Spec { Kind = MarkerKind.HeroRing, Folder = "Marker_HeroStand", Fbx = "Marker_HeroStand", Node = "HeroStand_Ring", Texture = "Marker_HeroRing", Glow = Grey(0.6f), Third = Grey(1f), CoreTint = 1f, Intensity = 1.4f },
            new Spec { Kind = MarkerKind.HeroBeacon, Folder = "Marker_HeroStand", Fbx = "Marker_HeroStand", Node = "HeroStand_Beacon", Texture = "Marker_Beacon", Glow = Grey(0.5f), Third = Grey(0.8f), CoreTint = 1f, SecondScroll = new Vector3(0f, 0.5f, 0f), SecondMask = new Vector3(0f, 0f, 1f) },
            new Spec { Kind = MarkerKind.HeroEmblem, Folder = "Marker_HeroStand", Fbx = "Marker_HeroStand", Node = "HeroStand_Emblem", Texture = "Marker_Emblem", Glow = Grey(1f), CoreTint = 1f, Intensity = 1.5f, Opacity = 1f, Cull = CullBack },
            new Spec { Kind = MarkerKind.SlowField, Folder = "Marker_SlowField", Fbx = "Marker_SlowField", Node = "Marker_SlowField", Texture = "Marker_SlowField", Glow = Grey(0.6f), Third = Grey(0.5f), CoreTint = 1f, Ripple = true, SecondScroll = new Vector3(0f, 0f, 0.3f), SecondMask = new Vector3(0f, 1f, 0f) },
            // The light streaks run towards the base (head to +U): the texture moves backwards along U.
            new Spec { Kind = MarkerKind.RoadCue, Folder = "Marker_RoadCue", Fbx = "Marker_RoadCue", Node = "RoadCue_Strip", Texture = "Cue_Trail", Glow = Grey(0.5f), Third = Grey(0.6f), CoreTint = 1f, Scroll = new Vector2(-0.35f, 0f) },
            new Spec { Kind = MarkerKind.FlightCue, Folder = "Marker_RoadCue", Fbx = "Marker_RoadCue", Node = "FlightCue_Strip", Texture = "Cue_Dash", Glow = Grey(0.5f), CoreTint = 1f, Scroll = new Vector2(-0.35f, 0f) },
            new Spec { Kind = MarkerKind.RoutePulse, Folder = "Decal_GlowPool", Fbx = "Decal_Quad", Node = "Quad_Flat", Texture = "Cue_Pulse", TexturePath = "Marker_RoadCue", Glow = Grey(0.6f), CoreTint = 1f },
            // Bars (alpha): plate PlateColor at alpha 0.5 x A with a 0.25 rim; fill tint x G + 0.35 R sheen, B ticks darken 0.25.
            new Spec { Kind = MarkerKind.BarPlate, Folder = "UI_HealthBar", Fbx = "UI_HealthBar", Node = "HealthBar_Plate", Texture = "UI_HealthBar", Core = Grey(0.25f), Glow = Grey(1f), Opacity = 1f, Offset = new Vector3(0f, 0f, PlateBehind) },
            new Spec { Kind = MarkerKind.BarFill, Folder = "UI_HealthBar", Fbx = "UI_HealthBar", Node = "HealthBar_Fill", Texture = "UI_HealthBar", Core = Grey(0.35f), Glow = Grey(1f), Darken = 0.25f, Opacity = 1f, Intensity = 0.9f },
            new Spec { Kind = MarkerKind.BarArmour, Folder = "UI_HealthBar", Fbx = "UI_HealthBar", Node = "HealthBar_Armour", Texture = "UI_HealthBar", Core = Grey(0.35f), Glow = Grey(1f), Opacity = 1f, Intensity = 0.9f },
            // Projectiles: white x R + tint x G (+ tint x B rings on the slug); HDR for the bloom.
            new Spec { Kind = MarkerKind.Bolt, Folder = "Proj_Bolt", Fbx = "Proj_Bolt", Node = "Proj_Bolt", Texture = "Proj_Glow", Core = Grey(3f), Glow = Grey(3f), Cull = CullBack },
            new Spec { Kind = MarkerKind.BoltGlow, Folder = "Proj_Bolt", Fbx = "Proj_Bolt", Node = "Proj_Bolt_Glow", Texture = "Bolt_Core", TexturePath = BoltCoreTexture, Core = Grey(2f), Glow = Grey(2f), Third = Grey(0.8f) },
            new Spec { Kind = MarkerKind.RailSlug, Folder = "Proj_RailSlug", Fbx = "Proj_RailSlug", Node = "Proj_RailSlug", Texture = "Proj_Glow", TexturePath = "Proj_Bolt", Core = Grey(3f), Glow = Grey(3f), Third = Grey(3f), Cull = CullBack },
            new Spec { Kind = MarkerKind.RailSlugGlow, Folder = "Proj_RailSlug", Fbx = "Proj_RailSlug", Node = "Proj_RailSlug_Glow", Texture = "Bolt_Core", TexturePath = BoltCoreTexture, Core = Grey(2f), Glow = Grey(2f), Third = Grey(0.8f) },
            new Spec { Kind = MarkerKind.EnemyBolt, Folder = "Proj_EnemyBolt", Fbx = "Proj_EnemyBolt", Node = "Proj_EnemyBolt", Texture = "Proj_EnemyBolt", Core = new Color(3f, 2.4f, 1.2f), Glow = Grey(2.5f), Darken = 0.6f, Cull = CullBack },
            new Spec { Kind = MarkerKind.EnemyBoltGlow, Folder = "Proj_EnemyBolt", Fbx = "Proj_EnemyBolt", Node = "Proj_EnemyBolt_Glow", Texture = "Bolt_Core", TexturePath = BoltCoreTexture, Core = new Color(1.6f, 1.2f, 0.7f), Glow = Grey(1.6f), Third = Grey(0.5f) },
            new Spec { Kind = MarkerKind.PulseRing, Folder = "Proj_PulseRing", Fbx = "Proj_PulseRing", Node = "Proj_PulseRing", Texture = "Proj_PulseRing", Glow = Grey(0.8f), Third = Grey(0.9f), CoreTint = 0.5f, Intensity = 2f },
            new Spec { Kind = MarkerKind.ToolBeam, Folder = "Beam_Tool", Fbx = "Beam_Tool", Node = "Beam_Tool", Texture = "Beam_Tool", Glow = Grey(0.8f), Third = Grey(0.6f), CoreTint = 1f, Intensity = 1.2f, Scroll = new Vector2(0f, -0.8f) },
            // A TrailRenderer material (Stretch, U head to tail); the quad only makes the piece complete.
            new Spec { Kind = MarkerKind.ScoutTrail, Folder = "Decal_GlowPool", Fbx = "Decal_Quad", Node = "Quad_Flat", Texture = "Trail_Scout", TexturePath = "Recon_Scout", Glow = Grey(0.7f), Third = Grey(0.6f), CoreTint = 0.5f, Intensity = 1.5f },
        };

        public static MarkerArt SyncAndBuild()
        {
            Sync();
            var art = BlenderArtBuilder.LoadOrCreate<MarkerArt>(ArtPath);
            var meshes = new MeshStore(MeshPath);
            var so = new SerializedObject(art);
            SerializedProperty pieces = so.FindProperty("_pieces");
            pieces.arraySize = Specs.Length;
            int built = 0;
            for (int i = 0; i < Specs.Length; i++)
            {
                Spec spec = Specs[i];
                SerializedProperty piece = pieces.GetArrayElementAtIndex(i);
                Mesh mesh = meshes.Bake(Node(spec.Folder, spec.Fbx, spec.Node), spec.Kind.ToString(), Matrix4x4.Translate(spec.Offset));
                piece.FindPropertyRelative("_kind").enumValueIndex = (int)spec.Kind;
                piece.FindPropertyRelative("_mesh").objectReferenceValue = mesh;
                piece.FindPropertyRelative("_material").objectReferenceValue = BuildMaterial(spec, false);
                piece.FindPropertyRelative("_lowMaterial").objectReferenceValue = BuildMaterial(spec, true);
                built += mesh != null ? 1 : 0;
            }

            WriteLit(so.FindProperty("_missile"), meshes, "Proj_Missile", "Proj_Missile", "Socket_Exhaust", "Socket_Nose");
            WriteLit(so.FindProperty("_scout"), meshes, "Recon_Scout", "Recon_Scout", "Socket_Trail", "Socket_Light");
            WriteDebris(so, meshes);
            WriteParticles(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            meshes.Finish();
            EditorUtility.SetDirty(art);
            Debug.Log($"Blender markers: {built}/{Specs.Length} flat pieces.");
            return art;
        }

        /// <summary>Hands the art to the effect library (registered at boot); false when there is none to hand.</summary>
        public static bool Apply(MarkerArt art)
        {
            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(VfxLibraryPath);
            if (library == null || art == null)
            {
                return false;
            }

            var so = new SerializedObject(library);
            so.FindProperty("_markers").objectReferenceValue = art;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            return true;
        }

        private static MeshFilter Node(string folder, string fbx, string node)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{BlenderArtBuilder.AssetFolder(folder)}/{fbx}.fbx");
            if (model == null)
            {
                return null;
            }

            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.name == node)
                {
                    return filter;
                }
            }

            return null;
        }

        private static Transform Socket(string folder, string fbx, string socket)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{BlenderArtBuilder.AssetFolder(folder)}/{fbx}.fbx");
            if (model == null)
            {
                return null;
            }

            foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == socket)
                {
                    return node;
                }
            }

            return null;
        }

        private static Material BuildMaterial(Spec spec, bool low)
        {
            string path = $"{MaterialFolder}/{spec.Kind}{(low ? "_Low" : string.Empty)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(ShaderName);
            if (material == null)
            {
                BlenderArtBuilder.EnsureFolder(MaterialFolder);
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.shaderKeywords = new string[0];
            material.SetTexture("_MainTex", PackTexture(spec, low));
            material.SetColor("_CoreColor", spec.Core);
            material.SetColor("_GlowColor", spec.Glow);
            material.SetColor("_ThirdColor", spec.Third);
            material.SetColor("_Tint", Color.white);
            material.SetFloat("_CoreTint", spec.CoreTint);
            material.SetFloat("_Intensity", spec.Intensity);
            material.SetFloat("_Opacity", spec.Opacity);
            material.SetVector("_Scroll", spec.Scroll);
            material.SetVector("_SecondScroll", spec.SecondScroll);
            material.SetVector("_SecondMask", spec.SecondMask);
            material.SetFloat("_Darken", spec.Darken);
            material.SetFloat("_Fill", 2f);
            material.SetFloat("_CoverFromCore", 0f);
            material.SetFloat("_Cull", spec.Cull);
            material.SetFloat("_ZTest", ZTestLessEqual);
            bool second = spec.SecondMask != Vector3.zero;
            SetKeyword(material, "_SECOND_SAMPLE", "_SecondSample", second);
            SetKeyword(material, "_SECOND_RIPPLE", "_SecondRipple", second && spec.Ripple);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetKeyword(Material material, string keyword, string toggle, bool on)
        {
            material.SetFloat(toggle, on ? 1f : 0f);
            if (on)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        // A texture of the piece's own folder (or another folder of the round, or a full path into the effect set).
        private static Texture2D PackTexture(Spec spec, bool low)
        {
            if (spec.TexturePath != null && spec.TexturePath.EndsWith(".png"))
            {
                string lowPath = spec.TexturePath.Replace("/" + BlenderVfxCatalog.Folder + "/", "/" + BlenderVfxCatalog.Folder + "/" + BlenderSync.LowFolder + "/");
                return Load(low ? lowPath : null) ?? Load(spec.TexturePath);
            }

            string folder = BlenderArtBuilder.AssetFolder(spec.TexturePath ?? spec.Folder);
            return Load(low ? $"{folder}/{BlenderSync.LowFolder}/{spec.Texture}.png" : null) ?? Load($"{folder}/{spec.Texture}.png");
        }

        private static Texture2D Load(string path) => path == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        private static void WriteLit(SerializedProperty lit, MeshStore meshes, string folder, string node, string socketA, string socketB)
        {
            lit.FindPropertyRelative("_mesh").objectReferenceValue = meshes.Bake(Node(folder, folder, node), folder, Matrix4x4.identity);
            lit.FindPropertyRelative("_material").objectReferenceValue = LitMaterial(folder, false);
            lit.FindPropertyRelative("_lowMaterial").objectReferenceValue = LitMaterial(folder, true);
            lit.FindPropertyRelative("_socketA").vector3Value = SocketPosition(folder, socketA);
            lit.FindPropertyRelative("_socketB").vector3Value = SocketPosition(folder, socketB);
        }

        private static Vector3 SocketPosition(string folder, string socket)
        {
            Transform node = Socket(folder, folder, socket);
            return node != null ? HalfTurn.MultiplyPoint3x4(node.position) : Vector3.zero;
        }

        // The tower map set (Albedo, Normal, Emission, Mask) on Game/UnitLit, rimmed in the colony's light.
        private static Material LitMaterial(string folder, bool low)
        {
            string maps = BlenderArtBuilder.AssetFolder(folder);
            if (low)
            {
                maps += "/" + BlenderSync.LowFolder;
            }

            var entry = new BlenderArtEntry(folder, folder) { Colony = true, Rim = BlenderArtEntry.TowerRim, Emission = BlenderArtEntry.TowerEmission };
            BlenderReport report = BlenderReport.Load($"{BlenderArtBuilder.AssetFolder(folder)}/{folder}_report.json");
            return BlenderArtBuilder.BuildMaterial(entry, report, maps, folder + (low ? "_Low" : string.Empty), low);
        }

        private static void WriteDebris(SerializedObject so, MeshStore meshes)
        {
            SerializedProperty chunks = so.FindProperty("_debris");
            var found = new List<Mesh>();
            foreach (string node in DebrisNodes)
            {
                Mesh mesh = meshes.Bake(Node(DebrisFolder, DebrisFolder, node), node, Matrix4x4.identity);
                if (mesh != null)
                {
                    found.Add(mesh);
                }
            }

            chunks.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++)
            {
                chunks.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }

            so.FindProperty("_hotChunks").intValue = 2;
            so.FindProperty("_debrisMaterial").objectReferenceValue = LitMaterial(DebrisFolder, false);
            so.FindProperty("_debrisLowMaterial").objectReferenceValue = LitMaterial(DebrisFolder, true);
        }

        // README: one texture, two materials: additive for motes, embers, frost and chrono, covering for drips and splats.
        // White channels: the systems tint through their start colour.
        private static void WriteParticles(SerializedObject so)
        {
            so.FindProperty("_particleGlow").objectReferenceValue = ParticleMaterial("Particles_Glow", false, 0f);
            so.FindProperty("_particleGlowLow").objectReferenceValue = ParticleMaterial("Particles_Glow", true, 0f);
            so.FindProperty("_particleCover").objectReferenceValue = ParticleMaterial("Particles_Cover", false, 1f);
            so.FindProperty("_particleCoverLow").objectReferenceValue = ParticleMaterial("Particles_Cover", true, 1f);
        }

        private static Material ParticleMaterial(string name, bool low, float opacity)
        {
            string path = $"{MaterialFolder}/{name}{(low ? "_Low" : string.Empty)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(ParticleShader);
            if (material == null)
            {
                BlenderArtBuilder.EnsureFolder(MaterialFolder);
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.shaderKeywords = new string[0];
            string folder = BlenderArtBuilder.AssetFolder(ParticleFolder);
            material.SetTexture("_MainTex", Load(low ? $"{folder}/{BlenderSync.LowFolder}/Particles_Atlas.png" : null) ?? Load($"{folder}/Particles_Atlas.png"));
            material.SetColor("_CoreColor", Grey(1.3f));
            material.SetColor("_GlowColor", Grey(1f));
            material.SetColor("_ThirdColor", Grey(0.6f));
            material.SetColor("_Tint", Color.white);
            material.SetFloat("_Intensity", 1f);
            material.SetFloat("_Opacity", opacity);
            material.SetVector("_Grid", new Vector4(MarkerArt.ParticleColumns, MarkerArt.ParticleRows, 0f, 0f));
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// The baked meshes, kept as sub-assets of one file so a re-run replaces them in place (same objects, same
        /// references) instead of adding copies.
        /// </summary>
        private sealed class MeshStore
        {
            private readonly string _path;
            private readonly Dictionary<string, Mesh> _existing = new Dictionary<string, Mesh>();
            private readonly HashSet<string> _baked = new HashSet<string>();
            private readonly ScriptableObject _holder;

            public MeshStore(string path)
            {
                _path = path;
                _holder = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (_holder == null)
                {
                    BlenderArtBuilder.EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
                    _holder = ScriptableObject.CreateInstance<MarkerMeshStore>();
                    AssetDatabase.CreateAsset(_holder, path);
                }

                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is Mesh mesh)
                    {
                        _existing[mesh.name] = mesh;
                    }
                }
            }

            /// <summary>The node's mesh with its world transform, the half turn and an extra offset baked in.</summary>
            public Mesh Bake(MeshFilter node, string name, Matrix4x4 extra)
            {
                if (node == null || node.sharedMesh == null)
                {
                    Debug.LogWarning($"Blender markers: no mesh for {name}.");
                    return null;
                }

                Mesh source = node.sharedMesh;
                _baked.Add(name);
                Matrix4x4 matrix = extra * HalfTurn * node.transform.localToWorldMatrix;
                if (!_existing.TryGetValue(name, out Mesh mesh))
                {
                    mesh = new Mesh { name = name };
                    AssetDatabase.AddObjectToAsset(mesh, _holder);
                    _existing[name] = mesh;
                }

                mesh.Clear();
                Vector3[] vertices = source.vertices;
                Vector3[] normals = source.normals;
                Vector4[] tangents = source.tangents;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
                    if (i < normals.Length)
                    {
                        normals[i] = matrix.MultiplyVector(normals[i]).normalized;
                    }

                    if (i < tangents.Length)
                    {
                        Vector3 t = matrix.MultiplyVector(tangents[i]).normalized;
                        tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
                    }
                }

                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.tangents = tangents;
                mesh.uv = source.uv;
                mesh.uv2 = source.uv2;
                mesh.colors = source.colors;
                mesh.subMeshCount = 1;
                mesh.triangles = source.triangles;
                mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh);
                return mesh;
            }

            public void Finish()
            {
                // A piece that left the round (the air net's strand) leaves no orphan mesh behind.
                foreach (KeyValuePair<string, Mesh> pair in _existing)
                {
                    if (!_baked.Contains(pair.Key))
                    {
                        Object.DestroyImmediate(pair.Value, true);
                    }
                }

                EditorUtility.SetDirty(_holder);
                AssetDatabase.SaveAssetIfDirty(_holder);
            }
        }
    }
}
