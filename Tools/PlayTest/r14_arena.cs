// Round 14: loads the developer boss arena of the boss named in r14_boss.txt (as BossArenaHarness.Load does), from Boot.
string bossName = System.IO.File.ReadAllText("Tools/PlayTest/r14_boss.txt").Trim();
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var arena = catalog.BossArena;
Game.Enemies.EnemyDefinition boss = null;
foreach (var candidate in arena.Bosses) { if (candidate.name == bossName) { boss = candidate; } }
if (boss == null) { return "no boss " + bossName; }
Game.Core.Services.Register(new Game.Levels.LevelSession(arena.LevelFor(boss), arena.Difficulty, false, null, arena.WavesFor(boss), arena.StartingMoney));
UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.Level);
return "loading arena " + arena.LevelFor(boss).name + " for " + bossName;
