// Round 49 import (set 3): for the first living enemy named in Tools/PlayTest/enemy_arg.txt, where its health bar sits
// against the top of its drawn body (near-LOD skinned renderers) and its head bone, and the travel cycle's playback rate
// (CreatureClips' clamp is 0.15-6; a rate pinned at either end means the feet slide).
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
foreach (var view in UnityEngine.Object.FindObjectsByType<Game.Enemies.EnemyView>(UnityEngine.FindObjectsSortMode.None))
{
    var enemy = view.GetComponent<Game.Enemies.Enemy>();
    if (enemy == null || !enemy.IsAlive || enemy.Definition.name != wanted) { continue; }
    float barY = float.NaN;
    foreach (var node in view.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        if (node.name == "HealthBar") { barY = node.position.y; break; }
    }
    float top = float.MinValue;
    float headY = float.NaN;
    foreach (var renderer in view.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        if (!renderer.enabled || renderer.name.EndsWith("_Far")) { continue; }
        top = UnityEngine.Mathf.Max(top, renderer.bounds.max.y);
    }

    foreach (var node in view.GetComponentsInChildren<UnityEngine.Transform>())
    {
        if (node.name == "Head" && float.IsNaN(headY)) { headY = node.position.y; }
    }

    var clips = typeof(Game.Enemies.EnemyView).GetField("_clips", flags).GetValue(view);
    string rate = "n/a";
    if (clips != null)
    {
        var player = clips.GetType().GetField("_player", flags).GetValue(clips);
        var cycle = player.GetType().GetProperty("CycleRate");
        if (cycle != null) { rate = ((float)cycle.GetValue(player)).ToString("0.00"); }
    }

    return $"{wanted}: ground {view.transform.position.y:0.00} bar {barY:0.00} bodyTop {top:0.00} head {headY:0.00} barAboveTop {barY - top:0.00} cycleRate {rate}";
}

return "no living " + wanted;
