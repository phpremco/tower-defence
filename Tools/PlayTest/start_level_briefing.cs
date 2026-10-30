// Loads the level whose campaign index is in Tools/PlayTest/level_arg.txt, on Medium, with the briefing and the recon
// fly-over as a player sees them from the map. Play mode must be running from Boot.
int index = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/level_arg.txt").Trim());
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
Game.Core.Services.Register(new Game.Levels.LevelSession(catalog.Nodes[index].Level, catalog.Difficulties[1], true));
UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.Level);
return "loading " + catalog.Nodes[index].Level.name + " with briefing";
