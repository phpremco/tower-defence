var mix = Game.Core.Services.Get<Game.Core.AudioMix>();
var p = Game.Core.Services.Get<Game.Economy.ProgressService>().Data;
var cam = Game.Core.Services.Get<Game.Core.CameraControls>();
return "listener=" + UnityEngine.AudioListener.volume.ToString("0.00") + " mix master=" + mix.Master.ToString("0.00") + " data m/mu/e/a=" + p.MasterVolumePercent + "/" + p.MusicVolumePercent + "/" + p.EffectsVolumePercent + "/" + p.AmbienceVolumePercent + " cam=" + cam.SpeedScale + " edge=" + cam.EdgeScrolling + " radar=" + cam.RadarOn + " " + cam.RadarScale + " | data cam " + p.CameraSpeedPercent + " edgeOff=" + p.EdgeScrollingOff + " radarOff=" + p.RadarOff + " size=" + p.RadarSizePercent;
