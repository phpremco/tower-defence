// Turns shadow casting off for every renderer of the first living enemy named in enemy_arg.txt, to tell shadows from geometry.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
int count = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != wanted) continue;
    foreach (var r in enemy.GetComponentsInChildren<UnityEngine.Renderer>())
    {
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        count++;
    }
}
return "shadows off on " + count + " renderers";
