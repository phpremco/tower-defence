// Round 60: the drone cards in the base panel. Tools/PlayTest/r60_card_arg.txt: "select <i>" gives card i the gamepad
// focus, "clear" drops the focus, "pos <i>" prints card i's screen pixel (for simulate_pointer), "state" prints each card's
// role, whether the mouse is over it, and the description box's text and visibility.
var words = System.IO.File.ReadAllText("Tools/PlayTest/r60_card_arg.txt").Trim().Split(' ');
var view = UnityEngine.Object.FindAnyObjectByType<Game.UI.BaseMenuView>();
if (view == null) return "no base panel";
var cards = new System.Collections.Generic.List<UnityEngine.UI.Button>();
foreach (var hover in view.GetComponentsInChildren<Game.UI.PointerHover>(true)) cards.Add(hover.GetComponent<UnityEngine.UI.Button>());
foreach (var hover in UnityEngine.Object.FindObjectsByType<Game.UI.PointerHover>(UnityEngine.FindObjectsSortMode.InstanceID)) if (!cards.Contains(hover.GetComponent<UnityEngine.UI.Button>())) cards.Add(hover.GetComponent<UnityEngine.UI.Button>());
cards.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
var events = UnityEngine.EventSystems.EventSystem.current;
switch (words[0])
{
    case "select":
        events.SetSelectedGameObject(cards[int.Parse(words[1])].gameObject);
        return "selected card " + words[1];
    case "clear":
        events.SetSelectedGameObject(null);
        return "cleared";
    case "pos":
        var card = cards[int.Parse(words[1])];
        var canvas = card.GetComponentInParent<UnityEngine.Canvas>();
        var p = UnityEngine.RectTransformUtility.WorldToScreenPoint(canvas.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, card.transform.position);
        return p.x.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " " + p.y.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
}
var sb = new System.Text.StringBuilder("cards " + cards.Count + ":");
foreach (var card in cards)
{
    var role = card.GetComponentsInChildren<UnityEngine.UI.Text>(true);
    sb.Append(" [");
    foreach (var t in role) sb.Append(t.text.Replace("\n", "/")).Append("|");
    sb.Append(" hover=").Append(card.GetComponent<Game.UI.PointerHover>().Hovered).Append("]");
}
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (t.name != "Description" || t.GetComponentInParent<Game.UI.BaseMenuView>(true) == null && t.parent?.name != "BasePanel") continue;
    sb.Append(" | description active=").Append(t.gameObject.activeInHierarchy).Append(" text=").Append(t.GetComponentInChildren<UnityEngine.UI.Text>(true).text.Replace("\n", " / ")).Append(" size=").Append(((UnityEngine.RectTransform)t).rect.size);
}
return sb.ToString();
