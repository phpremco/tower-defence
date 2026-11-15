// Round 63: the base shuttle's round in a level. Reads Tools/PlayTest/r63_shuttle_arg.txt: "<seconds since the end of the
// rest>" (negative: still resting), puts the shuttle's clock there, steps one frame and reports where its meshes are, their
// scale, whether they are shown and seen by the play camera, and its engine and wash state.
UnityEditor.EditorApplication.isPaused = true;
var inv = System.Globalization.CultureInfo.InvariantCulture;
var view = UnityEngine.Object.FindAnyObjectByType<Game.Levels.BaseShuttleView>();
if (view == null) return "no shuttle";
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var art = (Game.Levels.BaseArtDefinition)typeof(Game.Levels.BaseShuttleView).GetField("_art", flags).GetValue(view);
float at = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r63_shuttle_arg.txt").Trim(), inv);
float clock = art.ShuttleRestSeconds + at;
typeof(Game.Levels.BaseShuttleView).GetField("_clock", flags).SetValue(view, clock - UnityEngine.Time.deltaTime);
UnityEditor.EditorApplication.Step();
var parts = (UnityEngine.Transform[])typeof(Game.Levels.BaseShuttleView).GetField("_parts", flags).GetValue(view);
var sb = new System.Text.StringBuilder("t=" + at.ToString("0.00", inv));
foreach (var p in parts)
{
    var r = p.GetComponent<UnityEngine.Renderer>();
    sb.Append(" | ").Append(p.name).Append(" active ").Append(p.gameObject.activeInHierarchy).Append(" pos ").Append(p.position.ToString("0.00")).Append(" scale ").Append(p.localScale.x.ToString("0.000", inv));
    if (r != null) sb.Append(" visible ").Append(r.isVisible).Append(" enabled ").Append(r.enabled);
}
// Its size on screen (the play camera, pixels of the 1920-wide view): the larger side of its bounds projected.
var cam = UnityEngine.Camera.main;
var rend = parts[0].GetComponent<UnityEngine.Renderer>();
if (cam != null && rend != null && parts[0].gameObject.activeInHierarchy)
{
    var bb = rend.bounds; float minX = 1e9f, maxX = -1e9f, minY = 1e9f, maxY = -1e9f;
    for (int c = 0; c < 8; c++)
    {
        var corner = bb.center + UnityEngine.Vector3.Scale(bb.extents, new UnityEngine.Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
        var sp = cam.WorldToViewportPoint(corner);
        minX = UnityEngine.Mathf.Min(minX, sp.x); maxX = UnityEngine.Mathf.Max(maxX, sp.x); minY = UnityEngine.Mathf.Min(minY, sp.y); maxY = UnityEngine.Mathf.Max(maxY, sp.y);
    }
    sb.Append(" | screen ").Append(((maxX - minX) * 1920f).ToString("0.0", inv)).Append("x").Append(((maxY - minY) * 1080f).ToString("0.0", inv)).Append(" px at (").Append(((minX + maxX) * 0.5f).ToString("0.00", inv)).Append(",").Append(((minY + maxY) * 0.5f).ToString("0.00", inv)).Append(")");
}
var lod = parts[0].GetComponentInParent<UnityEngine.LODGroup>();
if (lod != null) sb.Append(" | lodgroup ").Append(lod.name);
return sb.ToString();
