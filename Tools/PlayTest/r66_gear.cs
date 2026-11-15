// Round 66: sets permanent upgrade levels in memory (not saved; the harness restores the save anyway) before a level is
// loaded, so the hero takes the field with that gear. Tools/PlayTest/r66_gear_arg.txt: "Hero_Armour=4 Hero_RepairDrone=2 Hero_Damage=4".
var progress = Game.Core.Services.Get<Game.Economy.ProgressService>();
string[] pairs = System.IO.File.ReadAllText("Tools/PlayTest/r66_gear_arg.txt").Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
foreach (string pair in pairs)
{
    string[] kv = pair.Split('=');
    int level = int.Parse(kv[1]);
    bool found = false;
    for (int i = 0; i < progress.Data.Upgrades.Count; i++)
    {
        if (progress.Data.Upgrades[i].Id != kv[0]) continue;
        progress.Data.Upgrades[i] = new Game.Economy.UpgradeLevel { Id = kv[0], Level = level };
        found = true;
    }
    if (!found) progress.Data.Upgrades.Add(new Game.Economy.UpgradeLevel { Id = kv[0], Level = level });
}
return "gear in memory: " + string.Join(" ", pairs);
