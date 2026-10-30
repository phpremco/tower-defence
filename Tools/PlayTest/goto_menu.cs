// Leaves the level for the main menu, so the menus can be screenshotted inside the same Play Mode session.
UnityEngine.Time.timeScale = 1f;
UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.MainMenu);
return "loading main menu";
