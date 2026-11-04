using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Copies the Blender exports the game uses from <c>ArtSource/Exports</c> (written by the Blender scripts, git-ignored)
    /// into <c>Assets/_Project/Art/Models/Blender</c>, so the Blender side never touches <c>Assets</c>. A file is copied
    /// only when it is missing or differs in size or write time; the copy takes the source's write time, so an unchanged
    /// export is skipped on the next run. Without an <c>ArtSource</c> folder (a fresh clone) the copies already in
    /// <c>Assets</c> stay as they are.
    /// </summary>
    public static class BlenderSync
    {
        public const string TargetRoot = "Assets/_Project/Art/Models/Blender";
        public const string DetailFolder = "Detail";
        public const string LowFolder = "Low";
        public const string SkinFolder = "Skins";
        public const string EnvironmentFolder = "Env";
        public const string TilingFolder = "Tiling";

        private const string SourceRelative = "ArtSource/Exports";
        private static readonly string[] MapSuffixes = { "_Albedo.png", "_Normal.png", "_Emission.png", "_Mask.png" };

        /// <summary>The export folder on disk, or null when this checkout has none.</summary>
        public static string SourceRoot
        {
            get
            {
                string root = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, SourceRelative);
                return Directory.Exists(root) ? root : null;
            }
        }

        [MenuItem("Tools/Generate/Sync Blender Models")]
        public static void SyncMenu()
        {
            int copied = Sync(BlenderArtCatalog.SyncedAssets());
            Debug.Log($"Blender sync: {copied} file(s) copied.");
        }

        /// <summary>Copies the exports of every named asset plus the shared detail normals; returns the number of files copied.</summary>
        /// <param name="extraModels">Per asset, the further FBX files beside <c>&lt;Asset&gt;.fbx</c> (a tower's tiers), by file name.</param>
        public static int Sync(IEnumerable<string> assets, IReadOnlyDictionary<string, string[]> extraModels = null)
        {
            string source = SourceRoot;
            if (source == null)
            {
                Debug.Log("Blender sync: no ArtSource/Exports folder, keeping the models already in Assets.");
                return 0;
            }

            int copied = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string asset in assets)
                {
                    copied += SyncAsset(source, asset);
                    if (extraModels != null && extraModels.TryGetValue(asset, out string[] models))
                    {
                        copied += SyncModels(source, asset, models);
                    }
                }

                copied += SyncFolder(Path.Combine(source, DetailFolder), Path.Combine(TargetPath, DetailFolder), "Detail_*_Normal.png");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (copied > 0)
            {
                AssetDatabase.Refresh();
            }

            return copied;
        }

        /// <summary>
        /// Copies a texture-only export (a planet): <c>&lt;Asset&gt;_&lt;Map&gt;.png</c> for each map named, the same in
        /// <c>Low/</c>, and the report. Returns the number of files copied.
        /// </summary>
        /// <param name="lowOnly">Maps taken from <c>Low/</c> alone, for both profiles (data that gains nothing from the big size).</param>
        public static int SyncTextures(string asset, IEnumerable<string> maps, IEnumerable<string> lowOnly = null)
        {
            string source = SourceRoot;
            if (source == null)
            {
                Debug.Log("Blender sync: no ArtSource/Exports folder, keeping the textures already in Assets.");
                return 0;
            }

            string from = Path.Combine(source, asset);
            if (!Directory.Exists(from))
            {
                Debug.LogWarning($"Blender sync: no export for '{asset}' in {SourceRelative}.");
                return 0;
            }

            string to = Path.Combine(TargetPath, asset);
            int copied = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                copied += CopyIfChanged(Path.Combine(from, asset + "_report.json"), Path.Combine(to, asset + "_report.json"));
                foreach (string map in maps)
                {
                    string file = $"{asset}_{map}.png";
                    copied += CopyIfChanged(Path.Combine(from, file), Path.Combine(to, file));
                    copied += CopyIfChanged(Path.Combine(from, LowFolder, file), Path.Combine(to, LowFolder, file));
                }

                foreach (string map in lowOnly ?? new string[0])
                {
                    string file = $"{asset}_{map}.png";
                    copied += CopyIfChanged(Path.Combine(from, LowFolder, file), Path.Combine(to, LowFolder, file));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (copied > 0)
            {
                AssetDatabase.Refresh();
            }

            return copied;
        }

        private static string TargetPath => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, TargetRoot);

        private static int SyncAsset(string source, string asset)
        {
            string from = Path.Combine(source, asset);
            if (!Directory.Exists(from))
            {
                Debug.LogWarning($"Blender sync: no export for '{asset}' in {SourceRelative}.");
                return 0;
            }

            // An asset in a sub folder (Env/ResourceNode) names its files after the last part.
            string name = Path.GetFileName(asset);
            string to = Path.Combine(TargetPath, asset);
            int copied = 0;
            copied += CopyIfChanged(Path.Combine(from, name + ".fbx"), Path.Combine(to, name + ".fbx"));
            copied += CopyIfChanged(Path.Combine(from, name + "_LOD1.fbx"), Path.Combine(to, name + "_LOD1.fbx"));
            copied += CopyIfChanged(Path.Combine(from, name + "_report.json"), Path.Combine(to, name + "_report.json"));
            copied += SyncMaps(from, to, name);

            // A second material on the same mesh (a boss's crystals or ice) has its own maps: <Asset>_<Material>_Albedo.png.
            foreach (string albedo in Directory.GetFiles(from, $"{name}_*{MapSuffixes[0]}"))
            {
                string prefix = Path.GetFileName(albedo);
                prefix = prefix.Substring(0, prefix.Length - MapSuffixes[0].Length);
                if (prefix != name)
                {
                    copied += SyncMaps(from, to, prefix);
                }
            }

            // Extra skins baked on the same mesh (the element bats): one folder of maps per skin.
            string skins = Path.Combine(from, SkinFolder);
            if (Directory.Exists(skins))
            {
                foreach (string skin in Directory.GetDirectories(skins))
                {
                    string skinName = Path.GetFileName(skin);
                    copied += SyncMaps(skin, Path.Combine(to, SkinFolder, skinName), $"{name}_{skinName}");
                }
            }

            return copied;
        }

        private static int SyncModels(string source, string asset, string[] models)
        {
            int copied = 0;
            foreach (string model in models)
            {
                copied += CopyIfChanged(Path.Combine(source, asset, model), Path.Combine(TargetPath, asset, model));
            }

            return copied;
        }

        private static int SyncMaps(string from, string to, string prefix)
        {
            int copied = 0;
            foreach (string suffix in MapSuffixes)
            {
                copied += CopyIfChanged(Path.Combine(from, prefix + suffix), Path.Combine(to, prefix + suffix));
                copied += CopyIfChanged(Path.Combine(from, LowFolder, prefix + suffix), Path.Combine(to, LowFolder, prefix + suffix));
            }

            return copied;
        }

        private static int SyncFolder(string from, string to, string pattern)
        {
            if (!Directory.Exists(from))
            {
                return 0;
            }

            int copied = 0;
            foreach (string file in Directory.GetFiles(from, pattern))
            {
                copied += CopyIfChanged(file, Path.Combine(to, Path.GetFileName(file)));
            }

            return copied;
        }

        private static int CopyIfChanged(string from, string to)
        {
            if (!File.Exists(from))
            {
                return 0;
            }

            var source = new FileInfo(from);
            var target = new FileInfo(to);
            if (target.Exists && target.Length == source.Length && target.LastWriteTimeUtc == source.LastWriteTimeUtc)
            {
                return 0;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(to) ?? string.Empty);
            File.Copy(from, to, true);
            File.SetLastWriteTimeUtc(to, source.LastWriteTimeUtc);
            return 1;
        }
    }
}
