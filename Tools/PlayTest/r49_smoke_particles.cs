// Round 49 smoke material fix: lists the live particles of every system named in Tools/PlayTest/r49_smoke_name.txt
// ("<parent prefix> <system name>"): position, size, colour, plus the renderer's layer, visibility and sorting.
var spec = System.IO.File.ReadAllText("Tools/PlayTest/r49_smoke_name.txt").Trim().Split(' ');
var report = new System.Text.StringBuilder();
var buffer = new UnityEngine.ParticleSystem.Particle[64];
foreach (var system in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>(UnityEngine.FindObjectsSortMode.None))
{
    var parent = system.transform.parent;
    if (system.name != spec[1] || parent == null || !parent.name.StartsWith(spec[0])) { continue; }
    var renderer = system.GetComponent<UnityEngine.ParticleSystemRenderer>();
    report.Append(parent.name + "/" + system.name + " at " + system.transform.position.ToString("0.00") + " layer " + system.gameObject.layer
        + " visible " + renderer.isVisible + " enabled " + renderer.enabled + " mat " + (renderer.sharedMaterial != null ? renderer.sharedMaterial.name + " q" + renderer.sharedMaterial.renderQueue : "null")
        + " sheet " + system.textureSheetAnimation.enabled + " space " + system.main.simulationSpace + ":");
    int count = system.GetParticles(buffer);
    for (int i = 0; i < count; i++)
    {
        var p = buffer[i];
        report.Append(" [" + p.position.ToString("0.00") + " s" + p.GetCurrentSize(system).ToString("0.00") + " c" + p.GetCurrentColor(system) + "]");
    }

    report.Append(" | ");
}

return report.Length > 0 ? report.ToString() : "none";
