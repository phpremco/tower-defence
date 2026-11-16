// Round 68: the paper side of one cell, wave by wave, on the waves the solve gives now (in memory, nothing written):
// kind, health multiplier, plated and flying heads, the human lens (towers alone) and with the hero, money and purchase.
// Argument: Tools/PlayTest/r68_arg.txt, "C1_03 0" (level key, difficulty). Writes Tools/PlayTest/r68_ledger.txt.
string[] arg = System.IO.File.ReadAllText("Tools/PlayTest/r68_arg.txt").Trim().Split(' ');
int d = int.Parse(arg[1]);
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
var upgrades = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>(Game.Editor.BalanceStages.UpgradesPath);
var unlocks = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>(Game.Editor.BalanceStages.UnlocksPath);
var calibration = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.BalanceCalibration>(Game.Levels.BalanceCalibration.AssetPath);
Game.Balance.BalanceRules rules = Game.Editor.BalanceSheet.Rules;
Game.Balance.CampaignModel model = Game.Levels.BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules, calibration);
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
{
    var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    model.Enemies[enemy.name] = Game.Levels.BalanceLedger.Stats(enemy);
}

int index = model.IndexOf(arg[0]);
Game.Balance.WaveScriptSpec script = Game.Editor.WaveScripts.ByKey(arg[0]);
var sets = model.BuildWaves(index, script, out _);
var waves = sets[d];
var human = Game.Balance.Ledger.Evaluate(model.InputFor(index, d), waves, model.Difficulties[d], rules.HumanTowersOnly());
var hero = Game.Balance.Ledger.Evaluate(model.InputFor(index, d), waves, model.Difficulties[d], rules.HumanWithHero());
var sb = new System.Text.StringBuilder(arg[0] + " d" + d + "\n");
for (int i = 0; i < waves.Count; i++)
{
    int plated = 0;
    int air = 0;
    int heads = 0;
    foreach (var group in waves[i].Groups)
    {
        if (group.Enemy.IsBoss)
        {
            continue;
        }

        heads += group.Count;
        plated += group.Enemy.Armor > 0f ? group.Count : 0;
        air += group.Enemy.Flies ? group.Count : 0;
    }

    sb.Append("w").Append(i + 1).Append(' ').Append(Game.Balance.WaveComposer.KindOf(script, d, i + 1)).Append(human[i].Boss ? " boss" : string.Empty)
        .Append(" x").Append(waves[i].HealthMultiplier.ToString("0.00"))
        .Append(" heads ").Append(heads).Append(" plated ").Append(plated).Append(" air ").Append(air)
        .Append(" | human g ").Append(human[i].GroundPressure.ToString("0.00")).Append(" a ").Append(human[i].AirPressure.ToString("0.00"))
        .Append(" | hero g ").Append(hero[i].GroundPressure.ToString("0.00")).Append(" a ").Append(hero[i].AirPressure.ToString("0.00"))
        .Append(" | money ").Append(human[i].Money).Append(" buys ").Append(human[i].Purchase).Append('\n');
}

System.IO.File.WriteAllText("Tools/PlayTest/r68_ledger.txt", sb.ToString());
return sb.ToString();
