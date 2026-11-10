var sel = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
if (sel == null) return "none";
var p = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, sel.transform.position);
var s = sel.GetComponent<UnityEngine.UI.Selectable>();
return sel.name + " x=" + p.x.ToString("0") + " y=" + p.y.ToString("0") + " interactable=" + s.IsInteractable() + " sibling=" + sel.transform.GetSiblingIndex();
