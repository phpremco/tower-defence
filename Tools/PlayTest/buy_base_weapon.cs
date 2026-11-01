// Buys base weapon levels (count in Tools/PlayTest/points_arg.txt) so a level starts with base turrets.
// This saves progress: restore the save file afterwards when testing on a real profile.
int wanted = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/points_arg.txt").Trim());
var progress = Game.Core.Services.Get<Game.Economy.ProgressService>();
var catalog = Game.Core.Services.Get<Game.Economy.UpgradeCatalog>();
progress.AddRewards(500, 0);
int bought = 0;
foreach (var upgrade in catalog.Upgrades)
{
    if (upgrade.Target == Game.Economy.UpgradeTarget.BaseWeapon)
    {
        for (int i = 0; i < wanted; i++)
        {
            if (progress.TryBuyUpgrade(upgrade))
            {
                bought++;
            }
        }
    }
}

return "bought " + bought + " base weapon levels, now at " + Game.Economy.UpgradeEffects.ComputeGlobal(progress, catalog).BaseWeaponLevel;
