// Night 2: the commander's right hand and head in world space and relative to his root, for the reaction pose.
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
var mercer = UnityEngine.GameObject.Find("Mercer");
if (player == null || mercer == null) return "none";
UnityEngine.Transform Find(UnityEngine.Transform t, string n) { if (t.name == n) return t; for (int i = 0; i < t.childCount; i++) { var f = Find(t.GetChild(i), n); if (f != null) return f; } return null; }
var hand = Find(mercer.transform, "Hand_R"); var head = Find(mercer.transform, "Head"); var arm = Find(mercer.transform, "UpperArm_R");
var root = mercer.transform;
return player.Sequencer.Current.Action + " t=" + player.Sequencer.LocalTime.ToString("0.00") + " handLocal " + root.InverseTransformPoint(hand.position).ToString("0.00") + " headLocal " + root.InverseTransformPoint(head.position).ToString("0.00") + " armRot " + arm.localEulerAngles.ToString("0") + " smr " + mercer.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().Length;
