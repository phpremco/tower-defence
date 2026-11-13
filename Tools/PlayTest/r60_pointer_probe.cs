// Round 60: where the Input System's mouse is, the screen size, and what the event system's raycast finds under it.
var mouse = UnityEngine.InputSystem.Mouse.current;
var events = UnityEngine.EventSystems.EventSystem.current;
var sb = new System.Text.StringBuilder();
sb.Append("screen ").Append(UnityEngine.Screen.width).Append("x").Append(UnityEngine.Screen.height);
if (mouse == null) return sb.Append(" no mouse").ToString();
var pos = mouse.position.ReadValue();
sb.Append(" mouse ").Append(pos.ToString("0")).Append(" module ").Append(events.currentInputModule != null ? events.currentInputModule.GetType().Name : "none");
var data = new UnityEngine.EventSystems.PointerEventData(events) { position = pos };
var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
events.RaycastAll(data, hits);
foreach (var h in hits) sb.Append(" | ").Append(h.gameObject.name).Append("<").Append(h.gameObject.transform.parent != null ? h.gameObject.transform.parent.name : "");
return sb.ToString();
