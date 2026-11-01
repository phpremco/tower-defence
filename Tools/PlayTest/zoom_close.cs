// After zoom_liquid.cs: switches the pan-zoom controller off and sets the orthographic size from zoom_size.txt
// directly (world units, half the view height), for close-ups beyond the gameplay zoom limit.
float half = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
pan.enabled = false;
UnityEngine.Camera.main.orthographicSize = half;
return "ortho size " + half;
