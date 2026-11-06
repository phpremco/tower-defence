// Round 15b: switches the lighting trial (PlaytestSettings._isolatePreviewLights) on the loaded settings object only, as
// named in r15b_flag.txt ("on" or "off"); the asset on disk is not touched. Takes effect from the next level load.
bool on = System.IO.File.ReadAllText("Tools/PlayTest/r15b_flag.txt").Trim() == "on";
if (!Game.Core.Services.TryGet(out Game.Core.PlaytestSettings settings)) { return "no settings"; }
typeof(Game.Core.PlaytestSettings).GetField("_isolatePreviewLights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(settings, on);
return "isolate preview lights " + settings.IsolatePreviewLights + ", layer " + Game.Core.PlaytestSettings.IsolatedPreviewLayer();
