// Round 50: the hero's weapon state: the logic's weapon in hand and the draw time left, the view's upper-body clip and its
// time, whether a bolt is drawn, and the shots and draws counted since r50_hero_count.cs hooked the events.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var hero = context.Hero;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var drawn = (Game.Hero.HeroWeaponDefinition)typeof(Game.Hero.Commander).GetField("_drawn", flags).GetValue(hero);
float drawLeft = (float)typeof(Game.Hero.Commander).GetField("_drawRemaining", flags).GetValue(hero);
var sb = new System.Text.StringBuilder();
sb.Append("t=").Append(UnityEngine.Time.time.ToString("0.00")).Append(" inHand=").Append(drawn != null ? drawn.name : "none").Append(" drawLeft=").Append(drawLeft.ToString("0.00"));
sb.Append(" target=").Append(hero.Target != null).Append(" walking=").Append(hero.IsWalking);
var view = UnityEngine.Object.FindAnyObjectByType<Game.Hero.HeroArtView>();
if (view != null)
{
    var body = typeof(Game.Hero.HeroArtView).GetField("_body", flags).GetValue(view);
    var player = (Game.Core.ModelClipPlayer)typeof(Game.Hero.HeroArtView).GetField("_player", flags).GetValue(view);
    var art = ((Game.Core.ModelArtDefinition)body.GetType().GetProperty("Art").GetValue(body));
    sb.Append(" upper=").Append(player.Upper >= 0 ? art.Clips[player.Upper].Clip.name : "none");
    sb.Append(" hand=").Append(typeof(Game.Hero.HeroArtView).GetField("_drawn", flags).GetValue(view));
    sb.Append(" switch=").Append((int)typeof(Game.Hero.HeroArtView).GetField("_switchClip", flags).GetValue(view));
    var bolt = (UnityEngine.LineRenderer)typeof(Game.Hero.HeroArtView).GetField("_bolt", flags).GetValue(view);
    sb.Append(" bolt=").Append(bolt != null && bolt.enabled);
}

string counts = System.IO.File.Exists("Tools/PlayTest/r50_hero_counts.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r50_hero_counts.txt") : "";
return sb.Append(" ").Append(counts).ToString();
