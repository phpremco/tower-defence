// Round 47: per art, how many clips loop and which do not (those get an echo slot in ClipMixer), and the slot total.
var sb = new System.Text.StringBuilder();
int clips = 0, echoes = 0;
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ModelArtDefinition"))
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (art == null || art.Clips.Length == 0) continue;
    var once = new System.Collections.Generic.List<string>();
    foreach (var clip in art.Clips) { clips++; if (clip.Clip != null && !clip.Clip.isLooping) { echoes++; once.Add(clip.Role.ToString()); } }
    sb.AppendLine(art.name + ": " + art.Clips.Length + " clips, " + once.Count + " echo (" + string.Join(" ", once) + ")");
}

System.IO.File.WriteAllText("Logs/r47/clip_loops.txt", sb.ToString());
return clips + " clips, " + echoes + " echo slots";
