// Round 78: reads the next-wave preview of the running level. Tools/PlayTest/r78_preview_arg.txt holds "probe" (wave, the
// preview's entries as the HUD lists them, how many have a chip, the strip's place on the canvas), "list" (the preview of
// every wave of the level on its difficulty) or "fast" (the developer fast-forward, then every living creature is killed,
// so the level can be walked to a wave without losing it).
string arg = System.IO.File.ReadAllText("Tools/PlayTest/r78_preview_arg.txt").Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no level";
}

var text = new System.Text.StringBuilder();
var entries = new System.Collections.Generic.List<Game.Levels.WavePreviewEntry>();
System.Action<Game.Levels.WaveDefinition> describe = wave =>
{
    Game.Levels.WavePreview.Collect(wave, context.Difficulty, entries);
    foreach (var entry in entries)
    {
        text.Append(' ').Append(entry.Enemy.name).Append('x').Append(entry.Count).Append(entry.Boss ? "[boss]" : "").Append(entry.Flies ? "[air]" : "").Append(entry.Armoured ? "[plate]" : "");
    }
};

if (arg == "list")
{
    var waves = context.Level.GetWaves(context.Difficulty);
    for (int w = 0; w < waves.Length; w++)
    {
        text.Append("w").Append(w + 1).Append(':');
        describe(waves[w]);
        text.Append(" | ");
    }

    return text.ToString();
}

if (arg == "fast")
{
    context.FastForwardWaveRequested.Raise();
    foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
    {
        if (enemy.IsAlive)
        {
            enemy.TakeDamage(1000000f);
        }
    }
}

var hud = UnityEngine.Object.FindAnyObjectByType<Game.UI.HudView>();
var view = hud != null ? hud.WavePreview : null;
text.Append("wave ").Append(context.WaveNumber).Append('/').Append(context.WaveCount).Append(" countdown ").Append(context.NextWaveCountdown.ToString("0.0")).Append(" next:");
describe(context.NextWave);
if (view != null)
{
    text.Append(" | view visible ").Append(view.Visible).Append(" entries ").Append(view.Entries.Count).Append(" with a chip ").Append(view.ShownEntries);
    var strip = UnityEngine.GameObject.Find("WavePreview");
    if (strip != null)
    {
        var rect = (UnityEngine.RectTransform)strip.transform;
        var corners = new UnityEngine.Vector3[4];
        float right = 0f;
        foreach (UnityEngine.RectTransform child in rect)
        {
            if (child.gameObject.activeSelf)
            {
                child.GetWorldCorners(corners);
                right = UnityEngine.Mathf.Max(right, corners[2].x);
            }
        }

        rect.GetWorldCorners(corners);
        var icons = (UnityEngine.RectTransform)UnityEngine.GameObject.Find("Icons").transform;
        var iconCorners = new UnityEngine.Vector3[4];
        icons.GetWorldCorners(iconCorners);
        text.Append(" strip x ").Append(corners[0].x.ToString("0")).Append("..").Append(right.ToString("0")).Append(" icons from ").Append(iconCorners[0].x.ToString("0")).Append(" screen ").Append(UnityEngine.Screen.width).Append('x').Append(UnityEngine.Screen.height);
    }
}

return text.ToString();
