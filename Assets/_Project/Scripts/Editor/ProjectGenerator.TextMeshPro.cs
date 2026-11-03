using System.IO;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for TextMeshPro: imports the Essential Resources (default font, settings, shaders) that ship
    /// inside the installed com.unity.ugui package, without the import dialog, so the dialogue box has its text
    /// assets on a fresh clone. Does nothing once they are there.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string UguiPackage = "Packages/com.unity.ugui";
        private const string TmpEssentialsPackage = "Package Resources/TMP Essential Resources.unitypackage";

        private static void EnsureTextMeshProEssentials()
        {
            if (File.Exists(TmpSettingsPath))
            {
                return;
            }

            UnityEditor.PackageManager.PackageInfo info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(UguiPackage);
            string package = info != null ? Path.Combine(info.resolvedPath, TmpEssentialsPackage) : null;
            if (package == null || !File.Exists(package))
            {
                Debug.LogError($"TextMeshPro essentials not found in {UguiPackage}; the dialogue box needs them.");
                return;
            }

            UnityEditor.AssetPackage.Package.Import(package, false);
        }
    }
}
