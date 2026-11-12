// Round 49: lists every vent emitter on the field (the Cragback's crater smoke and embers): whose body it rides on (a living
// enemy or a corpse), its emission rate, live particle count and budget, and its world height over the ground.
var sb = new System.Text.StringBuilder();
foreach (var system in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>(UnityEngine.FindObjectsSortMode.None))
{
    if (!system.name.StartsWith("Vent")) { continue; }
    var owner = system.transform;
    while (owner.parent != null && !owner.name.EndsWith("_corpse") && owner.GetComponent<Game.Enemies.Enemy>() == null) { owner = owner.parent; }
    var emission = system.emission;
    sb.Append(owner.name + "/" + system.transform.parent.name + "/" + system.name + " rate " + (emission.enabled ? emission.rateOverTime.constant : 0f).ToString("0.00")
        + " n " + system.particleCount + "/" + system.main.maxParticles + " y " + system.transform.position.y.ToString("0.00"));
    var particles = new UnityEngine.ParticleSystem.Particle[1];
    if (system.GetParticles(particles) > 0)
    {
        var r = system.GetComponent<UnityEngine.ParticleSystemRenderer>();
        sb.Append(" first at " + particles[0].position.ToString("0.00") + " size " + particles[0].GetCurrentSize(system).ToString("0.00") + " colour " + particles[0].GetCurrentColor(system) + " mat " + (r.sharedMaterial != null ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "null") + " visible " + r.isVisible + " scaling " + system.main.scalingMode);
    }

    sb.Append("; ");
}

return sb.Length > 0 ? sb.ToString() : "no vents";
