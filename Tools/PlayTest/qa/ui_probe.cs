// QA round 32: scene, time scale, the focused UI element, and every active interactable Selectable with its label and
// screen rect (pixels, y from the bottom). Flags a focus that is null or sits on an inactive or non-interactable object.
var es = UnityEngine.EventSystems.EventSystem.current;
var sb = new System.Text.StringBuilder();
sb.Append("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " ts=" + UnityEngine.Time.timeScale.ToString("0.00") + " screen=" + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height);
System.Func<UnityEngine.GameObject, string> label = go =>
{
    if (go == null) return "null";
    var t = go.GetComponentInChildren<UnityEngine.UI.Text>();
    string s = t != null && !string.IsNullOrEmpty(t.text) ? t.text.Replace("\n", "/") : go.name;
    return s.Length > 40 ? s.Substring(0, 40) : s;
};
UnityEngine.GameObject sel = es != null ? es.currentSelectedGameObject : null;
string state = sel == null ? "NONE" : !sel.activeInHierarchy ? "INACTIVE" : "ok";
var selComp = sel != null ? sel.GetComponent<UnityEngine.UI.Selectable>() : null;
if (selComp != null && !selComp.IsInteractable()) state = "NOT-INTERACTABLE";
sb.Append(" | focus[" + state + "]=" + label(sel));
int n = 0;
foreach (var s in UnityEngine.UI.Selectable.allSelectablesArray)
{
    if (!s.isActiveAndEnabled) continue;
    var canvas = s.GetComponentInParent<UnityEngine.Canvas>();
    var cam = canvas != null && canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    var c = new UnityEngine.Vector3[4];
    ((UnityEngine.RectTransform)s.transform).GetWorldCorners(c);
    var a = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, c[0]);
    var b = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, c[2]);
    string nav = s.navigation.mode.ToString();
    sb.Append("\n  " + (s.IsInteractable() ? "" : "(off) ") + s.GetType().Name + " '" + label(s.gameObject) + "' nav=" + nav + " rect=" + a.x.ToString("0") + "," + a.y.ToString("0") + "-" + b.x.ToString("0") + "," + b.y.ToString("0"));
    n++;
}
sb.Append("\n  selectables=" + n);
return sb.ToString();
