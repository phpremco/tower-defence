var hero = UnityEngine.Object.FindAnyObjectByType<Game.Hero.Commander>();
if (hero == null) return "no hero";
string r = "post=" + hero.PostIndex + "/" + hero.PostCount + " walking=" + hero.IsWalking + " flying=" + hero.IsFlying + " down=" + hero.IsDown;
for (int i = 0; i < hero.Abilities.Count; i++) { var a = hero.Abilities[i]; r += " | slot" + i + " " + a.Definition.DisplayName + " cd=" + a.Remaining.ToString("0.0") + " can=" + hero.CanUse(i); }
return r;
