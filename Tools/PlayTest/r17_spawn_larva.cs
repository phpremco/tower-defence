// Round 17: puts three FireLarvae on the continent boss's road just ahead of it (EnemySpawner.SpawnMinion, found on
// whichever behaviour holds the level's spawner), so a larva can be filmed crawling without waiting for an egg.
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain == null) { return "no boss"; }
Game.Levels.EnemySpawner spawner = null;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
foreach (var behaviour in UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>())
{
    foreach (var f in behaviour.GetType().GetFields(flags))
    {
        if (f.FieldType == typeof(Game.Levels.EnemySpawner)) { spawner = (Game.Levels.EnemySpawner)f.GetValue(behaviour); }
        if (spawner != null) { break; }
    }
    if (spawner != null) { break; }
}
if (spawner == null) { return "no spawner"; }
var larva = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/FireLarva.asset");
for (int i = 0; i < 3; i++) { spawner.SpawnMinion(larva, brain.GetComponent<Game.Enemies.Enemy>()); }
return "spawned 3 larvae";
