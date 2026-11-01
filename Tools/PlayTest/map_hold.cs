// Simulates holding the right-hand side arrow of the planet map with the mouse and reports whether the globe turned.
var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.WorldMapScreen>();
if (screen == null) { return "no map screen"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var globe = typeof(Game.UI.WorldMapScreen).GetField("_globe", flags).GetValue(screen);
var yawField = globe.GetType().GetField("_yaw", flags);
float before = (float)yawField.GetValue(globe);
var sb = new System.Text.StringBuilder("yawBefore=" + before.ToString("0.0"));
UnityEngine.UI.Button target = null;
foreach (var trigger in UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventTrigger>(UnityEngine.FindObjectsSortMode.None))
{
    var rect = trigger.GetComponent<UnityEngine.RectTransform>();
    if (rect.anchoredPosition.x > 0f) { target = trigger.GetComponent<UnityEngine.UI.Button>(); }
}
if (target == null) { return sb + " no side button"; }
var es = UnityEngine.EventSystems.EventSystem.current;
var canvas = target.GetComponentInParent<UnityEngine.Canvas>();
UnityEngine.Vector2 screenPoint = UnityEngine.RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, target.transform.position);
var data = new UnityEngine.EventSystems.PointerEventData(es) { position = screenPoint, button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
es.RaycastAll(data, hits);
sb.Append(" raycastTop=" + (hits.Count > 0 ? hits[0].gameObject.name + "/" + hits[0].gameObject.transform.parent.name : "nothing") + " hits=" + hits.Count);
UnityEngine.EventSystems.ExecuteEvents.Execute(target.gameObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 60; i++) { UnityEditor.EditorApplication.Step(); }
float after = (float)yawField.GetValue(globe);
UnityEngine.EventSystems.ExecuteEvents.Execute(target.gameObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
UnityEditor.EditorApplication.isPaused = false;
int focused = (int)typeof(Game.UI.WorldMapScreen).GetField("_focused", flags).GetValue(screen);
sb.Append(" yawAfter=" + after.ToString("0.0") + " dt=" + UnityEngine.Time.unscaledDeltaTime.ToString("0.000") + " focused=" + focused);
return sb.ToString();
