// Round 40: Gravemaw's reworked Roar, in edit mode (a preview scene). How far its last frame is from the crawl (Move) it
// blends into, bone by bone (largest turn and move), how high the maw points at the held roar (Head bone's forward over the
// horizontal), and the clip's events and lengths as imported.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/Gravemaw.asset");
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var sb = new System.Text.StringBuilder();
try
{
    var holder = new UnityEngine.GameObject("Holder");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, scene);
    var model = Game.Core.ArtModel.CreateSampled(holder.transform, art, false);
    var nodes = model.Root.GetComponentsInChildren<UnityEngine.Transform>(true);
    float roar = model.LengthOf(Game.Core.ModelClipRole.Roar);
    foreach (var clip in art.Clips) if (clip.Role == Game.Core.ModelClipRole.Roar || clip.Role == Game.Core.ModelClipRole.SurfaceThrow || clip.Role == Game.Core.ModelClipRole.Move) sb.Append(clip.Role).Append(' ').Append(clip.Length.ToString("0.000")).Append("s ev ").Append(clip.EventSeconds.ToString("0.000")).Append(" cue ").Append(clip.CueSeconds.ToString("0.000")).Append(" anchor ").Append(clip.AnchorForward.ToString("0.000")).Append(" end ").Append(clip.EndOffset.ToString("F3")).Append('\n');
    model.Sample(Game.Core.ModelClipRole.Roar, roar, false);
    var rot = new UnityEngine.Quaternion[nodes.Length]; var pos = new UnityEngine.Vector3[nodes.Length];
    for (int i = 0; i < nodes.Length; i++) { rot[i] = nodes[i].localRotation; pos[i] = nodes[i].localPosition; }
    model.Sample(Game.Core.ModelClipRole.Move, 0f, true);
    float maxAngle = 0f, maxMove = 0f; string worstA = "", worstM = "";
    for (int i = 0; i < nodes.Length; i++)
    {
        float a = UnityEngine.Quaternion.Angle(rot[i], nodes[i].localRotation);
        float m = (pos[i] - nodes[i].localPosition).magnitude;
        if (a > maxAngle) { maxAngle = a; worstA = nodes[i].name; }
        if (m > maxMove) { maxMove = m; worstM = nodes[i].name; }
    }
    sb.Append("Roar last frame vs Move frame 0: largest bone turn ").Append(maxAngle.ToString("0.0")).Append(" deg (").Append(worstA).Append("), largest bone move ").Append(maxMove.ToString("0.000")).Append(" (").Append(worstM).Append(")\n");
    UnityEngine.Transform head = model.Find("Head");
    foreach (int frame in new[] { 0, 12, 22, 30, 40, 49, 59, 66 })
    {
        model.Sample(Game.Core.ModelClipRole.Roar, frame / 30f, false);
        var f = head != null ? head.forward : UnityEngine.Vector3.zero;
        var u = head != null ? head.up : UnityEngine.Vector3.zero;
        float pitchF = UnityEngine.Mathf.Asin(UnityEngine.Mathf.Clamp(f.y, -1f, 1f)) * UnityEngine.Mathf.Rad2Deg;
        float pitchU = UnityEngine.Mathf.Asin(UnityEngine.Mathf.Clamp(u.y, -1f, 1f)) * UnityEngine.Mathf.Rad2Deg;
        sb.Append("roar f").Append(frame).Append(" head y ").Append(head != null ? head.position.y.ToString("0.00") : "-").Append(" fwd pitch ").Append(pitchF.ToString("0")).Append(" up pitch ").Append(pitchU.ToString("0")).Append('\n');
    }
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
}
return sb.ToString();
