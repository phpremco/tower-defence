// Night 2: lists the first seated eye pairs of the underground set (pivots, facing, scale, opening) and the count.
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no player";
var field = typeof(Game.UI.CutscenePlayer).GetField("_sets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var sets = (System.Collections.IDictionary)field.GetValue(player);
var under = sets[Game.Levels.CutsceneSet.Underground] as Game.UI.UndergroundSet;
if (under == null || under.Eyes == null) return "no underground";
var sb = new System.Text.StringBuilder("pairs " + under.Eyes.Count + " | ");
for (int i = 0; i < System.Math.Min(6, under.Eyes.Count); i++) { var p = under.Eyes[i]; sb.Append(i + ": L " + p.Left.ToString("0.00") + " R " + p.Right.ToString("0.00") + " f " + p.Facing.ToString("0.00") + " s " + p.Scale.ToString("0.00") + " at " + p.OpensAt.ToString("0.0") + " v" + p.Variant + " | "); }
sb.Append(" cam " + UnityEngine.Camera.main.transform.position.ToString("0.00") + " fwd " + UnityEngine.Camera.main.transform.forward.ToString("0.00"));
return sb.ToString();
