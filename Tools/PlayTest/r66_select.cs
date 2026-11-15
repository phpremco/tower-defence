// Round 66: on the upgrade screen, selects the tile whose name is the text of Tools/PlayTest/r66_select_arg.txt
// ("Spencer", "Base", "Gun Turret", ...) and puts the gamepad focus on it; an empty file lists the names.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/r66_select_arg.txt").Trim();
var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.UpgradeScreen>();
if (screen == null) { return "no upgrade screen"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var subjects = (System.Collections.IList)typeof(Game.UI.UpgradeScreen).GetField("_subjects", flags).GetValue(screen);
var names = new System.Text.StringBuilder();
foreach (object subject in subjects)
{
    string name = (string)subject.GetType().GetField("Name").GetValue(subject);
    names.Append(name).Append("; ");
    if (name == wanted)
    {
        typeof(Game.UI.UpgradeScreen).GetMethod("Select", flags).Invoke(screen, new[] { subject });
        var tile = (UnityEngine.UI.Button)subject.GetType().GetField("Tile").GetValue(subject);
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(tile.gameObject);
        return "selected " + name;
    }
}
return "no '" + wanted + "' among: " + names;
