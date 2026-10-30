// Lists the loadout cards (name, fill colour) and the chosen count, so the panel can be checked without a mouse.
var panel = UnityEngine.Object.FindAnyObjectByType<Game.UI.LoadoutPanelView>();
if (panel == null) return "no loadout panel";
var sb = new System.Text.StringBuilder();
foreach (var text in panel.GetComponentsInChildren<UnityEngine.UI.Text>()) { if (text.text.Length > 0) sb.Append(text.text.Replace("\n", " ")).Append(" | "); }
return sb.ToString();
