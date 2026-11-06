// Round 16: with "old" in r16_furrow_arg.txt, draws the Sealed One's furrow for this frame as round 13 did (open end half
// a body half width behind the pivot, a 2.2 m opening ramp, pinched to a point over 3 m) so a still can compare it; with
// "new" it draws the round-16 furrow (open end at the pivot, a 0.8 m ramp, a 0.4 m cut). The signature redraws the furrow
// on the next frame, so take the still at once. Also reports the open end against the pivot.
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r16_furrow_arg.txt").Trim();
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain == null) { return "no boss"; }
var signature = brain.Signature as Game.Bosses.RecognitionSignature;
if (signature == null) { return "not the Sealed One"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var furrow = (Game.Bosses.CrackLineView)typeof(Game.Bosses.RecognitionSignature).GetField("_furrow", flags).GetValue(signature);
float open = (float)typeof(Game.Bosses.RecognitionSignature).GetField("_furrowOpen", flags).GetValue(signature);
if (furrow == null) { return "no furrow yet"; }
var mark = (UnityEngine.Material)typeof(Game.Bosses.CrackLineView).GetField("_mark", flags).GetValue(furrow);
float half = 2.4f;
if (mark == null) { return "furrow has no mark"; }
if (mode == "old") { furrow.SetOpenRamp(2.2f); mark.SetFloat("_HeadTaper", 3f); furrow.OpenTo(open - half * 0.5f); }
else { furrow.SetOpenRamp(0.8f); mark.SetFloat("_HeadTaper", 0.4f); furrow.OpenTo(open); }
var near = furrow.AlongNear(brain.transform.position, System.Math.Max(0f, open - 3f), 6f);
return mode + ": open end " + open.ToString("0.00") + " m along, pivot at " + near.ToString("0.00") + " m, shader open " + mark.GetFloat("_OpenDistance").ToString("0.00") + " ramp " + mark.GetFloat("_OpenRamp").ToString("0.00") + " taper " + mark.GetFloat("_HeadTaper").ToString("0.00");
