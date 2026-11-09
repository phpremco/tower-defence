using System.Reflection;
using Game.UI;
using UnityEditor;

/// <summary>
/// Runs only the font part of the generator's "UI skin" step (Chakra Petch display and narrow faces on UiSkin.asset) and
/// saves, without the Blender UI sync the full step does. Same code as Tools/Generate/Scenes and Starter Data. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplySkinFonts.cs --entry ApplySkinFonts.Run
/// </summary>
public static class ApplySkinFonts
{
    public static string Run()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var skin = AssetDatabase.LoadAssetAtPath<UiSkin>("Assets/_Project/Settings/UiSkin.asset");
        var so = new SerializedObject(skin);
        typeof(Game.Editor.ProjectGenerator).GetMethod("AssignSkinFonts", flags).Invoke(null, new object[] { so });
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();
        return $"font {(skin.Font != null ? skin.Font.name : "null")}, narrow {(skin.NarrowFont != null ? skin.NarrowFont.name : "null")}";
    }
}
