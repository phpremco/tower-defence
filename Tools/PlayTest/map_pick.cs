// Presses the first open beacon on the planet map, as the player would, and reports which one it was.
var map = UnityEngine.Object.FindAnyObjectByType<Game.UI.WorldMapScreen>();
if (map == null)
{
    return "no map on screen";
}

foreach (var button in map.GetComponentsInChildren<UnityEngine.UI.Button>())
{
    if (button.name == "Mark" && button.interactable)
    {
        button.onClick.Invoke();
        return "picked beacon under " + button.transform.parent.GetComponentInChildren<UnityEngine.UI.Text>().text.Replace("\n", " / ");
    }
}

return "no open beacon";
