// Skips the intro the way a button press would, marking it seen and loading the main menu.
var intro = UnityEngine.Object.FindAnyObjectByType<Game.UI.IntroScreen>();
if (intro == null)
{
    return "no intro";
}

typeof(Game.UI.IntroScreen).GetMethod("Finish", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(intro, null);
return "skipped, introSeen=" + Game.Core.Services.Get<Game.Economy.ProgressService>().Data.IntroSeen;
