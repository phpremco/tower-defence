// Round 51: the boss beam's points on screen (the 1920x1080 still framing): for the core and the halo line, each point's
// pixel position, the turn of the strip at it in degrees and its width in pixels, so a fold or a hairpin near the source
// shows as a large turn or a step backwards. Also the socket's axis on screen against the way to the target.
var camera = UnityEngine.Camera.main;
float aspect = camera.aspect;
camera.aspect = 1920f / 1080f;
var sb = new System.Text.StringBuilder();
foreach (var line in UnityEngine.Object.FindObjectsByType<UnityEngine.LineRenderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (!line.enabled || (line.name != "BossBeam" && line.name != "BossBeamGlow")) { continue; }
    int n = line.positionCount;
    var points = new UnityEngine.Vector3[n];
    line.GetPositions(points);
    float total = 0f;
    for (int i = 1; i < n; i++) { total += UnityEngine.Vector3.Distance(points[i - 1], points[i]); }
    sb.Append(line.name).Append(" n=").Append(n).Append(" len=").Append(total.ToString("0.00")).Append(" w=").Append(line.widthMultiplier.ToString("0.00")).Append(": ");
    float along = 0f;
    UnityEngine.Vector2 previousDir = UnityEngine.Vector2.zero;
    for (int i = 0; i < n && i < 12; i++)
    {
        if (i > 0) { along += UnityEngine.Vector3.Distance(points[i - 1], points[i]); }
        var s = camera.WorldToViewportPoint(points[i]);
        var px = new UnityEngine.Vector2(s.x * 1920f, (1f - s.y) * 1080f);
        float turn = 0f;
        if (i + 1 < n)
        {
            var s2 = camera.WorldToViewportPoint(points[i + 1]);
            var dir = new UnityEngine.Vector2(s2.x * 1920f, (1f - s2.y) * 1080f) - px;
            if (i > 0 && previousDir.sqrMagnitude > 0f && dir.sqrMagnitude > 0f) { turn = UnityEngine.Vector2.SignedAngle(previousDir, dir); }
            previousDir = dir;
        }

        float width = line.widthCurve.Evaluate(total > 0f ? along / total : 0f) * line.widthMultiplier;
        float widthPx = width / (camera.orthographicSize * 2f) * 1080f;
        sb.Append("[").Append(px.x.ToString("0")).Append(",").Append(px.y.ToString("0")).Append(" t").Append(turn.ToString("0")).Append(" w").Append(widthPx.ToString("0")).Append("] ");
    }

    sb.Append(" || ");
}

camera.aspect = aspect;
return sb.ToString();
