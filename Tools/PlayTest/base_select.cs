// On the upgrade screen, selects the tile of the drone kind named in Tools/PlayTest/zoom_arg.txt so its preview shows.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/zoom_arg.txt").Trim();
var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.UpgradeScreen>();
if (screen == null) { return "no upgrade screen"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var subjects = (System.Collections.IList)typeof(Game.UI.UpgradeScreen).GetField("_subjects", flags).GetValue(screen);
foreach (object subject in subjects)
{
    var drone = subject.GetType().GetField("Drone").GetValue(subject) as Game.Drones.DroneDefinition;
    if (drone != null && drone.Kind.ToString() == wanted)
    {
        typeof(Game.UI.UpgradeScreen).GetMethod("Select", flags).Invoke(screen, new[] { subject });
        return "selected " + drone.name;
    }
}
return "no " + wanted;
