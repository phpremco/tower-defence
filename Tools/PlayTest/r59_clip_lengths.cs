// Round 59: lengths of the launch sound files (seconds), to size the one-shot voices for the salvo.
var sb = new System.Text.StringBuilder();
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio/Approved/Intro/Launch", "Assets/_Project/Audio/Approved/Intro/Orbit" }))
{
    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    sb.Append(clip.name).Append(' ').Append(clip.length.ToString("0.00")).Append("; ");
}
return sb.ToString();
