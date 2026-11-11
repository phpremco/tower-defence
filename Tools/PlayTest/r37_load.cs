// Round 37: loads campaign node "<index> <difficulty index>" or "arena <boss>" (Tools/PlayTest/r37_load_arg.txt; 0 Easy .. 3 Psycho) from a
// Boot play session, like the map's play button.
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/r37_load_arg.txt").Trim().Split(' ');
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
if (args[0] == "arena")
{
    // "arena <boss>": that continent boss's developer arena (its own level and waves), as r14_arena.cs loads it.
    var arena = catalog.BossArena;
    foreach (var boss in arena.Bosses)
    {
        if (boss.name != args[1]) continue;
        Game.Core.Services.Register(new Game.Levels.LevelSession(arena.LevelFor(boss), arena.Difficulty, false, null, arena.WavesFor(boss), arena.StartingMoney));
        UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.Level);
        return "loading arena " + arena.LevelFor(boss).name + " for " + boss.name;
    }
    return "no boss " + args[1];
}
var node = catalog.Nodes[int.Parse(args[0])];
var difficulty = catalog.Difficulties[int.Parse(args[1])];
Game.Core.Services.Register(new Game.Levels.LevelSession(node.Level, difficulty));
UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.Level);
return "loading " + node.Level.name + " on " + difficulty.name;
