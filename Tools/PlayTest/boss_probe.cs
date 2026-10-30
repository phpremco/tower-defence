// Reports the boss marker (arrow and label), the approach sounds playing and the boss itself (scale, light, glow).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var sb = new System.Text.StringBuilder();
sb.Append("bossWave=").Append(context.BossWave).Append(" bossesAlive=").Append(context.BossesAlive);
var arrow = UnityEngine.GameObject.Find("BossArrow");
var marker = UnityEngine.Object.FindAnyObjectByType<Game.UI.BossMarkerView>();
sb.Append(" marker=").Append(marker != null);
foreach (var rect in UnityEngine.Object.FindObjectsByType<UnityEngine.RectTransform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (rect.name == "BossArrow")
    {
        sb.Append(" arrowActive=").Append(rect.gameObject.activeSelf).Append(" pos=").Append(rect.anchoredPosition.ToString("0")).Append(" rot=").Append(rect.localEulerAngles.z.ToString("0"));
    }
}

var sfx = UnityEngine.Object.FindAnyObjectByType<Game.Boot.SfxPlayer>();
var voices = (UnityEngine.AudioSource[])typeof(Game.Boot.SfxPlayer).GetField("_voices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sfx);
sb.Append(" playing=");
foreach (var voice in voices)
{
    if (voice.isPlaying && voice.clip != null)
    {
        sb.Append(voice.clip.name).Append(',');
    }
}

var approach = (UnityEngine.AudioClip)typeof(Game.Boot.SfxPlayer).GetField("_approach", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sfx);
sb.Append(" approachClip=").Append(approach != null ? approach.name : "none");
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.IsBoss)
    {
        var light = enemy.GetComponentInChildren<UnityEngine.Light>();
        var pulse = enemy.GetComponentInChildren<Game.Core.EmissionPulse>();
        sb.Append(" boss=").Append(enemy.Definition.name).Append(" at ").Append(enemy.Position.ToString("0.0")).Append(" hp=").Append(enemy.Health.Current.ToString("0")).Append(" light=").Append(light != null ? light.intensity.ToString("0.0") : "none").Append(" pulse=").Append(pulse != null);
        foreach (var r in enemy.GetComponentsInChildren<UnityEngine.MeshRenderer>())
        {
            if (r.name == "Shell")
            {
                sb.Append(" shellScale=").Append(r.transform.lossyScale.x.ToString("0.00"));
                break;
            }
        }
    }
}

return sb.ToString();
