// Round 45: holds the first living enemy named in Tools/PlayTest/r45_pose.txt ("<Name> <ClipRole>") in that clip's pose
// (at 30% of the clip) for the stills: its rigs' Animators are switched off and the clip sampled onto each LOD, so the
// next stepped frame skins that pose (a roller curled in its ball hides its face). "<Name> off" switches them back on.
var spec = System.IO.File.ReadAllText("Tools/PlayTest/r45_pose.txt").Trim().Split(' ');
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != spec[0]) { continue; }
    var animators = enemy.GetComponentsInChildren<UnityEngine.Animator>(true);
    if (spec[1] == "off")
    {
        foreach (var animator in animators) { animator.enabled = true; }
        return "animators on " + animators.Length;
    }

    var art = enemy.Definition.Art;
    int index = art.IndexOf((Game.Core.ModelClipRole)System.Enum.Parse(typeof(Game.Core.ModelClipRole), spec[1]));
    if (index < 0) { return "no " + spec[1] + " clip"; }
    var clip = art.Clips[index].Clip;
    foreach (var animator in animators)
    {
        animator.enabled = false;
        clip.SampleAnimation(animator.gameObject, clip.length * 0.3f);
    }

    return "posed " + spec[0] + " in " + clip.name + " on " + animators.Length + " rig(s)";
}

return "no living " + spec[0];
