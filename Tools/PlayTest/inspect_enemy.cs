// Lists the renderers of the first living enemy named in enemy_arg.txt with their world bounds, to find oversized parts.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
var sb = new System.Text.StringBuilder();
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != wanted) continue;
    foreach (var r in enemy.GetComponentsInChildren<UnityEngine.Renderer>())
    {
        var b = r.bounds;
        sb.Append(r.name).Append(' ').Append(b.size.ToString("0.00")).Append(" scale ").Append(r.transform.lossyScale.ToString("0.00")).Append(" | ");
    }
    return sb.ToString();
}
return "no " + wanted;
