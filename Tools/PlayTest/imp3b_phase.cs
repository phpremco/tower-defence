// Import stage 3b screenshot phases for the hero, one CLI call each (the editor is held paused between them, as in
// imp2_phase.cs). Tools/PlayTest/imp3_phase.txt holds one phase:
//   setup                 waves off
//   view <ortho>          the camera on the hero at that ortho half height
//   foe <kind> <dist>     one creature of that kind walked down the road until it is within that distance of the hero
//   ability <slot>        uses the hero's ability in that slot (cooldown and money ignored)
//   clear                 kills every creature and heals the hero
//   down | revive | revive         knocks the hero down, or cuts the revive timer short
//   move <post>           sends the hero to a post
//   play | pause          lets the level run in real time, or holds it
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/imp3_phase.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
if (args[0] != "play") UnityEditor.EditorApplication.isPaused = true;
var report = new System.Text.StringBuilder();
var hero = context.Hero;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
if (args[0] == "setup")
{
    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    controller.enabled = false;
    report.Append("hero at ").Append(hero.transform.position.ToString("0.0")).Append(" art ").Append(hero.GetComponent<Game.Hero.HeroArtView>() != null).Append("; ");
}
else if (args[0] == "view")
{
    var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
    var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
    if (pan != null) pan.enabled = false;
    float fit = UnityEngine.Camera.main.orthographicSize * rig.Zoom;
    rig.SetView(hero.transform.position + UnityEngine.Vector3.up * 0.8f, fit / float.Parse(args[1], inv));
}
else if (args[0] == "foe")
{
    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    var spawner = (Game.Levels.EnemySpawner)typeof(Game.Levels.LevelController).GetField("_spawner", flags).GetValue(controller);
    var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + args[1] + ".asset");
    spawner.Spawn(enemy, 0, Game.Levels.SpawnSource.Gate);
    float within = float.Parse(args[2], inv);
    foreach (var walker in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
    {
        for (int i = 0; i < 4000 && UnityEngine.Vector3.Distance(walker.transform.position, hero.transform.position) > within; i++) walker.Step(0.02f);
        report.Append(walker.name).Append(" at ").Append(UnityEngine.Vector3.Distance(walker.transform.position, hero.transform.position).ToString("0.0")).Append(' ');
    }
}
else if (args[0] == "ability")
{
    var state = hero.AbilityAt(int.Parse(args[1]));
    context.Wallet.Add(10000);
    if (state != null) typeof(Game.Hero.HeroAbilityState).GetProperty("Remaining")?.SetValue(state, 0f);
    report.Append("used ").Append(hero.TryUse(int.Parse(args[1]))).Append(' ').Append(state != null ? state.Definition.name : "none");
}
else if (args[0] == "clear")
{
    foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>()) if (enemy.IsAlive) enemy.TakeDamage(1000000f);
    hero.Health.Heal(hero.Health.Max);
}
else if (args[0] == "down") hero.Health.TakeDamage(hero.Health.Max * 2f);
else if (args[0] == "revive") typeof(Game.Hero.Commander).GetField("_reviveRemaining", flags).SetValue(hero, 0.01f);
else if (args[0] == "move") hero.MoveTo(int.Parse(args[1]));

if (args[0] == "play") UnityEditor.EditorApplication.isPaused = false;
return report.Append(args[0]).ToString();
