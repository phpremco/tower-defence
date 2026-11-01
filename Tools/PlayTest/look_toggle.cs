// Switches one piece of the look off or on, named in look_arg.txt: "mist" (the drifting sheets), "detail" (the
// tiling ground detail layer), "fog" (distance fog), "post" (the post-processing volume), "rim" (the back light).
// Used to tell which layer darkens or washes the picture.
string what = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
string report = "unknown " + what;
if (what == "mist")
{
    int n = 0;
    foreach (var mist in UnityEngine.Object.FindObjectsByType<Game.Levels.GroundMist>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    {
        mist.gameObject.SetActive(!mist.gameObject.activeSelf);
        n++;
    }
    report = "toggled " + n + " mist sheets";
}
else if (what == "detail")
{
    var terrain = UnityEngine.GameObject.Find("Terrain");
    var material = terrain.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial;
    bool on = material.IsKeywordEnabled("_DETAIL_MULX2");
    if (on) material.DisableKeyword("_DETAIL_MULX2"); else material.EnableKeyword("_DETAIL_MULX2");
    report = "detail now " + !on;
}
else if (what == "fog")
{
    UnityEngine.RenderSettings.fog = !UnityEngine.RenderSettings.fog;
    report = "fog now " + UnityEngine.RenderSettings.fog;
}
else if (what == "post")
{
    var volume = UnityEngine.GameObject.Find("PostProcessing").GetComponent<UnityEngine.Rendering.Volume>();
    volume.enabled = !volume.enabled;
    report = "post now " + volume.enabled;
}
else if (what == "rim")
{
    var rim = UnityEngine.GameObject.Find("RimLight").GetComponent<UnityEngine.Light>();
    rim.enabled = !rim.enabled;
    report = "rim now " + rim.enabled;
}
return report;
