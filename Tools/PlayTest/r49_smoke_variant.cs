// Round 49 smoke material fix (tuning aid): sets every damage-smoke system named "Smoke" under the probe tower and the hero
// to the colour and size factor in Tools/PlayTest/r49_smoke_variant.txt ("r g b a sizeFactor"), so variants can be compared
// in one session. The change lives only in this Play Mode session.
var v = System.IO.File.ReadAllText("Tools/PlayTest/r49_smoke_variant.txt").Trim().Split(' ');
System.Func<int, float> f = i => float.Parse(v[i], System.Globalization.CultureInfo.InvariantCulture);
int changed = 0;
foreach (var system in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>(UnityEngine.FindObjectsSortMode.None))
{
    var parent = system.transform.parent;
    if (system.name != "Smoke" || parent == null || !(parent.name.StartsWith("ProbeTower") || parent.name == "Spine2")) { continue; }
    var main = system.main;
    main.startColor = new UnityEngine.Color(f(0), f(1), f(2), f(3));
    main.startSize = new UnityEngine.ParticleSystem.MinMaxCurve(0.2f * f(4), 0.45f * f(4));
    system.Clear();
    changed++;
}

return "variant on " + changed;
