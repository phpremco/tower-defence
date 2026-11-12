// Round 57: for the first living enemy named in Tools/PlayTest/enemy_arg.txt: the clip on top and its time, the moment's
// spark emitter (EnemyView._impact: where it stands, how many sparks are alive), the power clip's socket bone and the point
// its data offset puts the moment at (bone.TransformPoint(SocketOffset)), each also on screen (camera pixels).
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
var camera = UnityEngine.Camera.main;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != wanted) { continue; }
    var view = enemy.GetComponentInChildren<Game.Enemies.EnemyView>(true);
    var model = (Game.Enemies.CreatureModel)typeof(Game.Enemies.EnemyView).GetField("_model", flags).GetValue(view);
    var impact = (UnityEngine.ParticleSystem)typeof(Game.Enemies.EnemyView).GetField("_impact", flags).GetValue(view);
    var art = model.Art;
    var player = model.Player;
    string once = player.Once >= 0 ? art.Clips[player.Once].Clip.name + " t=" + player.OnceTime.ToString("0.000") : "none";
    int power = art.IndexOfTrigger("Spawn");
    string socket = "no Spawn clip";
    if (power >= 0)
    {
        var clip = art.Clips[power];
        var bone = model.Socket(clip.Socket);
        var point = bone.TransformPoint(clip.SocketOffset);
        socket = clip.Socket + " bone " + bone.position.ToString("0.00") + " moment " + point.ToString("0.00")
            + " screen bone " + camera.WorldToScreenPoint(bone.position).ToString("0") + " moment " + camera.WorldToScreenPoint(point).ToString("0");
    }

    string sparks = impact != null ? "impact at " + impact.transform.position.ToString("0.00") + " alive " + impact.particleCount + " screen " + camera.WorldToScreenPoint(impact.transform.position).ToString("0") : "no impact";
    return wanted + ": once " + once + " | " + socket + " | " + sparks;
}

return "no living " + wanted;
