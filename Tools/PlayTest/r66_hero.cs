// Round 66: the hero in play. Tools/PlayTest/r66_hero_arg.txt:
//   "hurt <share>"   takes that share of his full health off him (through his armour), so he mends afterwards
//   "probe"          health, mending, gear, the companion's light, the blade and its aura
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c) || c.Hero == null) return "no hero";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string[] a = System.IO.File.ReadAllText("Tools/PlayTest/r66_hero_arg.txt").Trim().Split(' ');
var hero = c.Hero;
if (a[0] == "hurt")
{
    float before = hero.Health.Current;
    hero.TakeHazardDamage(hero.Health.Max * float.Parse(a[1], inv));
    return "hurt: " + before.ToString("0.0", inv) + " -> " + hero.Health.Current.ToString("0.0", inv) + " of " + hero.Health.Max.ToString("0", inv);
}
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var view = hero.GetComponent<Game.Hero.HeroArtView>();
var gear = hero.Gear;
var sb = new System.Text.StringBuilder();
sb.Append("health " + hero.Health.Current.ToString("0.0", inv) + "/" + hero.Health.Max.ToString("0", inv) + " mending " + hero.IsMending);
sb.Append(" | gear regen x" + gear.RegenMultiplier.ToString("0.00", inv) + " drone tier " + gear.RepairDroneTier + " taken x" + gear.DamageTakenMultiplier.ToString("0.00", inv) + " armour " + gear.ArmourTier + "/" + gear.ArmourTiers + " bladeAtTop " + gear.BladeAtTop);
if (view != null)
{
    var companion = (Game.Hero.HeroCompanionDrone)typeof(Game.Hero.HeroArtView).GetField("_companion", flags).GetValue(view);
    sb.Append(" | companion " + (companion == null ? "none" : "light " + companion.Fade.ToString("0.00", inv) + " at " + (companion.Holder.position - hero.transform.position).ToString("0.00")));
    var aura = typeof(Game.Hero.HeroArtView).GetField("_bladeAura", flags).GetValue(view);
    float ignite = (float)typeof(Game.Hero.HeroArtView).GetField("_ignite", flags).GetValue(view);
    var blade = (Game.Core.ArtModel)typeof(Game.Hero.HeroArtView).GetField("_blade", flags).GetValue(view);
    sb.Append(" | blade " + (blade != null && blade.Root.gameObject.activeInHierarchy ? "lit " + ignite.ToString("0.00", inv) : "away") + " aura " + (aura != null));
    int plates = 0;
    foreach (var r in hero.GetComponentsInChildren<UnityEngine.MeshRenderer>()) if (r.name == "ArmourPlate") plates++;
    sb.Append(" plates " + plates);
}
return sb.ToString();
