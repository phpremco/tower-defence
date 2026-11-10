using System.Reflection;
using UnityEditor;

/// <summary>
/// Runs only the generator's "Sound library" step (every SoundSet filled from the approved recordings, gains measured,
/// creature voices) and saves, for a sound change while the full generator must not run. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplySounds.cs --entry ApplySounds.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class ApplySounds
{
    public static string Run()
    {
        typeof(Game.Editor.ProjectGenerator).GetMethod("CreateSfxLibrary", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        AssetDatabase.SaveAssets();
        var library = AssetDatabase.LoadAssetAtPath<Game.Core.SfxLibrary>("Assets/_Project/Data/Audio/SfxLibrary.asset");
        return library != null ? "sound library rebuilt, distant blasts " + library.DistantBlasts.Count : "sound library rebuilt";
    }
}
