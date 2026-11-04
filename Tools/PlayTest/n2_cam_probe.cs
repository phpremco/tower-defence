// Night 2: where the cutscene camera and the commander are right now, and the shot.
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no player";
var cam = UnityEngine.Camera.main.transform;
var mercer = UnityEngine.GameObject.Find("Mercer");
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sets = (System.Collections.IDictionary)typeof(Game.UI.CutscenePlayer).GetField("_sets", flags).GetValue(player);
var field = sets[Game.Levels.CutsceneSet.Field];
var layout = (Game.UI.FieldLayout)field.GetType().GetField("_layout", flags).GetValue(field);
return player.Sequencer.Current.Action + " t=" + player.Sequencer.LocalTime.ToString("0.00") + " cam " + cam.position.ToString("0.00") + " fwd " + cam.forward.ToString("0.00") + " mercer " + (mercer != null ? mercer.transform.position.ToString("0.00") + " active " + mercer.activeInHierarchy : "none") + " dig " + layout.DigSite.ToString("0.00") + " digYaw " + layout.DigFacing.eulerAngles.y.ToString("0") + " guard " + layout.GuardPost.ToString("0.00") + " gateC " + layout.GateCentre.ToString("0.00");
