// Drives the planet map without a controller: "next", "prev", "choose" or "cancel" from Tools/PlayTest/map_arg.txt.
string what = System.IO.File.ReadAllText("Tools/PlayTest/map_arg.txt").Trim();
var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.WorldMapScreen>();
if (screen == null)
{
    return "no map screen";
}

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
switch (what)
{
    case "next":
        typeof(Game.UI.WorldMapScreen).GetMethod("Step", flags).Invoke(screen, new object[] { 1 });
        break;
    case "prev":
        typeof(Game.UI.WorldMapScreen).GetMethod("Step", flags).Invoke(screen, new object[] { -1 });
        break;
    case "choose":
        typeof(Game.UI.WorldMapScreen).GetMethod("OnConfirm", flags).Invoke(screen, null);
        break;
    case "cancel":
        typeof(Game.UI.WorldMapScreen).GetMethod("OnCancel", flags).Invoke(screen, null);
        break;
}

int focused = (int)typeof(Game.UI.WorldMapScreen).GetField("_focused", flags).GetValue(screen);
int chosen = (int)typeof(Game.UI.WorldMapScreen).GetField("_chosen", flags).GetValue(screen);
return what + " -> focused=" + focused + " chosen=" + chosen;
