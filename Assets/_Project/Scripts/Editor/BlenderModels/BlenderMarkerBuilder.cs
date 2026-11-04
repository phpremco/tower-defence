using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// ArtSource README round 25b: syncs the marker, bar, projectile, beam, debris, scout and particle exports, bakes each
    /// mesh's node transform into a mesh asset of its own (so a view places it with an identity transform in the README's
    /// axes), builds the <c>Game/MarkerPack</c> materials per piece (High and Low) plus the lit and particle materials,
    /// writes <see cref="Game.Core.MarkerArt"/> and hands it to the effect library. Re-running updates in place.
    /// </summary>
    public static partial class BlenderMarkerBuilder
    {
        public const string ArtPath = BlenderArtBuilder.ArtDataFolder + "/Markers/MarkerArt.asset";
        private const string MeshPath = BlenderArtBuilder.ArtDataFolder + "/Markers/MarkerMeshes.asset";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Markers";

        [MenuItem("Tools/Generate/Blender Markers (sync, meshes, materials)")]
        public static void BuildMenu()
        {
            Game.Core.MarkerArt art = SyncAndBuild();
            bool applied = Apply(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender markers: {(art != null ? "built" : "missing")}, effect library {(applied ? "wears it" : "unchanged")}.");
        }

        /// <summary>Copies every export file of the round (both sizes and the reports) into Assets.</summary>
        public static int Sync()
        {
            string source = BlenderSync.SourceRoot;
            if (source == null)
            {
                return 0;
            }

            int copied = 0;
            foreach (string folder in BlenderMarkerCatalog.Folders)
            {
                string from = Path.Combine(source, folder);
                if (!Directory.Exists(from))
                {
                    Debug.LogWarning($"Blender markers: no export folder '{folder}'.");
                    continue;
                }

                var files = new List<string>();
                foreach (string file in Directory.GetFiles(from))
                {
                    string name = Path.GetFileName(file);
                    if (name.EndsWith(".fbx") || name.EndsWith(".png"))
                    {
                        files.Add(name);
                    }
                }

                copied += BlenderSync.SyncFiles(folder, files, folder + "_report.json");
            }

            return copied;
        }
    }
}
