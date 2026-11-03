// Imports the TextMeshPro Essential Resources from the installed com.unity.ugui package, without the import dialog.
var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
if (info == null)
{
    return "ugui package not found";
}

string package = System.IO.Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
if (!System.IO.File.Exists(package))
{
    return "missing " + package;
}

if (System.IO.File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
{
    return "already imported";
}

UnityEditor.AssetPackage.Package.Import(package, false);
return "import started: " + package;
