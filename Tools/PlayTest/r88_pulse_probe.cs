// Round 88a: when a power sound starts against its picture. Tools/PlayTest/r88_pulse_arg.txt:
//   "hero"        uses the hero's Shockwave and reports, in the same frame, the voice that plays its sound: how far into the
//                 recording it started (the swell skipped), the set's measured blow and the device's delay.
//   "boss <Name>" steps up to 900 frames with the named boss alive and reports the frame its pulse sound starts (seconds
//                 until the pulse then), and whether a second copy starts when the pulse itself fires.
string[] arg = System.IO.File.ReadAllText("Tools/PlayTest/r88_pulse_arg.txt").Trim().Split(' ');
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var inv = System.Globalization.CultureInfo.InvariantCulture;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var player = UnityEngine.Object.FindAnyObjectByType<Game.Boot.SfxPlayer>();
if (player == null) { return "no sfx player"; }
var voices = (UnityEngine.AudioSource[])typeof(Game.Boot.SfxPlayer).GetField("_voices", flags).GetValue(player);
float latency = (float)typeof(Game.Boot.SfxPlayer).GetField("_outputLatency", flags).GetValue(player);
System.Func<string, UnityEngine.AudioSource> playing = part =>
{
    foreach (var v in voices) { if (v.isPlaying && v.clip != null && v.clip.name.Contains(part)) { return v; } }
    return null;
};
if (arg[0] == "hero")
{
    var hero = context.Hero;
    int slot = -1;
    for (int i = 0; i < hero.Abilities.Count; i++) { if (hero.Abilities[i].Definition.Kind == Game.Hero.HeroAbilityKind.Shockwave) { slot = i; } }
    if (slot < 0) { return "the hero has no shockwave"; }
    context.Wallet.Add(1000);
    bool used = hero.TryUse(slot);
    var voice = playing("hero_power_pulse");
    var library = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.SfxLibrary>("Assets/_Project/Data/Audio/SfxLibrary.asset");
    return "used " + used + "; voice " + (voice != null ? "started " + voice.time.ToString("0.000", inv) + " s into " + voice.clip.name : "none playing (audio off, or scheduled)")
        + "; blow at " + library.HeroPulses.Impact.ToString("0.000", inv) + " s, wave leaves after " + Game.Hero.HeroArtView.AbilityLeadSeconds.ToString("0.00", inv) + " s, device delay " + latency.ToString("0.000", inv) + " s, time scale " + UnityEngine.Time.timeScale.ToString("0.0", inv);
}
Game.Enemies.Enemy boss = null;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.name == arg[1]) { boss = enemy; }
}
if (boss == null) { return "no living " + arg[1]; }
var abilities = boss.GetComponent<Game.Enemies.BossAbilities>();
int pulses = 0;
abilities.Pulsed += (c, r, col) => pulses++;
var sb = new System.Text.StringBuilder();
UnityEditor.EditorApplication.isPaused = true;
bool wasPlaying = false;
int starts = 0;
for (int frame = 0; frame < 900 && pulses < 2; frame++)
{
    float until = abilities.SecondsUntil(Game.Enemies.BossAbilityKind.Pulse);
    int before = pulses;
    UnityEditor.EditorApplication.Step();
    var voice = playing("power_pulse");
    bool now = voice != null && voice.time < 0.2f;
    if (now && !wasPlaying) { starts++; sb.Append("sound starts at frame ").Append(frame).Append(" with ").Append(until.ToString("0.00", inv)).Append(" s to the pulse; "); }
    wasPlaying = voice != null;
    if (pulses > before) { sb.Append("pulse fires at frame ").Append(frame).Append(voice != null ? ", sound " + voice.time.ToString("0.00", inv) + " s in; " : ", no sound playing; "); }
}
return sb + "sound starts " + starts + ", pulses " + pulses + ", device delay " + latency.ToString("0.000", inv);
