// Round 49 import (set 3): for the first living enemy named in Tools/PlayTest/enemy_arg.txt, where its health bar sits
// against the real top of its drawn body (near-LOD skinned renderers, baked vertices in the current pose) and its head
// bone, whether the bar is shown, and the body's tilt (a flyer banks and pitches as it steers).
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
foreach (var view in UnityEngine.Object.FindObjectsByType<Game.Enemies.EnemyView>(UnityEngine.FindObjectsSortMode.None))
{
    var enemy = view.GetComponent<Game.Enemies.Enemy>();
    if (enemy == null || !enemy.IsAlive || enemy.Definition.name != wanted) { continue; }
    float barY = float.NaN;
    bool barShown = false;
    foreach (var node in view.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        if (node.name == "HealthBar") { barY = node.position.y; barShown = node.gameObject.activeInHierarchy; break; }
    }

    float top = float.MinValue;
    float boundsTop = float.MinValue;
    float headY = float.NaN;
    UnityEngine.Transform root = null;
    var baked = new UnityEngine.Mesh();
    foreach (var renderer in view.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        if (!renderer.enabled || renderer.name.EndsWith("_Far")) { continue; }
        if (root == null) { root = renderer.transform.parent; }
        boundsTop = UnityEngine.Mathf.Max(boundsTop, renderer.bounds.max.y);
        renderer.BakeMesh(baked, true);
        var rot = renderer.transform.rotation;
        var pos = renderer.transform.position;
        foreach (var v in baked.vertices) { top = UnityEngine.Mathf.Max(top, (pos + rot * v).y); }
    }

    UnityEngine.Object.Destroy(baked);
    foreach (var node in view.GetComponentsInChildren<UnityEngine.Transform>())
    {
        if (node.name == "Head" && float.IsNaN(headY)) { headY = node.position.y; }
    }

    string tilt = root != null ? root.rotation.eulerAngles.ToString("0") : "-";
    return $"{wanted}: ground {view.transform.position.y:0.00} bar {barY:0.00} shown {barShown} bodyTop {top:0.00} boundsTop {boundsTop:0.00} head {headY:0.00} barAboveTop {barY - top:0.00} tilt {tilt}";
}

return "no living " + wanted;
