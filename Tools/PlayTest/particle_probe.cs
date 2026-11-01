// Lists every particle system whose name contains zoom_arg.txt: playing state, live count, emission rate and the
// position of its first particle, to check that a runtime-built effect exists and emits where expected.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/zoom_arg.txt").Trim();
var sb = new System.Text.StringBuilder();
var buffer = new UnityEngine.ParticleSystem.Particle[1];
foreach (var ps in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>(UnityEngine.FindObjectsSortMode.None))
{
    if (!ps.name.Contains(wanted)) continue;
    int got = ps.GetParticles(buffer, 1);
    string first = got > 0 ? buffer[0].position.ToString("0.0") + " size " + buffer[0].GetCurrentSize(ps).ToString("0.00") : "-";
    var r = ps.GetComponent<UnityEngine.ParticleSystemRenderer>();
    sb.Append(ps.name + ": playing=" + ps.isPlaying + " count=" + ps.particleCount + " rate=" + ps.emission.rateOverTime.constant.ToString("0.0") + " at " + ps.transform.position.ToString("0.0") + " first " + first + " mat=" + (r.sharedMaterial ? r.sharedMaterial.shader.name : "none") + " mode=" + r.renderMode + "; ");
}
return sb.Length == 0 ? "none named " + wanted : sb.ToString();
