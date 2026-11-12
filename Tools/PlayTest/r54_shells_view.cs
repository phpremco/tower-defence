// Round 54: the Shattered's armour off by the game's own path (SplitSignature after the rejoin: a BoneScaleView holding
// every Shell bone at 0 in LateUpdate, after the clip is sampled), then one held frame so the skinning runs. r52_shells_off.cs
// sets the bones on a paused frame, which the GPU skinning of the next render never sees. Reports the bones held.
var bones = new System.Collections.Generic.List<UnityEngine.Transform>();
Game.Enemies.Enemy giant = null;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.name == "Shattered") { giant = enemy; break; }
}

if (giant == null) { return "no living Shattered"; }
foreach (var bone in giant.GetComponentsInChildren<UnityEngine.Transform>(true))
{
    if (bone.name.StartsWith("Shell_")) { bones.Add(bone); }
}

var view = giant.gameObject.AddComponent<Game.Bosses.BoneScaleView>();
view.SetBones(bones.ToArray());
view.SetScale(0f, true);
view.Holding = true;
UnityEditor.EditorApplication.isPaused = true;
UnityEditor.EditorApplication.Step();
float largest = 0f;
foreach (var bone in bones) { largest = UnityEngine.Mathf.Max(largest, bone.localScale.magnitude); }
return "holding " + bones.Count + " shell bones at 0 after one frame (largest scale " + largest.ToString("0.000") + ")";
