// Round 15b: loads "node <index>" (a campaign level on Medium) or "arena <boss>" (that boss's developer arena), as named
// in r15b_load.txt. Play mode must run from Boot.
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/r15b_load.txt").Trim().Split(' ');
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
if (args[0] == "node")
{
    var node = catalog.Nodes[int.Parse(args[1])];
    Game.Core.Services.Register(new Game.Levels.LevelSession(node.Level, catalog.Difficulties[1]));
    UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.Level);
    return "loading " + node.Level.name;
}
var arena = catalog.BossArena;
foreach (var boss in arena.Bosses)
{
    if (boss.name == args[1])
    {
        Game.Core.Services.Register(new Game.Levels.LevelSession(arena.LevelFor(boss), arena.Difficulty, false, null, arena.WavesFor(boss), arena.StartingMoney));
        UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.Level);
        return "loading arena " + arena.LevelFor(boss).name;
    }
}
return "no boss " + args[1];
