// Night 1, item 2: the options title and the column's top, in canvas units, and the column's fit scale.
var fit = UnityEngine.Object.FindAnyObjectByType<Game.UI.ColumnFit>();
if (fit == null) return "no column";
var column = (UnityEngine.RectTransform)fit.transform;
var corners = new UnityEngine.Vector3[4]; column.GetWorldCorners(corners);
string title = "";
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>()) if (t.text == "Options" || t.text == "Story viewer") { var c = new UnityEngine.Vector3[4]; t.rectTransform.GetWorldCorners(c); title += t.text + " bottom y " + c[0].y.ToString("0") + " (text height " + t.preferredHeight.ToString("0") + ") "; }
return title + "column top y " + corners[1].y.ToString("0") + " bottom y " + corners[0].y.ToString("0") + " scale " + fit.Scale.ToString("0.00") + " screen " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height;
