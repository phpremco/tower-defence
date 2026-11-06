// Round 18: takes the continent boss down to 70% health, so Gravemaw crosses its first burrow threshold (75%).
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain == null) { return "no boss"; }
var enemy = brain.GetComponent<Game.Enemies.Enemy>();
float target = enemy.Health.Max * 0.7f;
if (enemy.Health.Current > target) { enemy.Health.TakeDamage(enemy.Health.Current - target); }
return "boss " + brain.State + " hp " + enemy.Health.Fraction.ToString("0.00");
