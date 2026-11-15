// Round 66: on the upgrade screen, presses the upgrade row with the index in Tools/PlayTest/r66_buy_arg.txt ("<row> [times]")
// of the chosen subject, through the row's own button (so a row that is not interactable buys nothing), and reports the rows.
string[] a = System.IO.File.ReadAllText("Tools/PlayTest/r66_buy_arg.txt").Trim().Split(' ');
var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.UpgradeScreen>();
if (screen == null) return "no upgrade screen";
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var rows = (System.Collections.IList)typeof(Game.UI.UpgradeScreen).GetField("_rows", flags).GetValue(screen);
int index = int.Parse(a[0]);
int times = a.Length > 1 ? int.Parse(a[1]) : 1;
var sb = new System.Text.StringBuilder();
for (int t = 0; t < times; t++)
{
    var button = (UnityEngine.UI.Button)rows[index].GetType().GetField("Button").GetValue(rows[index]);
    if (button.IsInteractable()) button.onClick.Invoke(); else sb.Append("[row not buyable] ");
}
foreach (object row in rows)
{
    var button = (UnityEngine.UI.Button)row.GetType().GetField("Button").GetValue(row);
    if (!button.gameObject.activeSelf) continue;
    var name = (UnityEngine.UI.Text)row.GetType().GetField("Name").GetValue(row);
    var change = (UnityEngine.UI.Text)row.GetType().GetField("Change").GetValue(row);
    var cost = (UnityEngine.UI.Text)row.GetType().GetField("Cost").GetValue(row);
    sb.Append(name.text + ": " + change.text + " | " + cost.text + " | buyable " + button.IsInteractable() + "; ");
}
var progress = Game.Core.Services.Get<Game.Economy.ProgressService>();
return sb + " research " + progress.Data.ResearchPoints + " power " + progress.Data.PowerCurrency;
