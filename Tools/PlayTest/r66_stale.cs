// Round 66: is the calibration stamped for the current inputs?
var loaded = Game.Editor.BalanceStages.Load(true);
return "stale " + loaded.Stale + " | " + loaded.CalibrationNote;
