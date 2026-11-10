// QA round 33: the upgrade screen's detail header (the subject on show) and the focused element.
var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.UpgradeScreen>();
if (screen == null) return "no upgrade screen";
var header = (UnityEngine.UI.Text)typeof(Game.UI.UpgradeScreen).GetField("_header", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(screen);
var es = UnityEngine.EventSystems.EventSystem.current;
return "detail=" + header.text + " focus=" + (es.currentSelectedGameObject != null ? es.currentSelectedGameObject.name : "NONE");
