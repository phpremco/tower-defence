// Round 50: for every living boss, where its beam starts against the bone its art names (the beam socket), the body and the
// logic's old fixed point; and every line and trail renderer in the scene with its material and shader (flags any on the
// URP default particle material or none).
var sb = new System.Text.StringBuilder();
var defaultParticle = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    var abilities = enemy.IsAlive ? enemy.GetComponent<Game.Enemies.BossAbilities>() : null;
    if (abilities == null) { continue; }
    var view = enemy.GetComponent<Game.Enemies.EnemyView>();
    var socket = view != null ? view.FindBeamSocket() : null;
    sb.Append(enemy.Definition.name).Append(" laser=").Append(abilities.LaserActive);
    sb.Append(" socket=").Append(socket != null ? socket.name + socket.position.ToString("0.00") : "none");
    sb.Append(" body=").Append(enemy.transform.position.ToString("0.00"));
    sb.Append(" oldOrigin=").Append(abilities.LaserOrigin.ToString("0.00"));
    foreach (var line in enemy.transform.parent.GetComponentsInChildren<UnityEngine.LineRenderer>(false))
    {
        if (line.name != "BossBeam" || !line.enabled) { continue; }
        var start = line.GetPosition(0);
        sb.Append(" beamStart=").Append(start.ToString("0.00"));
        if (socket != null) { sb.Append(" start-socket=").Append((start - socket.position).magnitude.ToString("0.000")); }
        sb.Append(" oldOrigin-socket=").Append(socket != null ? (abilities.LaserOrigin - socket.position).magnitude.ToString("0.00") : "-");
        sb.Append(" end=").Append(line.GetPosition(1).ToString("0.00")).Append(" laserEnd=").Append(abilities.LaserEnd.ToString("0.00"));
    }

    sb.Append(" | ");
}

int lines = 0, bad = 0;
var shaders = new System.Collections.Generic.Dictionary<string, int>();
foreach (var renderer in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (!(renderer is UnityEngine.LineRenderer) && !(renderer is UnityEngine.TrailRenderer)) { continue; }
    lines++;
    var m = renderer.sharedMaterial;
    string key = m == null ? "NONE" : m == defaultParticle ? "DEFAULT" : m.shader.name + "/" + m.name;
    if (m == null || m == defaultParticle) { bad++; sb.Append(renderer.GetType().Name + " " + renderer.name + " " + key + "; "); }
    shaders[key] = shaders.TryGetValue(key, out int c) ? c + 1 : 1;
}

sb.Append(" lines+trails=").Append(lines).Append(" default/none=").Append(bad).Append(" :: ");
foreach (var kv in shaders) { sb.Append(kv.Key).Append(" x").Append(kv.Value).Append("; "); }
return sb.ToString();
