// Round 23a: puts the level camera at the zoom in zoom_size.txt on the field's centre (through CameraPanZoom, as the
// player's zoom would), to compare the look zoomed in and zoomed out. Reports the lights and the camera size.
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>().SetView(UnityEngine.Vector3.zero, zoom);
int lit = 0;
foreach (var light in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None)) { if (light.enabled && light.type == UnityEngine.LightType.Directional) { lit++; } }
return "zoom " + zoom + " size " + UnityEngine.Camera.main.orthographicSize.ToString("0.0") + " directional " + lit + " fog " + UnityEngine.RenderSettings.fog + " " + UnityEngine.RenderSettings.fogDensity.ToString("0.0000") + " sun " + (UnityEngine.RenderSettings.sun != null ? UnityEngine.RenderSettings.sun.name : "none");
