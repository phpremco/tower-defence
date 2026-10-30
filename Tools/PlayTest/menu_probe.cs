// Reports what the main menu scene contains: the stage component and its asset, every camera, and the diorama.
var sb = new System.Text.StringBuilder();
var stage = UnityEngine.Object.FindAnyObjectByType<Game.Boot.MenuStage>();
sb.Append("stage=" + (stage == null ? "none" : stage.name));
if (stage != null)
{
    var field = typeof(Game.Boot.MenuStage).GetField("_stage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var asset = field.GetValue(stage) as Game.UI.MenuStageDefinition;
    sb.Append(" asset=" + (asset == null ? "null" : asset.name + " complete=" + asset.IsComplete));
}
foreach (var cam in UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsSortMode.None))
{
    sb.Append(" cam:" + cam.name + "@" + cam.transform.position.ToString("0.0") + " tag=" + cam.tag);
}
var diorama = UnityEngine.Object.FindAnyObjectByType<Game.UI.MenuDiorama>();
sb.Append(" diorama=" + (diorama == null ? "none" : diorama.transform.childCount + " children"));
sb.Append(" scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
sb.Append(" services=" + Game.Core.Services.TryGet(out Game.Platform.IPlatformSettings _) + "/" + Game.Core.Services.TryGet(out Game.Core.AudioMix _));
return sb.ToString();
