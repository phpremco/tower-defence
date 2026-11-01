// Jumps to the planet map. Play mode must be running from Boot.
UnityEngine.Time.timeScale = 1f;
UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.WorldMap);
return "loading world map";
