Game.Core.Services.TryGet(out Game.Levels.LevelContext context);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
var f = typeof(Game.Levels.CameraPanZoom).GetField("_focus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var g = typeof(Game.Levels.CameraPanZoom).GetField("_glideTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var recon = UnityEngine.Object.FindAnyObjectByType<Game.Levels.ReconView>();
return "hero " + context.Hero.transform.position.ToString("0.0") + " stand0 " + context.Hero.StandAt(0).ToString("0.0") + " rigFocus " + rig.Focus.ToString("0.0") + " panFocus " + ((UnityEngine.Vector3)f.GetValue(pan)).ToString("0.0") + " glide " + g.GetValue(pan) + " zoom " + rig.Zoom.ToString("0.00") + " locked " + pan.Locked + " recon " + (recon != null) + " pointer " + Game.Core.Services.Get<Game.Platform.IInputProvider>().PointerPosition;
