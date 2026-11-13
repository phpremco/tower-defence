// Round 58: for every living boss with a laser, the horizontal angle between the body's forward (the view's _body) and the
// beam (socket to strike point, and the beam's first segment), the angle between the spade's axis (socket up) and the beam's
// first segment in 3D, the body yaw and the yaw to the laser's strike point, and the laser state.
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder();
System.Func<UnityEngine.Vector3, UnityEngine.Vector3> flat = v => new UnityEngine.Vector3(v.x, 0f, v.z);
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    var abilities = enemy.IsAlive ? enemy.GetComponent<Game.Enemies.BossAbilities>() : null;
    if (abilities == null) { continue; }
    var view = enemy.GetComponent<Game.Enemies.EnemyView>();
    var body = (UnityEngine.Transform)typeof(Game.Enemies.EnemyView).GetField("_body", flags).GetValue(view);
    var socket = view.FindBeamSocket();
    var forward = flat(body.forward);
    sb.Append(enemy.Definition.name).Append(" laser=").Append(abilities.LaserActive).Append(" aiming=").Append(view.IsAimingBeam);
    var pending = typeof(Game.Enemies.BossAbilities).GetField("_pendingLaserTarget", flags).GetValue(abilities) as Game.Combat.IStructure;
    sb.Append(" turning=").Append(typeof(Game.Enemies.EnemyView).GetField("_aiming", flags).GetValue(view)).Append(" due=").Append(abilities.SecondsUntil(Game.Enemies.BossAbilityKind.Laser).ToString("0.00"));
    sb.Append(" pending=").Append(pending != null ? pending.Position.ToString("0.0") : "none");
    sb.Append(" bodyYaw=").Append(UnityEngine.Mathf.Atan2(forward.x, forward.z) * UnityEngine.Mathf.Rad2Deg).Append(" heading=");
    var heading = flat(enemy.Heading);
    sb.Append(heading.sqrMagnitude > 0f ? (UnityEngine.Mathf.Atan2(heading.x, heading.z) * UnityEngine.Mathf.Rad2Deg).ToString("0.0") : "-");
    var toEnd = flat(abilities.LaserEnd - enemy.Position);
    sb.Append(" yawToStrike=").Append((UnityEngine.Mathf.Atan2(toEnd.x, toEnd.z) * UnityEngine.Mathf.Rad2Deg).ToString("0.0"));
    foreach (var line in enemy.transform.parent.GetComponentsInChildren<UnityEngine.LineRenderer>(false))
    {
        if (line.name != "BossBeam" || !line.enabled) { continue; }
        int n = line.positionCount;
        var start = line.GetPosition(0);
        var next = line.GetPosition(1);
        var end = line.GetPosition(n - 1);
        sb.Append(" | body-vs-beam(chord) ").Append(UnityEngine.Vector3.Angle(forward, flat(end - start)).ToString("0.0"));
        sb.Append(" body-vs-beam(first seg) ").Append(UnityEngine.Vector3.Angle(forward, flat(next - start)).ToString("0.0"));
        if (socket != null)
        {
            sb.Append(" spadeAxis-vs-firstSeg(3D) ").Append(UnityEngine.Vector3.Angle(socket.up, next - start).ToString("0.0"));
            sb.Append(" start-socket ").Append((start - socket.position).magnitude.ToString("0.000"));
        }
    }

    sb.Append("; ");
}

return sb.Length == 0 ? "no boss" : sb.ToString();
