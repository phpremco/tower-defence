// Reports the screen pixel position of the level marker whose index is in Tools/PlayTest/map_arg.txt, for a simulated mouse click.
int index = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/map_arg.txt").Trim());
var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.WorldMapScreen>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var markers = (System.Collections.IList)typeof(Game.UI.WorldMapScreen).GetField("_markers", flags).GetValue(screen);
object marker = markers[index];
var root = (UnityEngine.RectTransform)marker.GetType().GetField("Root").GetValue(marker);
var canvas = root.GetComponentInParent<UnityEngine.Canvas>();
UnityEngine.Vector2 p = UnityEngine.RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, root.position);
return p.x.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " " + p.y.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " screen " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height;
