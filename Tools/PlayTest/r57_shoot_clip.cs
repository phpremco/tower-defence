// Round 57: the hero's Shoot clip frame by frame against its first frame: the summed turn of the upper body's bones, the gun
// arm's and the hand's, so the authored kick (and whether the last frame is back at the aim) can be read.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/Story/Mercer.asset");
int index = art.IndexOf(Game.Core.ModelClipRole.Shoot);
var clip = art.Clips[index].Clip;
var model = UnityEngine.Object.Instantiate(art.Model);
var sb = new System.Text.StringBuilder("Shoot " + clip.length.ToString("0.000") + "s upper=" + art.Clips[index].UpperBody + ":");
UnityEngine.Transform upper = null, arm = null, hand = null;
foreach (var t in model.GetComponentsInChildren<UnityEngine.Transform>(true)) { if (t.name == art.UpperBodyBone) upper = t; if (t.name == "UpperArm_R") arm = t; if (t.name == "Hand_R") hand = t; }
var bones = upper.GetComponentsInChildren<UnityEngine.Transform>(true);
clip.SampleAnimation(model, 0f);
var rest = new UnityEngine.Quaternion[bones.Length];
for (int i = 0; i < bones.Length; i++) rest[i] = bones[i].localRotation;
var armRest = arm.rotation; var handPos = hand != null ? hand.position : UnityEngine.Vector3.zero;
int frames = UnityEngine.Mathf.RoundToInt(clip.length * 30f);
for (int f = 0; f <= frames; f++)
{
    clip.SampleAnimation(model, f / 30f);
    float sum = 0f; for (int i = 0; i < bones.Length; i++) sum += UnityEngine.Quaternion.Angle(rest[i], bones[i].localRotation);
    sb.Append(" f" + f + " sum " + sum.ToString("0.0") + " arm " + UnityEngine.Quaternion.Angle(armRest, arm.rotation).ToString("0.0") + (hand != null ? " hand " + (hand.position - handPos).ToString("0.000") : ""));
}
UnityEngine.Object.DestroyImmediate(model);
return sb.ToString();
