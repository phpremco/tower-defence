// Lists the creatures of the menu diorama: name, position, whether alive and visible.
var sb = new System.Text.StringBuilder();
foreach (var creature in UnityEngine.Object.FindObjectsByType<Game.UI.MenuCreature>(UnityEngine.FindObjectsSortMode.None))
{
    var enemy = creature.GetComponent<Game.Enemies.Enemy>();
    var renderer = creature.GetComponentInChildren<UnityEngine.Renderer>();
    sb.Append(creature.name + "@" + creature.transform.position.ToString("0.0") + " alive=" + enemy.IsAlive + " visible=" + (renderer != null && renderer.enabled) + "; ");
}
return sb.ToString();
