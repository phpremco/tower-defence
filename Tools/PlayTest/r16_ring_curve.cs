// Round 16: prints Ring_1..3 of the Sealed One through its Walk clip: per 1/30 s the turn from the first frame and the
// axis of that turn in the ring's parent space, to see whether a ring spins about one axis and where it snaps.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/SealedOne.asset");
string role = System.IO.File.ReadAllText("Tools/PlayTest/r16_arg.txt").Trim();
var go = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(art.Model);
go.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
string result = "";
foreach (var clip in art.Clips)
{
    if (clip.Role.ToString() != role) { continue; }
    var anim = clip.Clip;
    int steps = UnityEngine.Mathf.CeilToInt(anim.length * 30f);
    result += anim.name + " " + anim.length.ToString("0.000") + " s loop " + anim.isLooping + " fr " + anim.frameRate + "\n";
    foreach (string name in new[] { "Ring_1", "Ring_2", "Ring_3" })
    {
        UnityEngine.Transform bone = null;
        foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>(true)) { if (t.name == name) { bone = t; } }
        anim.SampleAnimation(go, 0f);
        var q0 = bone.localRotation;
        result += name + ":";
        for (int s = 0; s <= steps; s += 1)
        {
            anim.SampleAnimation(go, anim.length * s / steps);
            var d = bone.localRotation * UnityEngine.Quaternion.Inverse(q0);
            d.ToAngleAxis(out float angle, out UnityEngine.Vector3 axis);
            result += " [" + s + "] " + angle.ToString("0") + "@" + axis.ToString("0.00");
        }
        result += "\n";
    }
}
UnityEngine.Object.DestroyImmediate(go);
return result;
