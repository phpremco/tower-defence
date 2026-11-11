// Round 40: the opening's staging geometry. Lists the shots, then seeks the landing (shot 3 at 0.6) and prints, in the
// landing anchor's frame (x right, z towards the base), the capsule, its scale, the exit root, the beacon; then the dig
// site's frame: the Queen's spot and the angle between the dig facing and the brood, the guard post and gate facing.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var sb = new System.Text.StringBuilder();
var q = player.Sequencer;
var shotsField = typeof(Game.Levels.CutsceneSequencer).GetField("_shots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var shots = (System.Collections.Generic.IReadOnlyList<Game.Levels.CutsceneShot>)shotsField.GetValue(q);
for (int i = 0; i < shots.Count; i++) sb.Append(i).Append(':').Append(shots[i].Action).Append(' ');
sb.Append('\n');
player.Seek(3, 0.6f);
UnityEditor.EditorApplication.Step();
player.Seek(3, 0.6f);
var set = player.CurrentSet as Game.UI.FieldSet;
if (set == null) return sb + "no field set";
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var layout = (Game.UI.FieldLayout)typeof(Game.UI.FieldSet).GetField("_layout", flags).GetValue(set);
var capsule = (UnityEngine.Transform)typeof(Game.UI.FieldSet).GetField("_capsule", flags).GetValue(set);
var exitRoot = (UnityEngine.Vector3)typeof(Game.UI.FieldSet).GetField("_exitRoot", flags).GetValue(set);
var stand = (UnityEngine.Vector3)typeof(Game.UI.FieldSet).GetField("_beaconStand", flags).GetValue(set);
var swarm = (Game.UI.FieldSwarm)typeof(Game.UI.FieldSet).GetField("_swarm", flags).GetValue(set);
System.Func<UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Quaternion, string> rel = (p, o, f) => (UnityEngine.Quaternion.Inverse(f) * (p - o)).ToString("F2");
UnityEngine.Vector3 L = layout.Landing; UnityEngine.Quaternion LF = layout.LandingFacing;
sb.Append("landing ").Append(L.ToString("F2")).Append(" capsule ").Append(rel(capsule.position, L, LF)).Append(" capsule yaw rel ").Append(UnityEngine.Mathf.DeltaAngle(LF.eulerAngles.y, capsule.eulerAngles.y).ToString("0")).Append(" scale ").Append(capsule.localScale.x.ToString("0.00")).Append('\n');
sb.Append("exitRoot ").Append(rel(exitRoot, L, LF)).Append(" y-ground ").Append((exitRoot.y - layout.Ground(exitRoot).y).ToString("0.000")).Append(" beacon ").Append(rel(layout.BeaconSpot, L, LF)).Append(" stand ").Append(rel(stand, L, LF)).Append('\n');
var hero = UnityEngine.GameObject.Find("Mercer");
if (hero != null) sb.Append("hero ").Append(rel(hero.transform.position, L, LF)).Append(" yaw rel ").Append(UnityEngine.Mathf.DeltaAngle(LF.eulerAngles.y, hero.transform.eulerAngles.y).ToString("0")).Append('\n');
UnityEngine.Vector3 D = layout.DigSite; UnityEngine.Quaternion DF = layout.DigFacing;
UnityEngine.Vector3 queen = swarm.QueenSpot;
var toQueen = queen - D; toQueen.y = 0;
float broodYaw = UnityEngine.Quaternion.LookRotation(toQueen).eulerAngles.y;
sb.Append("dig ").Append(D.ToString("F2")).Append(" digYaw ").Append(DF.eulerAngles.y.ToString("0")).Append(" queen rel dig ").Append(rel(queen, D, DF)).Append(" broodYaw-digYaw ").Append(UnityEngine.Mathf.DeltaAngle(DF.eulerAngles.y, broodYaw).ToString("0")).Append(" dist ").Append(toQueen.magnitude.ToString("0.0")).Append('\n');
sb.Append("guard rel dig ").Append(rel(layout.GuardPost, D, DF)).Append(" gateYaw-digYaw ").Append(UnityEngine.Mathf.DeltaAngle(DF.eulerAngles.y, layout.GateFacing.eulerAngles.y).ToString("0")).Append('\n');
return sb.ToString();
