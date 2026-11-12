// Round 54: lists the instanced decoration standing near the RuinGate (within 4 m of its pivot, in the gate's frame:
// x across the opening, z out of it), by mesh, and whether each piece is breakable (what BossField.BreakDecor smashes).
// Reads EnvironmentInstancer's private batches by reflection (a probe, not game code).
var gate = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.RuinGateView>();
if (gate == null) { return "no RuinGate"; }
var instancer = UnityEngine.Object.FindAnyObjectByType<Game.Levels.EnvironmentInstancer>();
if (instancer == null) { return "no instancer"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var type = typeof(Game.Levels.EnvironmentInstancer);
var batches = (System.Collections.IList)type.GetField("_batches", flags).GetValue(instancer);
var breakable = (System.Collections.IList)type.GetField("_breakable", flags).GetValue(instancer);
var brittle = new System.Collections.Generic.HashSet<UnityEngine.Vector3>();
foreach (var entry in breakable)
{
    var m = (UnityEngine.Matrix4x4)entry.GetType().GetField("Item2").GetValue(entry);
    brittle.Add(m.GetPosition());
}
var sb = new System.Text.StringBuilder("gate " + gate.transform.position.ToString("0.00") + " scale " + gate.transform.localScale.x.ToString("0.00") + ":");
foreach (var batch in batches)
{
    var bt = batch.GetType();
    var mesh = (UnityEngine.Mesh)bt.GetField("Mesh").GetValue(batch);
    var all = (UnityEngine.Matrix4x4[])bt.GetField("Matrices").GetValue(batch);
    if (all == null) { continue; }
    foreach (var m in all)
    {
        var p = m.GetPosition();
        var local = gate.transform.InverseTransformPoint(p) * gate.transform.localScale.x;
        if (UnityEngine.Mathf.Abs(local.x) > 4f || local.z > 3f || local.z < -2.5f) { continue; }
        sb.Append(" | ").Append(mesh != null ? mesh.name : "?").Append(" x").Append(local.x.ToString("0.0")).Append(" z").Append(local.z.ToString("0.0"));
        sb.Append(" s").Append(m.lossyScale.x.ToString("0.0")).Append(brittle.Contains(p) ? " breakable" : " solid");
    }
}
return sb.ToString();
