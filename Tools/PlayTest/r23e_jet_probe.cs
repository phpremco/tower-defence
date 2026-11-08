// Round 23e: the jet sockets' exhaust axis (+Y) in the world, the flame cards' up axis, and the hero's facing.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var sb = new System.Text.StringBuilder();
sb.Append("hero fwd ").Append(context.Hero.transform.forward.ToString("0.00")).Append(" ");
foreach (var t in context.Hero.GetComponentsInChildren<UnityEngine.Transform>(true))
{
    if (t.name == "Jet_L" || t.name == "Jet_R") sb.Append(t.name).Append(" up ").Append(t.up.ToString("0.00")).Append(" at ").Append(t.position.ToString("0.00")).Append("; ");
}
foreach (var r in context.Hero.GetComponentsInChildren<UnityEngine.Renderer>(false))
{
    string n = r.name.ToLowerInvariant();
    if (n.Contains("flame") || n.Contains("exhaust") || n.Contains("jetfire")) sb.Append(r.name).Append(" c ").Append(r.bounds.center.ToString("0.00")).Append(" up ").Append(r.transform.up.ToString("0.00")).Append("; ");
}
return sb.ToString();
