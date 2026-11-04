// VFX round screenshot phases (after imp2_phase.cs set the lineup up). Tools/PlayTest/vfx_phase.txt holds one phase:
//   burst <impact name> <radius> [dx dz]   plays an impact from Data/Vfx/Vfx_<name>.asset at the lineup centre (+ offset)
//   boss <enemy asset>                     spawns a boss, walks it to the lineup and kills it (the chain explosion path)
//   time <scale>                           sets the time scale (slow motion for short effects)
//   probe                                  counts the live effect particles and the painted cards
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/vfx_phase.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var parent = UnityEngine.GameObject.Find("Imp2Towers");
UnityEngine.Vector3 centre = UnityEngine.Vector3.zero;
int towers = 0;
if (parent != null)
{
    foreach (UnityEngine.Transform child in parent.transform) { centre += child.position; towers++; }
}

if (towers > 0) centre /= towers;
if (args[0] == "burst")
{
    var vfx = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Combat.ImpactVfxDefinition>("Assets/_Project/Data/Vfx/Vfx_" + args[1] + ".asset");
    if (vfx == null) return "no impact " + args[1];
    float radius = float.Parse(args[2], inv);
    UnityEngine.Vector3 at = centre + (args.Length > 4 ? new UnityEngine.Vector3(float.Parse(args[3], inv), 0f, float.Parse(args[4], inv)) : UnityEngine.Vector3.zero);
    at.y = 0f;
    context.Projectiles.PlayBurst(at, radius, vfx);
    return "burst " + args[1] + " r" + radius + " at " + at.ToString("0.0");
}

if (args[0] == "boss")
{
    var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + args[1] + ".asset");
    if (definition == null) return "no enemy " + args[1];
    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    var field = typeof(Game.Levels.LevelController).GetField("_spawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var spawner = (Game.Levels.EnemySpawner)field.GetValue(controller);
    spawner.Spawn(definition, 0, Game.Levels.SpawnSource.Gate);
    foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
    {
        if (!enemy.IsAlive || enemy.Definition != definition) continue;
        // Walk it along its route until it is level with the lineup, then kill it there.
        float best = float.MaxValue;
        for (int i = 0; i < 600; i++)
        {
            float d = (enemy.Position - centre).sqrMagnitude;
            if (d > best && d < 64f) break;
            best = UnityEngine.Mathf.Min(best, d);
            enemy.Step(0.1f);
        }

        UnityEngine.Vector3 at = enemy.Position;
        enemy.TakeDamage(enemy.Health.Max * 10f);
        return "boss " + definition.name + " killed at " + at.ToString("0.0") + " alive=" + enemy.IsAlive;
    }

    return "boss did not spawn";
}

if (args[0] == "wave")
{
    // A fresh group of walkers and flyers, walked in until the first of them is near the lineup.
    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    var field = typeof(Game.Levels.LevelController).GetField("_spawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var spawner = (Game.Levels.EnemySpawner)field.GetValue(controller);
    string[] kinds = { "Cragback", "Skitter", "Prowler", "Skyray", "Shalebat", "Cragback", "Skitter", "Skyray", "Prowler", "Cragback" };
    foreach (string kind in kinds)
    {
        var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + kind + ".asset");
        if (definition != null) spawner.Spawn(definition, 0, Game.Levels.SpawnSource.Gate);
    }

    int steps = int.Parse(args[1]);
    int k = 0;
    foreach (var walker in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
    {
        if (!walker.IsAlive) continue;
        // Spread them out so the lineup has targets for a while.
        for (int i = 0; i < steps - (k % 5) * 6; i++) walker.Step(0.1f);
        k++;
    }

    return "wave of " + kinds.Length + ", alive " + spawner.Alive;
}

if (args[0] == "time")
{
    UnityEngine.Time.timeScale = float.Parse(args[1], inv);
    return "time " + UnityEngine.Time.timeScale;
}

int particles = 0;
int systems = 0;
foreach (var system in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>(UnityEngine.FindObjectsSortMode.None))
{
    var renderer = system.GetComponent<UnityEngine.ParticleSystemRenderer>();
    if (system.particleCount == 0 || renderer == null || renderer.sharedMaterial == null || !renderer.sharedMaterial.shader.name.StartsWith("Game/Vfx")) continue;
    particles += system.particleCount;
    systems++;
}

int muzzles = 0;
foreach (var muzzle in UnityEngine.Object.FindObjectsByType<Game.Combat.MuzzleFlashView>(UnityEngine.FindObjectsSortMode.None)) muzzles++;
int cards = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (r.sharedMaterial != null && r.sharedMaterial.shader.name.StartsWith("Game/Vfx")) cards++;
}

return "painted particles " + particles + " in " + systems + " systems, muzzle views " + muzzles + ", active cards " + cards + ", camera.main=" + (UnityEngine.Camera.main != null);
