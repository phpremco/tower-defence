// Clicks the level marker whose index is in Tools/PlayTest/map_arg.txt (as a number), like a mouse click.
int index = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/map_arg.txt").Trim());
var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.WorldMapScreen>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(Game.UI.WorldMapScreen).GetMethod("OnMarkerClicked", flags).Invoke(screen, new object[] { index });
return "clicked " + index + " focused=" + typeof(Game.UI.WorldMapScreen).GetField("_focused", flags).GetValue(screen) + " chosen=" + typeof(Game.UI.WorldMapScreen).GetField("_chosen", flags).GetValue(screen);
