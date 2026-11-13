// Round 61: what the shockwave view is showing right now: per particle system its live count, renderer and material, per
// card whether it is on, its scale and intensity. For checking that every part of the effect really plays.
var view = UnityEngine.Object.FindAnyObjectByType<Game.Combat.ShockwaveView>(UnityEngine.FindObjectsInactive.Include);
if (view == null) { return "no shockwave view"; }
var sb = new System.Text.StringBuilder("playing " + view.IsPlaying + " at " + view.transform.position.ToString("0.0") + ": ");
foreach (var system in view.GetComponentsInChildren<UnityEngine.ParticleSystem>(true))
{
    var renderer = system.GetComponent<UnityEngine.ParticleSystemRenderer>();
    sb.Append(system.name).Append(' ').Append(system.particleCount).Append(renderer.enabled ? " on " : " off ").Append(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "no material").Append("; ");
}

foreach (var card in view.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
{
    sb.Append(card.name).Append(card.gameObject.activeSelf ? " on " : " off ").Append(card.transform.localScale.x.ToString("0.00")).Append("; ");
}

return sb.ToString();
