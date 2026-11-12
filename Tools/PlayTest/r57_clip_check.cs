// Round 57 (copy of r52_clip_check.cs for the round-57 rows): for the imported rows and their corrupted rows, every clip
// the catalog declares must be in the art with its role and a playable AnimationClip of that name, its moment on the
// declared frame (30 fps) and its power trigger mapped; the Shattered keeps all 15 clips. Also compares each stored bar
// height with the cycle-aware measure (CreatureBarHeight). Lists what is wrong, then the heights.
var rows = new[] { "SealedOne", "Skyray", "RayBoss", "Bulwark", "CrystalWarlord", "PackAlpha", "FireLarva", "Bat_Mire" };
const float Fps = 30f;
const int ShatteredClips = 15;
var problems = new System.Text.StringBuilder();
var heights = new System.Text.StringBuilder();
foreach (var entry in Game.Editor.BlenderArtCatalog.Creatures)
{
    string baseId = entry.Id.Replace(Game.Editor.BlenderArtCatalog.CorruptedSuffix, "");
    if (System.Array.IndexOf(rows, baseId) < 0) continue;
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + entry.Id + ".asset");
    foreach (var spec in entry.Clips)
    {
        Game.Core.ModelClip found = null;
        foreach (var clip in art.Clips) { if (clip.Role == spec.Role && clip.Clip != null && clip.Clip.name == spec.Clip) found = clip; }
        if (found == null) { problems.Append(entry.Id + " misses " + spec.Clip + "/" + spec.Role + "; "); continue; }
        if (spec.EventFrame > 0 && UnityEngine.Mathf.Abs(found.EventSeconds - spec.EventFrame / Fps) > 0.002f) problems.Append(entry.Id + " " + spec.Clip + " event " + found.EventSeconds + "; ");
        if (spec.Role == Game.Core.ModelClipRole.Power && found.Trigger != spec.Trigger) problems.Append(entry.Id + " " + spec.Clip + " trigger '" + found.Trigger + "'; ");
    }

    if (baseId == "Shattered" && art.Clips.Length != ShatteredClips) problems.Append("Shattered has " + art.Clips.Length + " clips; ");
    float measured = Game.Editor.CreatureBarHeight.Measure(art.Model, art);
    heights.Append(entry.Id + " " + art.Height.ToString("0.000") + " measure " + measured.ToString("0.000") + "; ");
}

return (problems.Length == 0 ? "all declared clips present, events and triggers mapped. " : problems.ToString()) + heights;
