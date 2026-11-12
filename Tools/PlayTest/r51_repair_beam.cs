// Round 51: shows the hero's gate-repair beam for a still without needing a damaged gate: hands the hero's ability view
// the GateRepair ability through its own handler (view only; the gate is not healed). Reports the beam's ends.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var hero = context.Hero;
Game.Hero.HeroAbilityDefinition repair = null;
int slot = -1;
for (int i = 0; i < hero.Abilities.Count; i++)
{
    if (hero.Abilities[i].Definition.Kind == Game.Hero.HeroAbilityKind.GateRepair) { repair = hero.Abilities[i].Definition; slot = i; break; }
}
if (repair == null) { return "the hero has no gate repair"; }
var view = UnityEngine.Object.FindAnyObjectByType<Game.Hero.HeroAbilityView>();
if (view == null) { return "no ability view"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(Game.Hero.HeroAbilityView).GetMethod("OnAbilityUsed", flags).Invoke(view, new object[] { slot, repair, hero.transform.position });
return "repair beam shown, slot " + slot + " gate " + hero.GuardedPosition.ToString("0.00");
