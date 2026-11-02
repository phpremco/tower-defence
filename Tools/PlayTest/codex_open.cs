// Opens the codex on the page named in Tools/PlayTest/menu_arg.txt ("recipes" or "enemies") and selects the row
// index in card_arg.txt, so a screenshot shows that entry.
string page = System.IO.File.ReadAllText("Tools/PlayTest/menu_arg.txt").Trim();
int row = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/card_arg.txt").Trim());
var codex = UnityEngine.Object.FindAnyObjectByType<Game.UI.CodexView>();
if (codex == null) { return "no codex view"; }
codex.Open();
var type = typeof(Game.UI.CodexView);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var pageType = type.GetNestedType("Page", flags);
object wanted = System.Enum.Parse(pageType, page == "enemies" ? "Enemies" : "Recipes");
type.GetMethod("ShowPage", flags).Invoke(codex, new[] { wanted });
type.GetMethod("Show", flags).Invoke(codex, new object[] { row });
var rows = (System.Collections.Generic.List<UnityEngine.UI.Button>)type.GetField("_rows", flags).GetValue(codex);
if (row < rows.Count) { Game.UI.UiKit.Select(rows[row]); }
return "codex open on " + page + " row " + row + " of " + rows.Count + ", available " + codex.Available;
