// Round 37: what the field holds right now: living creatures, flyers among them, the boss, towers, drones, wave, money.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
int alive = 0, air = 0;
foreach (var e in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!e.isActiveAndEnabled || e.Health == null || e.Health.Fraction <= 0f) continue;
    alive++;
    if (e.Definition != null && (e.Definition.Layer & Game.Core.TargetLayers.Air) != 0) air++;
}
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
int towers = UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None).Length;
int drones = UnityEngine.Object.FindObjectsByType<Game.Drones.Drone>(UnityEngine.FindObjectsSortMode.None).Length;
return "wave " + context.WaveNumber + " alive " + alive + " air " + air + " boss " + (brain != null ? brain.State.ToString() : "none") + " towers " + towers + " drones " + drones + " money " + context.Wallet.Money + " t=" + UnityEngine.Time.time.ToString("0");
