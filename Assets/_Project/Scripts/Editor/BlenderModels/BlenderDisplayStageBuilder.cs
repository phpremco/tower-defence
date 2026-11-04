using Game.Core;
using Game.UI;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// ArtSource README round 27: syncs the upgrade screen's display stage (pedestal with its top ring, halo quad and
    /// texture, backdrop), builds the pedestal's <c>Game/UnitLit</c> material like a tower's with a stronger colony-blue
    /// rim and glow, writes <see cref="DisplayStageArt"/> and hands it to the UI skin. Re-running updates in place.
    /// </summary>
    public static class BlenderDisplayStageBuilder
    {
        public const string ArtPath = BlenderArtBuilder.ArtDataFolder + "/UI/DisplayStageArt.asset";
        private const string SkinPath = "Assets/_Project/Settings/UiSkin.asset";
        private const string Folder = BlenderModelImporter.UiDisplayAsset;
        private const string Pedestal = "UI_DisplayPedestal";
        private const string PedestalRing = "UI_DisplayPedestal_Ring";
        private const string Halo = "UI_DisplayRing";
        private const string Backdrop = "UI_DisplayBackdrop";
        private const string Report = "UI_Display_report.json";
        // README: the white emission map (rim channel, deck ring, seam and ring lamps) takes the colony blue, a bit hotter
        // than the old code ring (1.1) so the rim reads at the preview's size; the halo is a softer 1.1.
        private const float PedestalGlow = 1.3f;
        private const float PedestalRim = 1f;
        private const float HaloGlow = 1.1f;
        // Round 27's FBX nodes arrive turned 270 degrees about X (the island's are 90): upside down, the pedestal rising
        // from its pivot and the halo facing the floor. Half a turn about X puts the pivot back on the top face (README).
        private static readonly Matrix4x4 UpsideDown = Matrix4x4.Rotate(Quaternion.Euler(180f, 0f, 0f));

        [MenuItem("Tools/Generate/Blender Display Stage (sync, pedestal, halo, backdrop)")]
        public static void BuildMenu()
        {
            DisplayStageArt art = SyncAndBuild();
            bool applied = Apply(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender display stage: {(art != null && art.IsComplete ? "complete" : "missing pieces")}, skin {(applied ? "wears it" : "unchanged")}.");
        }

        /// <summary>Syncs the export and writes the art; null when the pedestal is not in Assets.</summary>
        public static DisplayStageArt SyncAndBuild()
        {
            BlenderSync.SyncFiles(Folder, new[]
            {
                Pedestal + ".fbx", Halo + ".fbx", Halo + ".png", Backdrop + ".png",
                Pedestal + "_Albedo.png", Pedestal + "_Normal.png", Pedestal + "_Emission.png", Pedestal + "_Mask.png",
            }, Report);
            string folder = BlenderArtBuilder.AssetFolder(Folder);
            string low = $"{folder}/{BlenderSync.LowFolder}";
            var pedestal = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{Pedestal}.fbx");
            var halo = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{Halo}.fbx");
            if (pedestal == null || halo == null)
            {
                Debug.LogWarning($"Blender display stage: no pedestal or halo at {folder}.");
                return null;
            }

            var art = BlenderArtBuilder.LoadOrCreate<DisplayStageArt>(ArtPath);
            var so = new SerializedObject(art);
            WriteMesh(so, "_pedestal", "_pedestalNode", Find(pedestal, Pedestal));
            WriteMesh(so, "_pedestalRing", "_pedestalRingNode", Find(pedestal, PedestalRing));
            WriteMesh(so, "_halo", "_haloNode", halo.GetComponentInChildren<MeshFilter>());
            BlenderReport report = BlenderReport.Load($"{folder}/{Report}");
            so.FindProperty("_pedestalMaterial").objectReferenceValue = BuildMaterial(report, folder, Pedestal);
            so.FindProperty("_pedestalLowMaterial").objectReferenceValue = BuildMaterial(report, low, Pedestal + "_Low");
            so.FindProperty("_haloTexture").objectReferenceValue = Texture($"{folder}/{Halo}.png");
            so.FindProperty("_haloLowTexture").objectReferenceValue = Texture($"{low}/{Halo}.png");
            so.FindProperty("_haloColour").colorValue = SurfaceMaterials.ColonyRim * HaloGlow;
            so.FindProperty("_backdrop").objectReferenceValue = Texture($"{folder}/{Backdrop}.png");
            so.FindProperty("_backdropLow").objectReferenceValue = Texture($"{low}/{Backdrop}.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            return art;
        }

        /// <summary>Hands the art to the UI skin; false when there is none to hand.</summary>
        public static bool Apply(DisplayStageArt art)
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            if (skin == null || art == null)
            {
                return false;
            }

            var so = new SerializedObject(skin);
            so.FindProperty("_displayStage").objectReferenceValue = art;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skin);
            return true;
        }

        private static MeshFilter Find(GameObject root, string name)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.name == name)
                {
                    return filter;
                }
            }

            return null;
        }

        // In a model asset the root's world transform is its own, so the node's world matrix is its place in the FBX.
        private static void WriteMesh(SerializedObject so, string meshField, string nodeField, MeshFilter filter)
        {
            so.FindProperty(meshField).objectReferenceValue = filter != null ? filter.sharedMesh : null;
            so.FindProperty(nodeField).boxedValue = filter != null ? UpsideDown * filter.transform.localToWorldMatrix : Matrix4x4.identity;
        }

        // A tower's material setup (Albedo, Normal, Emission, Mask with the report's detail tiling), colony blue on the rim and the glow.
        private static Material BuildMaterial(BlenderReport report, string maps, string name)
        {
            var entry = new BlenderArtEntry(name, Pedestal) { Colony = true, Rim = PedestalRim, Emission = PedestalGlow };
            Material material = BlenderArtBuilder.BuildMaterial(entry, report, maps, name, name.EndsWith("_Low"));
            material.SetColor("_EmissionColor", SurfaceMaterials.ColonyRim * PedestalGlow);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D Texture(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
