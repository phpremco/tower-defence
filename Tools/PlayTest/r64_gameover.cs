// Round 64: the level's end. Mode from Tools/PlayTest/r64_gameover_arg.txt:
//  "fast <scale>"  sets the game speed (to get creatures on the road quickly);
//  "lose"          takes the base's last hit points (the defeat);
//  "win"           kills every living creature and ends the level as won through the level controller's own check
//                  (it marks every wave as sent first);
//  "probe"         reports the clock and what still moves: time scale, game time, the summed position of the living
//                  creatures and projectiles (two probes a few real seconds apart must agree once the field is frozen),
//                  whether the result panel is up, and every looping sound still audible.
string[] overArgs = System.IO.File.ReadAllText("Tools/PlayTest/r64_gameover_arg.txt").Trim().Split(' ');
var overInv = System.Globalization.CultureInfo.InvariantCulture;
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext overContext)) return "no level";
if (overArgs[0] == "fast")
{
    // Never after the level is decided: the result screen owns the clock then, and this probe is there to watch it.
    if (overContext.Outcome != Game.Levels.LevelOutcome.Running) return "level already " + overContext.Outcome + ", clock left alone";
    UnityEngine.Time.timeScale = float.Parse(overArgs[1], overInv);
    return "time scale " + UnityEngine.Time.timeScale;
}

if (overArgs[0] == "lose")
{
    overContext.BaseHealth.TakeDamage(overContext.BaseHealth.Current + 1f);
    return "base down, outcome " + overContext.Outcome;
}

if (overArgs[0] == "win")
{
    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    typeof(Game.Levels.LevelController).GetMethod("Finish", flags).Invoke(controller, new object[] { Game.Levels.LevelOutcome.Won });
    return "outcome " + overContext.Outcome;
}

double sum = 0;
int living = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.isActiveAndEnabled || !enemy.IsAlive) continue;
    living++;
    sum += enemy.transform.position.x * 1.37 + enemy.transform.position.z;
}

int shots = 0;
foreach (var projectile in UnityEngine.Object.FindObjectsByType<Game.Combat.Projectile>(UnityEngine.FindObjectsSortMode.None))
{
    if (!projectile.isActiveAndEnabled) continue;
    shots++;
    sum += projectile.transform.position.x * 0.71 + projectile.transform.position.z * 1.13;
}

foreach (var drone in UnityEngine.Object.FindObjectsByType<Game.Drones.Drone>(UnityEngine.FindObjectsSortMode.None))
{
    sum += drone.transform.position.x + drone.transform.position.y + drone.transform.position.z;
}

var loud = new System.Collections.Generic.List<string>();
foreach (var source in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None))
{
    if (source.isPlaying && source.loop && source.volume > 0.02f) loud.Add((source.clip != null ? source.clip.name : source.name) + " " + source.volume.ToString("0.00", overInv));
}

var panel = UnityEngine.GameObject.Find("ResultPanel");
return "outcome " + overContext.Outcome + " timeScale " + UnityEngine.Time.timeScale.ToString("0.000", overInv) + " gameTime " + UnityEngine.Time.time.ToString("0.000", overInv)
    + " wave " + overContext.WaveNumber + " living " + living + " shots " + shots + " positions " + sum.ToString("0.0000", overInv)
    + " panel " + (panel != null && panel.activeInHierarchy) + " loops: " + (loud.Count > 0 ? string.Join(", ", loud) : "none");
