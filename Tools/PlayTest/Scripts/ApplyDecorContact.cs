using System.Reflection;
using UnityEditor;

/// <summary>
/// Round 13b: syncs Blender round 38 (the decor remains) with the environment exports, rebuilds every environment set (each
/// mesh now carries its boss contact: Breakable, Obstacle, Climbable) and its remains, points the themes at them again, and
/// refills the sound library (the new decor smash set). No creature, tower or level data is touched.
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyDecorContact.cs --entry ApplyDecorContact.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyDecorContact
{
    public static string Run()
    {
        Game.Editor.BlenderEnvironmentBuilder.BuildMenu();
        typeof(Game.Editor.ProjectGenerator).GetMethod("CreateSfxLibrary", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        AssetDatabase.SaveAssets();
        string report = string.Empty;
        foreach (string guid in AssetDatabase.FindAssets("t:EnvironmentSet"))
        {
            var set = AssetDatabase.LoadAssetAtPath<Game.Levels.EnvironmentSet>(AssetDatabase.GUIDToAssetPath(guid));
            int breakable = 0, obstacle = 0, climbable = 0;
            foreach (Game.Levels.PropKind kind in System.Enum.GetValues(typeof(Game.Levels.PropKind)))
            {
                Game.Levels.EnvironmentDecor decor = set.Decor(kind);
                if (decor == null)
                {
                    continue;
                }

                switch (decor.Variants[0].Contact)
                {
                    case Game.Levels.DecorContact.Breakable: breakable++; break;
                    case Game.Levels.DecorContact.Obstacle: obstacle++; break;
                    case Game.Levels.DecorContact.Climbable: climbable++; break;
                }
            }

            report += $"{set.name}: breakable {breakable} obstacle {obstacle} climbable {climbable} remains {set.Remains(Game.Levels.DecorRemains.Crystal) != null}/{set.Remains(Game.Levels.DecorRemains.Flora) != null}/{set.Remains(Game.Levels.DecorRemains.Stone) != null}; ";
        }

        return report;
    }
}
