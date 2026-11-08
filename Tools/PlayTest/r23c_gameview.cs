// Round 23c: the Game view's rendering resolution and its window size, to judge what the overlays get to work with.
UnityEditor.PlayModeWindow.GetRenderingResolution(out uint w, out uint h);
var type = System.Type.GetType("UnityEditor.GameView,UnityEditor");
var views = UnityEngine.Resources.FindObjectsOfTypeAll(type);
var sb = new System.Text.StringBuilder("render " + w + "x" + h);
foreach (var view in views) { sb.Append(" window ").Append(((UnityEditor.EditorWindow)view).position.size); }
return sb.ToString();
