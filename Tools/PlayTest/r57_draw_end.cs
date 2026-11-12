// Round 57: how far the pistol draw's last frame is from the aim (Shoot frame 0) on the upper body: summed and largest bone turn.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/Story/Mercer.asset");
var shoot = art.Clips[art.IndexOf(Game.Core.ModelClipRole.Shoot)].Clip;
UnityEngine.AnimationClip draw = null;
foreach (var c in art.Clips) if (c.Clip != null && c.Clip.name == "Draw_Pistol") draw = c.Clip;
var model = UnityEngine.Object.Instantiate(art.Model);
UnityEngine.Transform upper = null;
foreach (var t in model.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == art.UpperBodyBone) upper = t;
var bones = upper.GetComponentsInChildren<UnityEngine.Transform>(true);
shoot.SampleAnimation(model, 0f);
var aim = new UnityEngine.Quaternion[bones.Length];
for (int i = 0; i < bones.Length; i++) aim[i] = bones[i].localRotation;
draw.SampleAnimation(model, draw.length);
float sum = 0f, max = 0f; string who = "";
for (int i = 0; i < bones.Length; i++) { float a = UnityEngine.Quaternion.Angle(aim[i], bones[i].localRotation); sum += a; if (a > max) { max = a; who = bones[i].name; } }
UnityEngine.Object.DestroyImmediate(model);
return "draw end vs aim: sum " + sum.ToString("0.0") + " max " + max.ToString("0.0") + " " + who + " (draw " + draw.length.ToString("0.000") + "s)";
