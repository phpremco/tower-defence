// Round 88c: what a still of a boss fight shows, in numbers. Tools/PlayTest/r88c_field_arg.txt holds words:
//   "state"       the level's outcome, the wave clock and every living creature (boss first) with its place on its road
//   "bolts"       how many enemy bolts are in the air right now, and the health of every tower
//   "killboss"    kills every living boss (to check that the level goes on while its brood lives)
//   "killall"     kills everything alive
//   "purse"       the wallet, and the air raid alert line when it shows
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c)) return "no context";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string[] words = System.IO.File.ReadAllText("Tools/PlayTest/r88c_field_arg.txt").Trim().Split(' ');
var text = new System.Text.StringBuilder();
var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var spawner = (Game.Levels.EnemySpawner)typeof(Game.Levels.LevelController).GetField("_spawner", flags).GetValue(controller);
foreach (string word in words)
{
    if (word == "state")
    {
        text.Append("outcome ").Append(c.Outcome).Append(", wave ").Append(c.WaveNumber).Append(", alive ").Append(spawner.Alive).Append(", bosses ").Append(c.BossesAlive).Append(": ");
        Game.Enemies.Enemy boss = null;
        foreach (var e in spawner.Living) if (e.Definition.IsBoss) boss = e;
        foreach (var e in spawner.Living)
        {
            float gap = boss != null ? e.PathProgress - boss.PathProgress : 0f;
            text.Append(e.Definition.name).Append(e == boss ? " BOSS" : string.Empty).Append(" at ").Append(e.PathProgress.ToString("0.0", inv)).Append(boss != null && e != boss ? " (" + gap.ToString("+0.0;-0.0", inv) + ")" : string.Empty).Append(e.IsHasted ? " running" : string.Empty).Append("; ");
        }
    }
    else if (word == "bolts")
    {
        int flying = 0;
        foreach (var bolt in UnityEngine.Object.FindObjectsByType<Game.Enemies.EnemyBoltView>(UnityEngine.FindObjectsSortMode.None)) flying += bolt.InFlight ? 1 : 0;
        text.Append("bolts in flight ").Append(flying).Append("; towers:");
        foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None))
        {
            if (!tower.Definition.BlocksGroundEnemies) text.Append(' ').Append(tower.Definition.name).Append(' ').Append(tower.Health.Current.ToString("0", inv)).Append('/').Append(tower.Health.Max.ToString("0", inv));
        }
        text.Append("; ");
    }
    else if (word == "killboss" || word == "killall")
    {
        var doomed = new System.Collections.Generic.List<Game.Enemies.Enemy>();
        foreach (var e in spawner.Living) if (word == "killall" || e.Definition.IsBoss) doomed.Add(e);
        foreach (var e in doomed) e.TakeDamage(1e9f);
        text.Append(word).Append(' ').Append(doomed.Count).Append("; ");
    }
    else if (word == "nogates")
    {
        // Wrecks every gate, so a boss spawned by hand walks the whole road for a still.
        int gates = 0;
        foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None))
        {
            if (tower.Definition.BlocksGroundEnemies && !tower.IsWrecked) { tower.TakeDamage(1e9f); gates++; }
        }
        text.Append("gates wrecked ").Append(gates).Append("; ");
    }
    else if (word.StartsWith("wavescale:"))
    {
        // Creatures spawned by hand walk in as tough as that wave of the level (1-based) would send them.
        var waves = c.Level.GetWaves(c.Difficulty);
        var wave = waves[int.Parse(word.Substring(10)) - 1];
        spawner.WaveHealthScale = wave.HealthMultiplier;
        spawner.WaveBossHealthScale = wave.BossHealthScale;
        text.Append("wave scale ").Append(wave.HealthMultiplier.ToString("0.00", inv)).Append(" boss ").Append(wave.BossHealthScale.ToString("0.00", inv)).Append("; ");
    }
    else if (word.StartsWith("gotowave:"))
    {
        // The wave timeline jumps: the given wave (1-based) starts on the next frame, as stored. For stills of a late wave.
        int number = int.Parse(word.Substring(9));
        var clock = typeof(Game.Levels.LevelController).GetField("_clock", flags).GetValue(controller);
        typeof(Game.Levels.LevelController).GetField("_scheduler", flags).SetValue(controller, null);
        var clockType = typeof(Game.Levels.WaveClock);
        clockType.GetField("<Started>k__BackingField", flags).SetValue(clock, number - 1);
        clockType.GetField("<Spawning>k__BackingField", flags).SetValue(clock, false);
        clockType.GetField("<Countdown>k__BackingField", flags).SetValue(clock, 0.05f);
        text.Append("next wave ").Append(number).Append("; ");
    }
    else if (word == "healbase")
    {
        // The base is mended in full, so a check that needs the level to keep running is not ended by a leak.
        c.BaseHealth.Heal(1e9f);
        text.Append("base ").Append(c.BaseHealth.Current.ToString("0", inv)).Append("; ");
    }
    else if (word == "waveson")
    {
        controller.enabled = true;
        text.Append("waves on; ");
    }
    else if (word == "diag")
    {
        text.Append(controller.SpawnDiagnostic()).Append("; ");
    }
    else if (word == "purse")
    {
        text.Append("money ").Append(c.Wallet.Money);
        var alert = UnityEngine.Object.FindAnyObjectByType<Game.UI.AirRaidAlertView>();
        text.Append(alert != null && alert.IsShowing ? ", alert: " + alert.Line : ", no alert").Append("; ");
    }
}
return text.ToString();
