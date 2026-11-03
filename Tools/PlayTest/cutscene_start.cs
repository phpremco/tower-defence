// Plays the opening cutscene from a Boot play session (Play Mode running): registers a request and loads the Intro scene.
// Tools/PlayTest/cut_speed.txt (optional) holds the speed; "auto" in Tools/PlayTest/cut_auto.txt closes dialogue boxes at once.
var catalog = Game.Core.Services.Get<Game.Levels.CampaignCatalog>();
var request = new Game.Levels.CutsceneRequest(catalog.Story.OpeningCutscene, Game.Levels.CutsceneHandoff.FirstCampaignLevel);
if (System.IO.File.Exists("Tools/PlayTest/cut_speed.txt")) request.Speed = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/cut_speed.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
request.AutoAdvance = System.IO.File.Exists("Tools/PlayTest/cut_auto.txt") && System.IO.File.ReadAllText("Tools/PlayTest/cut_auto.txt").Trim() == "auto";
Game.Core.Services.Register(request);
UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.Intro);
return "intro speed " + request.Speed + " auto " + request.AutoAdvance;
