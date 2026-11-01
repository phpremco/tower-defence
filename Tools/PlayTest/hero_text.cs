// Forces the HUD hero timer text so its placement beside the base text can be checked without losing the hero.
var hud = UnityEngine.Object.FindAnyObjectByType<Game.UI.HudView>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var hero = (UnityEngine.UI.Text)typeof(Game.UI.HudView).GetField("_heroText", flags).GetValue(hud);
var baseText = (UnityEngine.UI.Text)typeof(Game.UI.HudView).GetField("_baseText", flags).GetValue(hud);
hero.text = "Hero down 1:53";
return "base width " + baseText.preferredWidth.ToString("0") + " hero x " + hero.rectTransform.anchoredPosition.x.ToString("0");
