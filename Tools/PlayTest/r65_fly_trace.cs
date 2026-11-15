// Round 65: steps the paused editor and records where every living flyer is, which way its body faces and how far it
// is rolled, every third frame. Tools/PlayTest/r65_fly_arg.txt: "<frames>" (default 60). Appends to Logs/r65/fly_trace.csv
// (time,id,name,x,z,headingYaw,bodyYaw,bodyRoll). Run it in a loop: one call may only take a few seconds.
var inv = System.Globalization.CultureInfo.InvariantCulture;
int frames = System.IO.File.Exists("Tools/PlayTest/r65_fly_arg.txt") ? int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r65_fly_arg.txt").Trim()) : 60;
var bodyField = typeof(Game.Enemies.EnemyView).GetField("_body", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var sb = new System.Text.StringBuilder();
int flyers = 0; float worstRoll = 0f;
UnityEditor.EditorApplication.isPaused = true;
for (int f = 0; f < frames; f++)
{
    UnityEditor.EditorApplication.Step();
    if (f % 3 != 0) continue;
    flyers = 0;
    foreach (var e in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
    {
        if (!e.isActiveAndEnabled || !e.IsAlive || e.Definition == null || !e.Definition.Flies) continue;
        flyers++;
        var view = e.GetComponent<Game.Enemies.EnemyView>();
        var body = view != null ? (UnityEngine.Transform)bodyField.GetValue(view) : null;
        float yaw = UnityEngine.Mathf.Atan2(e.Heading.x, e.Heading.z) * UnityEngine.Mathf.Rad2Deg;
        float bodyYaw = body != null ? body.eulerAngles.y : 0f;
        float roll = body != null ? UnityEngine.Mathf.DeltaAngle(0f, body.eulerAngles.z) : 0f;
        worstRoll = UnityEngine.Mathf.Max(worstRoll, UnityEngine.Mathf.Abs(roll));
        sb.Append(UnityEngine.Time.time.ToString("0.00", inv)).Append(',').Append(e.transform.GetSiblingIndex()).Append(',').Append(e.Definition.name).Append(',')
          .Append(e.Position.x.ToString("0.000", inv)).Append(',').Append(e.Position.z.ToString("0.000", inv)).Append(',')
          .Append(yaw.ToString("0.0", inv)).Append(',').Append(bodyYaw.ToString("0.0", inv)).Append(',').Append(roll.ToString("0.0", inv)).Append('\n');
    }
}
System.IO.Directory.CreateDirectory("Logs/r65");
System.IO.File.AppendAllText("Logs/r65/fly_trace.csv", sb.ToString());
return "flyers " + flyers + " most roll " + worstRoll.ToString("0.0", inv) + " time " + UnityEngine.Time.time.ToString("0.0", inv);
