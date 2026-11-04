using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// One-off (2026-11-04 cleanup): rewrites the tower, enemy and level/theme assets so the removed model fields (Kenney
/// SpaceKit fallbacks) leave the files. Run: unity command run_script --file Tools/PlayTest/Scripts/ReserializeCleanup.cs
/// --entry ReserializeCleanup.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class ReserializeCleanup
{
    public static string Run()
    {
        var paths = new List<string>();
        foreach (string folder in new[] { "Assets/_Project/Data/Towers", "Assets/_Project/Data/Enemies", "Assets/_Project/Data/Levels" })
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        AssetDatabase.ForceReserializeAssets(paths, ForceReserializeAssetsOptions.ReserializeAssets);
        return "rewrote " + paths.Count;
    }
}
