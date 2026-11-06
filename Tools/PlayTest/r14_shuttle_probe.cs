// Round 14: the shuttle's parts (active, world position, scale) and its clock.
var shuttle = UnityEngine.Object.FindAnyObjectByType<Game.Levels.BaseShuttleView>();
if (shuttle == null) { return "no shuttle"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var parts = (UnityEngine.Transform[])typeof(Game.Levels.BaseShuttleView).GetField("_parts", flags).GetValue(shuttle);
var up = (UnityEngine.Vector3)typeof(Game.Levels.BaseShuttleView).GetField("_up", flags).GetValue(shuttle);
var nose = (UnityEngine.Vector3)typeof(Game.Levels.BaseShuttleView).GetField("_nose", flags).GetValue(shuttle);
var sb = new System.Text.StringBuilder();
sb.Append("clock ").Append(((float)typeof(Game.Levels.BaseShuttleView).GetField("_clock", flags).GetValue(shuttle)).ToString("0.00")).Append(" up ").Append(up.ToString("0.00")).Append(" nose ").Append(nose.ToString("0.00"));
foreach (var p in parts) { sb.Append(" | ").Append(p.name).Append(p.gameObject.activeInHierarchy ? " on " : " off ").Append(p.position.ToString("0.00")).Append(" s ").Append(p.localScale.x.ToString("0.00")); }
return sb.ToString();
