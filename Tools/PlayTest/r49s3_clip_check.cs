// Round 49 import (set 3b): for the set-3 rows and their corrupted rows, every clip the catalog declares must be in the art
// with its role and a playable AnimationClip of that name; lists what is missing.
var rows = new[] { "Prowler", "PackAlpha", "Skyray", "RayBoss", "SlimeMonarch", "CinderMatriarch", "Knitback" };
var sb = new System.Text.StringBuilder();
foreach (var entry in Game.Editor.BlenderArtCatalog.Creatures)
{
    string baseId = entry.Id.Replace(Game.Editor.BlenderArtCatalog.CorruptedSuffix, "");
    if (System.Array.IndexOf(rows, baseId) < 0) continue;
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + entry.Id + ".asset");
    foreach (var spec in entry.Clips)
    {
        bool found = false;
        foreach (var clip in art.Clips) { if (clip.Role == spec.Role && clip.Clip != null && clip.Clip.name == spec.Clip) found = true; }
        if (!found) sb.Append(entry.Id + " misses " + spec.Clip + "/" + spec.Role + "; ");
    }
}

return sb.Length == 0 ? "all declared clips present" : sb.ToString();
