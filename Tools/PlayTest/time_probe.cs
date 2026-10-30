// Reports the time scale, whether the briefing is still up, the wave countdown and the selected UI object.
var sb = new System.Text.StringBuilder();
sb.Append("timeScale=").Append(UnityEngine.Time.timeScale.ToString("0.0"));
if (Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    sb.Append(" briefing=").Append(context.BriefingPending).Append(" countdown=").Append(context.NextWaveCountdown.ToString("0.0")).Append(" level=").Append(context.Level.name);
}

var selected = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
sb.Append(" selected=").Append(selected != null ? selected.name + (selected.GetComponentInChildren<UnityEngine.UI.Text>() != null ? ":" + selected.GetComponentInChildren<UnityEngine.UI.Text>().text : "") : "none");
sb.Append(" scene=").Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
return sb.ToString();
