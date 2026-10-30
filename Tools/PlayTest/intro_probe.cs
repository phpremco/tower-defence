// Reports where the intro is: beat index, elapsed seconds, planet scale and the text on screen.
var intro = UnityEngine.Object.FindAnyObjectByType<Game.UI.IntroScreen>();
if (intro == null)
{
    return "no intro";
}

var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
int beat = (int)typeof(Game.UI.IntroScreen).GetField("_beat", flags).GetValue(intro);
float elapsed = (float)typeof(Game.UI.IntroScreen).GetField("_elapsed", flags).GetValue(intro);
var planet = (UnityEngine.RectTransform)typeof(Game.UI.IntroScreen).GetField("_planet", flags).GetValue(intro);
var text = (UnityEngine.UI.Text)typeof(Game.UI.IntroScreen).GetField("_text", flags).GetValue(intro);
return "beat=" + beat + " elapsed=" + elapsed.ToString("0.0") + " planetScale=" + planet.localScale.x.ToString("0.00") + " pos=" + planet.anchoredPosition.ToString("0") + " text=" + text.text.Substring(0, System.Math.Min(30, text.text.Length)) + " unscaled=" + UnityEngine.Time.unscaledTime.ToString("0.0");
