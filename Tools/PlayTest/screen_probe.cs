// Reports the game view size and the canvas scale, to judge screenshots.
var canvas = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Canvas>();
return "screen=" + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + " canvasScale=" + (canvas != null ? canvas.scaleFactor.ToString("0.000") : "none") + " aspect=" + ((float)UnityEngine.Screen.width / UnityEngine.Screen.height).ToString("0.000");
