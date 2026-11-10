// QA round 32: the scene and the focused UI element in one line.
var es = UnityEngine.EventSystems.EventSystem.current;
var sel = es != null ? es.currentSelectedGameObject : null;
string label = sel == null ? "NONE" : (sel.GetComponentInChildren<UnityEngine.UI.Text>() != null ? sel.GetComponentInChildren<UnityEngine.UI.Text>().text.Replace("\n", "/") : sel.name);
if (sel != null && !sel.activeInHierarchy) label = "INACTIVE:" + label;
return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " focus=" + label + " ts=" + UnityEngine.Time.timeScale.ToString("0.0");
